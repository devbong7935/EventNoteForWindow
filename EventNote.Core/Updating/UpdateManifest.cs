using System.Text.Json;
using System.Text.Json.Serialization;

namespace EventNote.Core.Updating;

/// <summary>내려받을 설치 파일 한 개.</summary>
public sealed record UpdatePackage
{
    public required Uri Url { get; init; }

    /// <summary>바이트 수. 진행률 표시와 받은 파일 검사에 쓴다. 0 이면 모른다는 뜻.</summary>
    public long Size { get; init; }

    /// <summary>
    /// 소문자 16진수 64자리. 적어 두면 받은 파일을 실행하기 전에 이 값과 대조한다.
    /// 비워 두면 대조를 건너뛴다. 그래도 받다가 끊긴 파일은 크기로 걸러진다.
    /// </summary>
    public string Sha256 { get; init; } = string.Empty;

    /// <summary>해시 대조를 할 수 있는지.</summary>
    public bool HasChecksum => Sha256.Length == 64;

    /// <summary>
    /// 설치 파일에 넘길 인자. NSIS 면 "/S" 로 조용히 깔 수 있다.
    /// 비워 두면 설치 화면이 그대로 뜬다. 설치 관리자가 무인 설치를 지원하는지 모를 때는
    /// 비워 두는 편이 낫다. 지원하지 않는데 인자를 주면 설치가 그냥 멎을 수 있다.
    /// </summary>
    public string Arguments { get; init; } = string.Empty;

    /// <summary>주소에서 딴 파일 이름. 받은 파일을 이 이름으로 저장한다.</summary>
    public string FileName => Path.GetFileName(Url.AbsolutePath);

    /// <summary>
    /// setup.exe 처럼 .NET 런타임까지 함께 까는 부트스트래퍼인지.
    /// 이쪽은 msiexec 이 아니라 파일을 그대로 실행해야 한다.
    /// </summary>
    public bool IsBootstrapper =>
        Path.GetExtension(Url.AbsolutePath).Equals(".exe", StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// 배포처에 올려 두는 version.json 한 벌. 앱은 이 파일 하나만 보고 새 버전 여부를 판단한다.
/// 파일 목록을 뒤지지 않는 이유는 목록 API 가 배포처마다 다르고 언제든 바뀔 수 있어서다.
/// </summary>
public sealed record UpdateManifest
{
    public required Version Version { get; init; }

    public DateOnly? ReleasedAt { get; init; }

    /// <summary>참이면 건너뛸 수 없는 업데이트로 안내한다.</summary>
    public bool Mandatory { get; init; }

    /// <summary>
    /// 이 버전으로 곧바로 올릴 수 있는 최소 버전. 이보다 낮은 데서 돌고 있으면
    /// 자동 설치가 깨질 수 있으므로 직접 받아 설치하도록 안내한다.
    /// </summary>
    public Version? MinUpgradableFrom { get; init; }

    public string Notes { get; init; } = string.Empty;

    public required UpdatePackage Installer { get; init; }

    /// <summary>
    /// 받아 온 JSON 을 매니페스트로 바꾼다. 형식이 어긋나면 null 을 돌려준다.
    /// 여기서 예외를 던지지 않는 건 업데이트 확인 실패가 앱 사용을 막아서는 안 되기 때문이다.
    /// </summary>
    public static UpdateManifest? Parse(string json)
    {
        Payload? payload;
        try
        {
            // 맨 앞의 BOM 을 떼고 넘긴다. 메모장이나 PowerShell 로 매니페스트를 고치면
            // 눈에 보이지 않는 세 바이트가 붙는데, JSON 파서는 그 첫 글자에서 그대로 멎는다.
            payload = JsonSerializer.Deserialize<Payload>(json.TrimStart('﻿', ' ', '\t', '\r', '\n'), Options);
        }
        catch (JsonException)
        {
            return null;
        }

        if (payload?.Installer is null) return null;
        if (!AppVersion.TryParse(payload.Version, out var version)) return null;
        if (!Uri.TryCreate(payload.Installer.Url, UriKind.Absolute, out var url)) return null;

        // http 로 받은 설치 파일은 중간에서 바꿔치기할 수 있다. https 가 아니면 쓰지 않는다.
        if (url.Scheme != Uri.UriSchemeHttps) return null;

        // 우리가 실행할 줄 아는 건 이 둘뿐이다. 다른 확장자는 받아 봐야 처리하지 못한다.
        var extension = Path.GetExtension(url.AbsolutePath);
        if (!extension.Equals(".msi", StringComparison.OrdinalIgnoreCase)
            && !extension.Equals(".exe", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        // 해시는 선택이다. 배포처가 https 로 고정돼 있어 중간에서 바꿔치기하는 건 TLS 가 막고,
        // 받다가 끊긴 건 바이트 수로 잡는다. 해시가 더 잡아 주는 건 "엉뚱한 파일을 올린 실수" 쪽이다.
        // 적어 두었다면 형식만은 맞아야 한다. 반쯤 적힌 해시로 검사하는 척하는 게 제일 나쁘다.
        var sha256 = payload.Installer.Sha256?.Trim().ToLowerInvariant() ?? string.Empty;
        if (sha256.Length > 0 && (sha256.Length != 64 || !sha256.All(Uri.IsHexDigit))) return null;

        return new UpdateManifest
        {
            Version = version,
            ReleasedAt = DateOnly.TryParse(payload.ReleasedAt, out var released) ? released : null,
            Mandatory = payload.Mandatory,
            MinUpgradableFrom = AppVersion.TryParse(payload.MinUpgradableFrom, out var min) ? min : null,
            Notes = payload.Notes?.Trim() ?? string.Empty,
            Installer = new UpdatePackage
            {
                Url = url,
                Size = payload.Installer.Size,
                Sha256 = sha256,
                Arguments = payload.Installer.Arguments?.Trim() ?? string.Empty,
            },
        };
    }

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
    };

    /// <summary>JSON 을 그대로 받아 내는 중간 형태. 버전 문자열 검증은 위에서 따로 한다.</summary>
    private sealed record Payload
    {
        public string? Version { get; init; }
        public string? ReleasedAt { get; init; }
        public bool Mandatory { get; init; }
        public string? MinUpgradableFrom { get; init; }
        public string? Notes { get; init; }
        public PayloadInstaller? Installer { get; init; }
    }

    private sealed record PayloadInstaller
    {
        public string? Url { get; init; }
        public long Size { get; init; }
        public string? Sha256 { get; init; }
        public string? Arguments { get; init; }
    }
}
