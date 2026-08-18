using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;

namespace EventNote.Core.Updating;

/// <summary>
/// 정적 파일 두 개(version.json · 설치 파일)만 있으면 도는 업데이트 서비스.
/// 서버 쪽에 따로 프로그램을 둘 필요가 없다.
/// </summary>
public sealed class HttpUpdateService : IUpdateService, IDisposable
{
    private readonly UpdateOptions _options;
    private readonly HttpClient _http;

    public HttpUpdateService(UpdateOptions options)
    {
        _options = options;

        _http = new HttpClient
        {
            // 요청마다 CancellationToken 으로 따로 시간을 건다. 확인은 짧게, 내려받기는 길게 잡아야 하는데
            // HttpClient.Timeout 은 클라이언트 하나에 하나뿐이라 여기서는 풀어 둔다.
            Timeout = Timeout.InfiniteTimeSpan,
        };
        _http.DefaultRequestHeaders.UserAgent.ParseAdd($"EventNote/{AppVersion.ToDisplay(CurrentVersion)}");
    }

    public Version CurrentVersion => AppVersion.Current;

    public async Task<UpdateCheckResult> CheckAsync(CancellationToken cancellationToken = default)
    {
        var manifest = await ReadManifestAsync(cancellationToken);
        if (manifest is null) return new UpdateCheckResult(UpdateStatus.Unavailable);

        if (manifest.Version <= CurrentVersion) return new UpdateCheckResult(UpdateStatus.UpToDate, manifest);

        // 너무 옛 버전에서 건너뛰어 올리면 설치가 깨질 수 있는 경우. 직접 받아 설치하도록 안내한다.
        if (manifest.MinUpgradableFrom is { } min && CurrentVersion < min)
            return new UpdateCheckResult(UpdateStatus.ManualRequired, manifest);

        return new UpdateCheckResult(UpdateStatus.Available, manifest);
    }

    public async Task<string> DownloadAsync(
        UpdateManifest manifest,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(_options.DownloadDirectory);
        RemoveLeftovers();

        var path = Path.Combine(_options.DownloadDirectory, manifest.Installer.FileName);

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(_options.DownloadTimeout);

        string actual;
        try
        {
            actual = await SaveAsync(manifest, path, progress, timeout.Token);
        }
        catch
        {
            Delete(path);
            throw;
        }

        // 해시를 적어 두었을 때만 대조한다. 적지 않았다면 위 SaveAsync 안에서
        // 받은 바이트 수를 세어 이미 걸렀으므로, 잘린 파일이 여기까지 오지는 않는다.
        if (manifest.Installer.HasChecksum
            && !string.Equals(actual, manifest.Installer.Sha256, StringComparison.Ordinal))
        {
            Delete(path);
            throw new InvalidDataException(
                "내려받은 설치 파일이 서버의 파일과 다릅니다. 잠시 후 다시 시도해 주세요.");
        }

        return path;
    }

    public void StartInstall(string installerPath, UpdateManifest manifest)
    {
        if (!File.Exists(installerPath))
            throw new FileNotFoundException("설치 파일을 찾지 못했습니다.", installerPath);

        // Program Files 에 설치한다면 관리자 승인(UAC) 창이 뜬다.
        // UseShellExecute 를 켜야 그 승격 요청이 제대로 올라간다.
        var start = Path.GetExtension(installerPath).Equals(".exe", StringComparison.OrdinalIgnoreCase)
            // NSIS · Inno 같은 설치 관리자 exe. 무인 설치 인자는 매니페스트가 정한다.
            // NSIS 는 대문자 "/S" 다. 소문자 "/s" 는 먹지 않는다.
            ? new ProcessStartInfo(installerPath)
            {
                Arguments = manifest.Installer.Arguments,
                UseShellExecute = true,
            }
            // MSI 를 직접 받은 경우. 인자를 따로 주지 않았으면 진행 막대만 있는 화면으로 깐다.
            : new ProcessStartInfo("msiexec.exe")
            {
                Arguments = string.IsNullOrWhiteSpace(manifest.Installer.Arguments)
                    ? $"/i \"{installerPath}\" /qb /norestart"
                    : $"/i \"{installerPath}\" {manifest.Installer.Arguments}",
                UseShellExecute = true,
            };

        Process.Start(start);
    }

    public void Dispose() => _http.Dispose();

    // 내부 처리 -------------------------------------------------------------

    /// <summary>매니페스트를 받아 온다. 못 읽으면 null. 여기서 예외가 새 나가면 안 된다.</summary>
    private async Task<UpdateManifest?> ReadManifestAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(_options.CheckTimeout);

            // raw.githubusercontent.com 은 CDN 이 몇 분간 옛 내용을 물고 있다.
            // 헤더만으로는 캐시가 잘 안 비켜서 주소에도 매번 다른 값을 붙인다.
            var url = new UriBuilder(_options.ManifestUrl);
            url.Query = string.IsNullOrEmpty(url.Query)
                ? $"t={DateTimeOffset.UtcNow.Ticks}"
                : $"{url.Query.TrimStart('?')}&t={DateTimeOffset.UtcNow.Ticks}";

            using var request = new HttpRequestMessage(HttpMethod.Get, url.Uri);
            request.Headers.CacheControl = new CacheControlHeaderValue { NoCache = true, NoStore = true };

            using var response = await _http.SendAsync(request, timeout.Token);
            if (!response.IsSuccessStatusCode) return null;

            return UpdateManifest.Parse(await response.Content.ReadAsStringAsync(timeout.Token));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            // 네트워크가 없거나 느린 것뿐이다. 다음에 켤 때 다시 본다.
            return null;
        }
    }

    /// <summary>본문을 파일로 흘려 담으면서 해시를 같이 계산한다. 계산된 해시를 돌려준다.</summary>
    private async Task<string> SaveAsync(
        UpdateManifest manifest,
        string path,
        IProgress<int>? progress,
        CancellationToken cancellationToken)
    {
        using var response = await _http.GetAsync(
            manifest.Installer.Url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        // 매니페스트가 알려 준 크기를 먼저 믿고, 없으면 응답 헤더를 본다.
        var total = manifest.Installer.Size > 0
            ? manifest.Installer.Size
            : response.Content.Headers.ContentLength ?? 0;

        using var sha = SHA256.Create();
        await using var source = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var target = new FileStream(
            path, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true);

        var buffer = new byte[81920];
        long received = 0;
        var lastPercent = -1;

        while (true)
        {
            var read = await source.ReadAsync(buffer, cancellationToken);
            if (read == 0) break;

            sha.TransformBlock(buffer, 0, read, null, 0);
            await target.WriteAsync(buffer.AsMemory(0, read), cancellationToken);

            received += read;
            if (total <= 0) continue;

            // 진행률이 실제로 바뀔 때만 알린다. 바이트마다 알리면 화면이 그것만 그리다 만다.
            var percent = (int)Math.Min(100, received * 100 / total);
            if (percent == lastPercent) continue;

            lastPercent = percent;
            progress?.Report(percent);
        }

        // 받다가 끊긴 파일을 그대로 실행하면 설치가 이상하게 깨진다. 해시를 적어 두지 않았을 때
        // 이게 유일한 안전장치다. 서버가 크기를 알려 준 경우에만 볼 수 있다.
        var expected = response.Content.Headers.ContentLength ?? 0;
        if (expected > 0 && received != expected)
        {
            throw new InvalidDataException(
                $"내려받다가 중간에 끊겼습니다. ({received:N0} / {expected:N0} 바이트) 잠시 후 다시 시도해 주세요.");
        }

        sha.TransformFinalBlock([], 0, 0);
        return Convert.ToHexString(sha.Hash ?? []).ToLowerInvariant();
    }

    /// <summary>지난번에 받다 만 파일이나 이미 설치한 파일을 치운다. 실패해도 그냥 넘어간다.</summary>
    private void RemoveLeftovers()
    {
        try
        {
            foreach (var file in Directory.EnumerateFiles(_options.DownloadDirectory))
                Delete(file);
        }
        catch (IOException)
        {
            // 지우지 못해도 새로 받는 데는 지장이 없다.
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static void Delete(string path)
    {
        try
        {
            if (File.Exists(path)) File.Delete(path);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
