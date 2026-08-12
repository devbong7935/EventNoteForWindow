using System.Windows;
using System.Windows.Controls;

namespace EventNote.Support.UI.Units;

/// <summary>버튼의 성격. 색은 Themes/AccentButton.xaml 의 트리거에서 결정한다.</summary>
public enum AccentButtonKind
{
    /// <summary>기본 동작(파랑).</summary>
    Primary,

    /// <summary>추가/확인 같은 긍정 동작(연두).</summary>
    Success,

    /// <summary>삭제 같은 위험 동작(연빨강).</summary>
    Danger,

    /// <summary>보조 동작(흰 배경 + 테두리).</summary>
    Neutral,
}

/// <summary>
/// 앱 전반에서 재사용하는 알약 모양 버튼. Kind 로 색을, Glyph 로 오른쪽 아이콘 문자를 지정한다.
/// </summary>
public class AccentButton : Button
{
    static AccentButton()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(AccentButton), new FrameworkPropertyMetadata(typeof(AccentButton)));
    }

    public static readonly DependencyProperty KindProperty = DependencyProperty.Register(
        nameof(Kind), typeof(AccentButtonKind), typeof(AccentButton),
        new FrameworkPropertyMetadata(AccentButtonKind.Neutral));

    /// <summary>버튼 성격(색).</summary>
    public AccentButtonKind Kind
    {
        get => (AccentButtonKind)GetValue(KindProperty);
        set => SetValue(KindProperty, value);
    }

    public static readonly DependencyProperty GlyphProperty = DependencyProperty.Register(
        nameof(Glyph), typeof(string), typeof(AccentButton),
        new FrameworkPropertyMetadata(string.Empty));

    /// <summary>텍스트 오른쪽에 붙는 기호. 예: "＋", "－", "⤓".</summary>
    public string Glyph
    {
        get => (string)GetValue(GlyphProperty);
        set => SetValue(GlyphProperty, value);
    }

    public static readonly DependencyProperty CornerRadiusProperty = DependencyProperty.Register(
        nameof(CornerRadius), typeof(CornerRadius), typeof(AccentButton),
        new FrameworkPropertyMetadata(new CornerRadius(6)));

    public CornerRadius CornerRadius
    {
        get => (CornerRadius)GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }
}
