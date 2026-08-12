using EventNote.Core.Models;

namespace EventNote.Core.Services;

/// <summary>내보낸 파일 앞머리에 함께 담기는 정보. 가져오기 전에 미리 보여준다.</summary>
public sealed record ArchiveManifest
{
    public string Application { get; init; } = "EventNote";

    public int FormatVersion { get; init; } = 1;

    public DateTime ExportedAt { get; init; } = DateTime.Now;

    /// <summary>내보낸 컴퓨터 이름. 여러 곳에서 주고받을 때 출처를 알기 쉽다.</summary>
    public string SourceMachine { get; init; } = string.Empty;

    public int EventCount { get; init; }

    public int GuestCount { get; init; }
}

/// <summary>내보내기 파일에서 읽어낸 내용.</summary>
public sealed record ArchiveContent(ArchiveManifest Manifest, IReadOnlyList<CeremonyEvent> Events);

/// <summary>행사 데이터를 다른 컴퓨터로 옮기기 위한 앱 전용 파일(.enote) 입출력.</summary>
public interface IEventArchiveService
{
    /// <summary>내보내기 파일 확장자.</summary>
    static string FileExtension => ".enote";

    /// <summary>파일 열기/저장 대화상자에 쓸 필터.</summary>
    string FileFilter { get; }

    Task ExportAsync(IEnumerable<CeremonyEvent> events, string filePath, CancellationToken cancellationToken = default);

    Task<ArchiveContent> ImportAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>기본 파일 이름(확장자 포함).</summary>
    string SuggestFileName();
}
