using System.Windows;
using System.Windows.Controls;

namespace EventNote.Support.UI.Units;

/// <summary>
/// 제목이 달린 흰색 카드. 화면을 "하객 리스트", "집계" 같은 구획으로 나눌 때 쓴다.
/// Header 를 비워두면 제목 줄이 사라진다.
/// </summary>
public class CardPanel : HeaderedContentControl
{
    static CardPanel()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(CardPanel), new FrameworkPropertyMetadata(typeof(CardPanel)));
    }

    public static readonly DependencyProperty CornerRadiusProperty = DependencyProperty.Register(
        nameof(CornerRadius), typeof(CornerRadius), typeof(CardPanel),
        new FrameworkPropertyMetadata(new CornerRadius(6)));

    public CornerRadius CornerRadius
    {
        get => (CornerRadius)GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }

    public static readonly DependencyProperty HeaderAlignmentProperty = DependencyProperty.Register(
        nameof(HeaderAlignment), typeof(HorizontalAlignment), typeof(CardPanel),
        new FrameworkPropertyMetadata(HorizontalAlignment.Left));

    /// <summary>제목 정렬. 기본은 왼쪽.</summary>
    public HorizontalAlignment HeaderAlignment
    {
        get => (HorizontalAlignment)GetValue(HeaderAlignmentProperty);
        set => SetValue(HeaderAlignmentProperty, value);
    }

    public static readonly DependencyProperty HeaderAccessoryProperty = DependencyProperty.Register(
        nameof(HeaderAccessory), typeof(object), typeof(CardPanel),
        new FrameworkPropertyMetadata(null));

    /// <summary>제목 줄 오른쪽에 얹을 부가 요소(버튼 등).</summary>
    public object? HeaderAccessory
    {
        get => GetValue(HeaderAccessoryProperty);
        set => SetValue(HeaderAccessoryProperty, value);
    }
}
