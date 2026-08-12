using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace EventNote.Support.UI.Units;

/// <summary>
/// "전체 금액 / 0 원" 처럼 왼쪽에 항목명, 오른쪽에 값을 보여주는 집계 타일.
/// </summary>
public class StatTile : Control
{
    static StatTile()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(StatTile), new FrameworkPropertyMetadata(typeof(StatTile)));
    }

    public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(
        nameof(Label), typeof(string), typeof(StatTile), new FrameworkPropertyMetadata(string.Empty));

    /// <summary>왼쪽 항목명.</summary>
    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value), typeof(string), typeof(StatTile), new FrameworkPropertyMetadata(string.Empty));

    /// <summary>오른쪽 값. 서식이 적용된 문자열을 그대로 넣는다.</summary>
    public string Value
    {
        get => (string)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public static readonly DependencyProperty UnitProperty = DependencyProperty.Register(
        nameof(Unit), typeof(string), typeof(StatTile), new FrameworkPropertyMetadata(string.Empty));

    /// <summary>값 뒤에 붙는 단위. 예: 원, 명, 장.</summary>
    public string Unit
    {
        get => (string)GetValue(UnitProperty);
        set => SetValue(UnitProperty, value);
    }

    public static readonly DependencyProperty AccentProperty = DependencyProperty.Register(
        nameof(Accent), typeof(Brush), typeof(StatTile),
        new FrameworkPropertyMetadata(Brushes.WhiteSmoke));

    /// <summary>값 영역 배경색.</summary>
    public Brush Accent
    {
        get => (Brush)GetValue(AccentProperty);
        set => SetValue(AccentProperty, value);
    }

    public static readonly DependencyProperty LabelWidthProperty = DependencyProperty.Register(
        nameof(LabelWidth), typeof(double), typeof(StatTile), new FrameworkPropertyMetadata(100d));

    public double LabelWidth
    {
        get => (double)GetValue(LabelWidthProperty);
        set => SetValue(LabelWidthProperty, value);
    }
}
