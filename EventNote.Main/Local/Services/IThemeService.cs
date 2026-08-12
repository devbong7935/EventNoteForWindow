using EventNote.Support.Local.Theming;

namespace EventNote.Main.Local.Services;

/// <summary>테마를 적용하고, 고른 테마를 다음 실행까지 기억한다.</summary>
public interface IThemeService
{
    AppThemeMode Current { get; }

    void Apply(AppThemeMode mode);
}
