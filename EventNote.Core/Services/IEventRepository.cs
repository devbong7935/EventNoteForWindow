using EventNote.Core.Models;

namespace EventNote.Core.Services;

/// <summary>행사 목록 전체를 읽고 쓰는 저장소.</summary>
public interface IEventRepository
{
    /// <summary>실제 데이터 파일 경로. 설정 화면/도움말에서 안내용으로 쓴다.</summary>
    string StorePath { get; }

    Task<IReadOnlyList<CeremonyEvent>> LoadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(IEnumerable<CeremonyEvent> events, CancellationToken cancellationToken = default);
}
