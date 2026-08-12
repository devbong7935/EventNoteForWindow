using System.IO;
using EventNote.Core.Services;
using EventNote.Main.Local.Services;
using EventNote.Support.Local.Theming;

namespace EventNote.Local.Services;

/// <summary>
/// 고른 테마를 데이터 폴더의 theme.txt 에 한 줄로 남긴다.
/// 저장에 실패해도 이번 실행에는 지장이 없으므로 조용히 넘어간다.
/// </summary>
public sealed class ThemeService : IThemeService
{
    private readonly string _path;

    public ThemeService(IAppPathProvider paths)
    {
        _path = Path.Combine(paths.DataDirectory, "theme.txt");
        Current = Read();
    }

    public AppThemeMode Current { get; private set; }

    public void Apply(AppThemeMode mode)
    {
        ThemeManager.Apply(mode);
        Current = mode;
        Write(mode);
    }

    private AppThemeMode Read()
    {
        try
        {
            return File.Exists(_path) && Enum.TryParse<AppThemeMode>(File.ReadAllText(_path).Trim(), true, out var mode)
                ? mode
                : AppThemeMode.Light;
        }
        catch (IOException)
        {
            return AppThemeMode.Light;
        }
    }

    private void Write(AppThemeMode mode)
    {
        try
        {
            File.WriteAllText(_path, mode.ToString());
        }
        catch (IOException)
        {
            // 다음에 켜면 라이트로 시작할 뿐이다.
        }
    }
}
