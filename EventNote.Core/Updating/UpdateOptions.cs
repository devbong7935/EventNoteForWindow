namespace EventNote.Core.Updating;

/// <summary>업데이트를 어디서 어떻게 받아 올지. 배포처를 바꾸려면 여기만 손대면 된다.</summary>
public sealed record UpdateOptions
{
    /// <summary>
    /// 매니페스트 주소. 이 주소는 앱에 박히므로 절대 바뀌지 않아야 한다.
    /// GitHub Releases 를 쓰더라도 매니페스트만은 raw.githubusercontent.com 의 정적 파일로 둔다.
    /// /releases/latest API 는 인증 없이 쓰면 IP 당 시간당 60회 제한이 걸려,
    /// 회사처럼 여러 PC 가 같은 공인 IP 를 쓰면 업데이트 확인이 막힌다.
    /// </summary>
    public required Uri ManifestUrl { get; init; }

    /// <summary>설치 파일을 받아 둘 폴더. 비워 두면 임시 폴더 아래를 쓴다.</summary>
    public string DownloadDirectory { get; init; } =
        Path.Combine(Path.GetTempPath(), "EventNote", "update");

    /// <summary>매니페스트를 읽는 데 걸어 둘 시간. 시작할 때 조용히 확인하므로 짧게 잡는다.</summary>
    public TimeSpan CheckTimeout { get; init; } = TimeSpan.FromSeconds(10);

    /// <summary>설치 파일을 받는 데 걸어 둘 시간.</summary>
    public TimeSpan DownloadTimeout { get; init; } = TimeSpan.FromMinutes(10);
}
