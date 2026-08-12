using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace EventNote.Support.UI.Units;

/// <summary>
/// 금액 입력칸에 붙이는 동작. 숫자만 받고, 입력하는 동안 천 단위마다 쉼표를 넣는다.
/// 어떤 TextBox 에나 붙일 수 있다.
///
///   &lt;TextBox ctl:MoneyInput.IsEnabled="True" /&gt;
/// </summary>
public static class MoneyInput
{
    public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached(
        "IsEnabled", typeof(bool), typeof(MoneyInput),
        new PropertyMetadata(false, OnIsEnabledChanged));

    public static void SetIsEnabled(DependencyObject element, bool value)
        => element.SetValue(IsEnabledProperty, value);

    public static bool GetIsEnabled(DependencyObject element)
        => (bool)element.GetValue(IsEnabledProperty);

    /// <summary>서식을 다시 입히는 동안 TextChanged 가 재귀하지 않도록 세우는 깃발.</summary>
    private static readonly DependencyProperty IsFormattingProperty = DependencyProperty.RegisterAttached(
        "IsFormatting", typeof(bool), typeof(MoneyInput), new PropertyMetadata(false));

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextBox box) return;

        box.PreviewTextInput -= OnPreviewTextInput;
        box.TextChanged -= OnTextChanged;
        DataObject.RemovePastingHandler(box, OnPaste);

        if (!(bool)e.NewValue) return;

        box.PreviewTextInput += OnPreviewTextInput;
        box.TextChanged += OnTextChanged;
        DataObject.AddPastingHandler(box, OnPaste);
        Reformat(box);
    }

    private static void OnPreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        // 숫자가 아닌 글자는 아예 들어오지 못하게 막는다.
        if (!e.Text.All(char.IsDigit)) e.Handled = true;
    }

    private static void OnPaste(object sender, DataObjectPastingEventArgs e)
    {
        if (sender is not TextBox) return;

        var text = e.DataObject.GetDataPresent(DataFormats.UnicodeText)
            ? e.DataObject.GetData(DataFormats.UnicodeText) as string
            : null;

        // "45,000원" 처럼 붙여 넣어도 숫자만 남긴다.
        if (text is not null && text.All(char.IsDigit)) return;

        e.CancelCommand();

        if (text is null) return;
        var digits = new string(text.Where(char.IsDigit).ToArray());
        if (digits.Length > 0 && sender is TextBox box) box.SelectedText = digits;
    }

    private static void OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is TextBox box) Reformat(box);
    }

    private static void Reformat(TextBox box)
    {
        if ((bool)box.GetValue(IsFormattingProperty)) return;

        var text = box.Text ?? string.Empty;
        var caret = Math.Clamp(box.CaretIndex, 0, text.Length);

        // 커서 앞에 숫자가 몇 개 있었는지 기억해 두었다가 같은 자리로 되돌린다.
        var digitsBeforeCaret = text.Take(caret).Count(char.IsDigit);

        var digits = new string(text.Where(char.IsDigit).ToArray()).TrimStart('0');

        string formatted;
        if (digits.Length == 0)
        {
            formatted = string.Empty;
        }
        else if (long.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out var value))
        {
            formatted = value.ToString("N0", CultureInfo.CurrentCulture);
        }
        else
        {
            // long 범위를 넘는 자릿수. 서식을 건드리지 않고 그대로 둔다.
            return;
        }

        if (formatted == text) return;

        box.SetValue(IsFormattingProperty, true);
        try
        {
            box.Text = formatted;
            box.CaretIndex = CaretAfterDigits(formatted, digitsBeforeCaret);
        }
        finally
        {
            box.SetValue(IsFormattingProperty, false);
        }
    }

    /// <summary>서식이 바뀐 문자열에서 숫자 n개를 지난 위치를 찾는다.</summary>
    private static int CaretAfterDigits(string text, int digitCount)
    {
        if (digitCount <= 0) return 0;

        var seen = 0;
        for (var i = 0; i < text.Length; i++)
        {
            if (char.IsDigit(text[i])) seen++;
            if (seen == digitCount) return i + 1;
        }

        return text.Length;
    }
}
