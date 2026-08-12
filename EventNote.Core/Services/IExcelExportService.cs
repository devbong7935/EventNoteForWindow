using EventNote.Core.Models;

namespace EventNote.Core.Services;

/// <summary>하객 명부를 엑셀(.xlsx)로 내보낸다.</summary>
public interface IExcelExportService
{
    /// <summary>행사 하나를 시트 하나로 저장한다.</summary>
    Task ExportAsync(CeremonyEvent ceremonyEvent, string filePath, CancellationToken cancellationToken = default);

    /// <summary>여러 행사를 한 파일에 시트별로 저장한다.</summary>
    Task ExportAsync(IEnumerable<CeremonyEvent> events, string filePath, CancellationToken cancellationToken = default);

    /// <summary>행사명/날짜로 만든 기본 파일 이름(확장자 포함).</summary>
    string SuggestFileName(CeremonyEvent ceremonyEvent);
}
