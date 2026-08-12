using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace EventNote.Support.Local.Converters;

/// <summary>false 일 때 보이고 true 일 때 숨긴다. 빈 화면 안내 문구에 쓴다.</summary>
public sealed class InverseBooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is Visibility.Collapsed;
}
