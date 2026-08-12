using System.Text.Json;
using EventNote.Core.Models;
using EventNote.Core.Security;
using EventNote.Core.Serialization;

namespace EventNote.Core.Services;

/// <summary>
/// 행사 데이터를 .enote 파일 하나로 묶는다. 저장소와 같은 암호화 형식을 쓰므로
/// 텍스트 편집기로 열어도 내용을 알아볼 수 없고, 다른 컴퓨터의 이 프로그램에서는 그대로 열린다.
/// </summary>
public sealed class EventArchiveService : IEventArchiveService
{
    private readonly IDataProtector _protector;

    public EventArchiveService(IDataProtector protector) => _protector = protector;

    public string FileFilter => $"경조사 명부 데이터 (*{IEventArchiveService.FileExtension})|*{IEventArchiveService.FileExtension}";

    public async Task ExportAsync(
        IEnumerable<CeremonyEvent> events, string filePath, CancellationToken cancellationToken = default)
    {
        var snapshot = events.ToList();

        var envelope = new ArchiveEnvelope
        {
            Manifest = new ArchiveManifest
            {
                ExportedAt = DateTime.Now,
                SourceMachine = Environment.MachineName,
                EventCount = snapshot.Count,
                GuestCount = snapshot.Sum(e => e.Guests.Count),
            },
            Events = snapshot,
        };

        var payload = _protector.Protect(EventJson.Serialize(envelope));
        await SecureEventRepository.WriteAtomicAsync(filePath, payload, cancellationToken).ConfigureAwait(false);
    }

    public async Task<ArchiveContent> ImportAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var bytes = await File.ReadAllBytesAsync(filePath, cancellationToken).ConfigureAwait(false);

        if (!_protector.IsProtected(bytes))
            throw new InvalidDataException("경조사 명부에서 내보낸 파일이 아닙니다.");

        ArchiveEnvelope? envelope;
        try
        {
            envelope = EventJson.Deserialize<ArchiveEnvelope>(_protector.Unprotect(bytes));
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException("파일 내용을 해석하지 못했습니다.", ex);
        }

        if (envelope?.Events is null)
            throw new InvalidDataException("파일에 행사 정보가 없습니다.");

        return new ArchiveContent(envelope.Manifest, envelope.Events);
    }

    public string SuggestFileName()
        => $"경조사명부_{DateTime.Now:yyyyMMdd_HHmm}{IEventArchiveService.FileExtension}";

    /// <summary>파일에 실제로 담기는 구조.</summary>
    private sealed class ArchiveEnvelope
    {
        public ArchiveManifest Manifest { get; set; } = new();

        public List<CeremonyEvent> Events { get; set; } = new();
    }
}
