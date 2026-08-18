using System.Reflection;

namespace EventNote.Core.Updating;

/// <summary>버전을 읽고 비교하는 일을 한곳에 모아 둔다.</summary>
public static class AppVersion
{
    /// <summary>지금 돌고 있는 앱의 버전. Directory.Build.props 의 Version 을 따라간다.</summary>
    public static Version Current { get; } = Normalize(Assembly.GetEntryAssembly()?.GetName().Version);

    /// <summary>
    /// 빠진 자리를 0 으로 채운다. 비교하기 전에 양쪽 다 반드시 거쳐야 한다.
    ///
    /// Version.Parse("1.0.1") 은 Revision 이 -1 인 값을 만든다. 어셈블리에서 읽은 버전은
    /// 언제나 네 자리라 1.0.1.0 이고, Version 의 비교는 -1 을 0 보다 작게 본다.
    /// 그대로 두면 매니페스트의 "1.0.1" 이 실행 중인 1.0.1.0 보다 낮다고 나와,
    /// 같은 버전인데도 업데이트가 있다고 판단하거나 그 반대가 된다.
    /// </summary>
    public static Version Normalize(Version? version) => version is null
        ? new Version(0, 0, 0, 0)
        : new Version(version.Major, version.Minor, Math.Max(version.Build, 0), Math.Max(version.Revision, 0));

    /// <summary>매니페스트에서 읽은 문자열을 버전으로 바꾼다. 실패하면 false.</summary>
    public static bool TryParse(string? text, out Version version)
    {
        if (Version.TryParse(text, out var parsed))
        {
            version = Normalize(parsed);
            return true;
        }

        version = new Version(0, 0, 0, 0);
        return false;
    }

    /// <summary>
    /// 화면에 보여 줄 문구.
    ///
    /// 네 번째 자리를 쓰는 버전(1.0.0.1)이면 네 자리로, 아니면 세 자리로 적는다.
    /// 늘 세 자리로 자르면 1.0.0.1 이 "1.0.0" 으로 나와, 업데이트 창에
    /// "새 버전 1.0.0 · 현재 버전 1.0.0" 처럼 같은 숫자가 두 번 찍힌다.
    /// </summary>
    public static string ToDisplay(Version version)
        => version.Revision > 0 ? version.ToString(4) : version.ToString(3);
}
