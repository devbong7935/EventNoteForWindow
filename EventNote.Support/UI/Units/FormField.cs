using System.Windows;
using System.Windows.Controls;

namespace EventNote.Support.UI.Units;

/// <summary>왼쪽에 라벨, 오른쪽에 입력 컨트롤을 두는 폼 한 줄. 행사 편집 창에서 쓴다.</summary>
public class FormField : ContentControl
{
    static FormField()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(FormField), new FrameworkPropertyMetadata(typeof(FormField)));
    }

    public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(
        nameof(Label), typeof(string), typeof(FormField), new FrameworkPropertyMetadata(string.Empty));

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public static readonly DependencyProperty LabelWidthProperty = DependencyProperty.Register(
        nameof(LabelWidth), typeof(double), typeof(FormField), new FrameworkPropertyMetadata(88d));

    public double LabelWidth
    {
        get => (double)GetValue(LabelWidthProperty);
        set => SetValue(LabelWidthProperty, value);
    }

    public static readonly DependencyProperty HintProperty = DependencyProperty.Register(
        nameof(Hint), typeof(string), typeof(FormField), new FrameworkPropertyMetadata(string.Empty));

    /// <summary>입력란 아래 작은 설명.</summary>
    public string Hint
    {
        get => (string)GetValue(HintProperty);
        set => SetValue(HintProperty, value);
    }
}
