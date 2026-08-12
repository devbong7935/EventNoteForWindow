using System.Windows;

namespace EventNote.Support.Local.Theming;

/// <summary>화면 테마.</summary>
public enum AppThemeMode
{
    Light,
    Dark,
}

/// <summary>
/// 팔레트 딕셔너리(Palette.Light.xaml / Palette.Dark.xaml)를 앱 리소스 맨 뒤에 얹어 색을 바꾼다.
/// 색을 쓰는 쪽이 전부 DynamicResource 라 이 한 번의 교체로 화면 전체가 즉시 따라온다.
///
/// AppTheme.xaml 안에도 팔레트가 한 벌 들어 있지만 그쪽은 건드리지 않는다.
/// 중첩된 자식 딕셔너리를 고치면 WPF 가 이미 그려진 화면까지 무효화를 전파해 주지 않아,
/// 시작할 때는 멀쩡하고 실행 중에 바꾸면 색이 그대로 남는다.
/// Application.Resources.MergedDictionaries 최상위에서 더하고 빼야 화면이 다시 그려진다.
/// 병합 딕셔너리는 나중에 넣은 쪽이 이기므로, 맨 뒤에 얹으면 AppTheme.xaml 안쪽 팔레트를 덮는다.
/// </summary>
public static class ThemeManager
{
    private const string PaletteMarker = "Themes/Palette.";

    public static AppThemeMode Current { get; private set; } = AppThemeMode.Light;

    public static void Apply(AppThemeMode mode)
    {
        var application = Application.Current;
        if (application is null) return;

        var palette = new Uri(
            $"pack://application:,,,/EventNote.Support;component/Themes/Palette.{mode}.xaml",
            UriKind.Absolute);

        var merged = application.Resources.MergedDictionaries;

        // 이미 같은 팔레트가 맨 뒤에 얹혀 있으면 아무것도 하지 않는다.
        // 다시 끼우면 화면 전체가 공연히 한 번 더 그려진다.
        if (merged.Count > 0 && merged[^1].Source == palette)
        {
            Current = mode;
            return;
        }

        for (var i = merged.Count - 1; i >= 0; i--)
        {
            if (IsPalette(merged[i].Source)) merged.RemoveAt(i);
        }

        merged.Add(new ResourceDictionary { Source = palette });
        Current = mode;
    }

    private static bool IsPalette(Uri? source)
        => source is not null
           && source.OriginalString.Contains(PaletteMarker, StringComparison.OrdinalIgnoreCase);
}
