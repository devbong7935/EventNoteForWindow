namespace EventNote.Core.Updating;

/// <summary>업데이트 확인 결과.</summary>
public enum UpdateStatus
{
    /// <summary>지금이 최신이다.</summary>
    UpToDate,

    /// <summary>받을 수 있는 새 버전이 있다.</summary>
    Available,

    /// <summary>새 버전이 있지만 지금 버전에서 바로 올릴 수 없다. 직접 받아 설치해야 한다.</summary>
    ManualRequired,

    /// <summary>확인하지 못했다. 네트워크가 없거나 매니페스트를 읽지 못한 경우.</summary>
    Unavailable,
}

/// <param name="Status">확인 결과.</param>
/// <param name="Manifest">UpToDate · Unavailable 이면 null 일 수 있다.</param>
public sealed record UpdateCheckResult(UpdateStatus Status, UpdateManifest? Manifest = null);

/// <summary>배포처에서 새 버전을 확인하고 설치 파일을 받아 설치를 시작한다.</summary>
public interface IUpdateService
{
    /// <summary>지금 돌고 있는 앱의 버전.</summary>
    Version CurrentVersion { get; }

    /// <summary>매니페스트를 읽어 새 버전이 있는지 본다. 실패해도 예외를 던지지 않는다.</summary>
    Task<UpdateCheckResult> CheckAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 설치 파일을 임시 폴더로 받고 해시를 검사한다. 받은 파일의 전체 경로를 돌려준다.
    /// 검사에 실패하면 파일을 지우고 예외를 던진다.
    /// </summary>
    /// <param name="progress">0~100 사이의 진행률. 서버가 크기를 알려주지 않으면 호출되지 않는다.</param>
    Task<string> DownloadAsync(
        UpdateManifest manifest,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 받아 둔 설치 파일로 설치를 시작한다. 호출한 쪽은 곧바로 앱을 종료해야 한다.
    /// 실행 중인 파일은 덮어쓸 수 없기 때문이다.
    /// </summary>
    /// <param name="manifest">설치 파일에 넘길 인자를 여기서 가져온다.</param>
    void StartInstall(string installerPath, UpdateManifest manifest);
}
