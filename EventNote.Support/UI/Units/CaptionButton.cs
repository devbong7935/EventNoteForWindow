using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace EventNote.Support.UI.Units;

/// <summary>제목줄 오른쪽의 최소화 · 최대화 · 닫기 버튼. 닫기만 빨갛게 반응한다.</summary>
public class CaptionButton : Button
{
    static CaptionButton()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(CaptionButton), new FrameworkPropertyMetadata(typeof(CaptionButton)));
    }

    public static readonly DependencyProperty GlyphProperty = DependencyProperty.Register(
        nameof(Glyph), typeof(Geometry), typeof(CaptionButton),
        new FrameworkPropertyMetadata(null));

    /// <summary>가운데 그릴 선 도형. 채우지 않고 선으로만 그린다.</summary>
    public Geometry? Glyph
    {
        get => (Geometry?)GetValue(GlyphProperty);
        set => SetValue(GlyphProperty, value);
    }

    public static readonly DependencyProperty IsCloseProperty = DependencyProperty.Register(
        nameof(IsClose), typeof(bool), typeof(CaptionButton),
        new FrameworkPropertyMetadata(false));

    /// <summary>닫기 버튼인지. 켜면 마우스를 올렸을 때 빨간 배경 + 흰 글자가 된다.</summary>
    public bool IsClose
    {
        get => (bool)GetValue(IsCloseProperty);
        set => SetValue(IsCloseProperty, value);
    }
}
