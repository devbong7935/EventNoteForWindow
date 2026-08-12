using System.Windows;
using System.Windows.Controls;

namespace EventNote.Support.UI.Units;

/// <summary>
/// 금액 전용 입력칸. 천 단위 쉼표가 자동으로 붙고, 칸 오른쪽 안쪽에 단위(원 등)를 띄운다.
/// </summary>
public class MoneyTextBox : TextBox
{
    static MoneyTextBox()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(MoneyTextBox), new FrameworkPropertyMetadata(typeof(MoneyTextBox)));
    }

    public MoneyTextBox()
    {
        MoneyInput.SetIsEnabled(this, true);
    }

    public static readonly DependencyProperty SuffixProperty = DependencyProperty.Register(
        nameof(Suffix), typeof(string), typeof(MoneyTextBox),
        new FrameworkPropertyMetadata(string.Empty));

    /// <summary>칸 안쪽 오른쪽에 붙는 단위. 예: 원.</summary>
    public string Suffix
    {
        get => (string)GetValue(SuffixProperty);
        set => SetValue(SuffixProperty, value);
    }

    public static readonly DependencyProperty PlaceholderProperty = DependencyProperty.Register(
        nameof(Placeholder), typeof(string), typeof(MoneyTextBox),
        new FrameworkPropertyMetadata(string.Empty));

    /// <summary>비어 있을 때 흐리게 보여줄 안내 문구.</summary>
    public string Placeholder
    {
        get => (string)GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }
}
