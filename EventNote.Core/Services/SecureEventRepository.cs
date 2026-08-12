using System.Text.Json;
using EventNote.Core.Models;
using EventNote.Core.Security;
using EventNote.Core.Serialization;

namespace EventNote.Core.Services;

/// <summary>
/// 행사 전체를 앱 전용 암호화 파일(events.dat) 하나에 보관한다.
/// 저장은 임시 파일에 쓴 뒤 교체하므로 저장 도중 전원이 꺼져도 기존 파일이 남는다.
/// 예전 버전의 events.json 이 있으면 처음 읽을 때 자동으로 옮겨 담는다.
/// </summary>
public sealed class SecureEventRepository : IEventRepository
{
    private readonly IDataProtector _protector;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _legacyPath;

    public SecureEventRepository(IAppPathProvider paths, IDataProtector protector)
    {
        _protector = protector;
        StorePath = Path.Combine(paths.DataDirectory, "events.dat");
        _legacyPath = Path.Combine(paths.DataDirectory, "events.json");
    }

    public string StorePath { get; }

    public async Task<IReadOnlyList<CeremonyEvent>> LoadAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (File.Exists(StorePath))
            {
                var envelope = await File.ReadAllBytesAsync(StorePath, cancellationToken).ConfigureAwait(false);
                return Decode(envelope);
            }

            if (File.Exists(_legacyPath))
                return await MigrateLegacyAsync(cancellationToken).ConfigureAwait(false);

            return Array.Empty<CeremonyEvent>();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SaveAsync(IEnumerable<CeremonyEvent> events, CancellationToken cancellationToken = default)
    {
        var payload = _protector.Protect(EventJson.Serialize(events.ToList()));

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await WriteAtomicAsync(StorePath, payload, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>임시 파일에 먼저 쓰고 원본과 바꿔치기한다. 직전 내용은 .bak 으로 남는다.</summary>
    internal static async Task WriteAtomicAsync(string path, byte[] payload, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        var tempPath = path + ".tmp";
        await File.WriteAllBytesAsync(tempPath, payload, cancellationToken).ConfigureAwait(false);

        if (File.Exists(path))
            File.Replace(tempPath, path, path + ".bak", ignoreMetadataErrors: true);
        else
            File.Move(tempPath, path);
    }

    private IReadOnlyList<CeremonyEvent> Decode(byte[] envelope)
    {
        try
        {
            var json = _protector.Unprotect(envelope);
            return EventJson.Deserialize<List<CeremonyEvent>>(json) ?? new List<CeremonyEvent>();
        }
        catch (Exception ex) when (ex is InvalidDataException or JsonException)
        {
            // 읽지 못한 파일은 반드시 사본을 남긴다. 빈 목록으로 시작했다가 덮어써 버리면 복구할 길이 없다.
            var copy = BackupUnreadableFile();
            throw new InvalidDataException(
                $"저장된 데이터를 읽지 못했습니다. ({ex.Message})\n" +
                $"원본 사본을 남겨 두었습니다: {copy}", ex);
        }
    }

    private async Task<IReadOnlyList<CeremonyEvent>> MigrateLegacyAsync(CancellationToken cancellationToken)
    {
        var json = await File.ReadAllBytesAsync(_legacyPath, cancellationToken).ConfigureAwait(false);

        List<CeremonyEvent> events;
        try
        {
            events = JsonSerializer.Deserialize<List<CeremonyEvent>>(json, EventJson.LegacyOptions) ?? new();
        }
        catch (JsonException)
        {
            return Array.Empty<CeremonyEvent>();
        }

        // 곧바로 암호화해 새 파일로 옮기고, 평문 파일은 확장자를 바꿔 눈에 띄지 않게 치운다.
        await WriteAtomicAsync(StorePath, _protector.Protect(EventJson.Serialize(events)), cancellationToken)
            .ConfigureAwait(false);

        try
        {
            File.Move(_legacyPath, _legacyPath + ".migrated", overwrite: true);
        }
        catch (IOException)
        {
            // 옮기지 못해도 새 파일은 이미 만들어졌다.
        }

        return events;
    }

    private string BackupUnreadableFile()
    {
        var copy = $"{StorePath}.broken-{DateTime.Now:yyyyMMddHHmmss}";
        try
        {
            File.Copy(StorePath, copy, overwrite: true);
            return copy;
        }
        catch (IOException)
        {
            return "(사본을 만들지 못했습니다)";
        }
    }
}
