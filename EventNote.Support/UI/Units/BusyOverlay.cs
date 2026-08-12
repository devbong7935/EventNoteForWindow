using System.Windows;
using System.Windows.Controls;

namespace EventNote.Support.UI.Units;

/// <summary>
/// 오래 걸리는 일이 도는 동안 화면을 덮는 판. 가운데에서 동그라미가 돈다.
/// 뒤쪽을 흐리게 하는 것은 이 컨트롤이 아니라 덮이는 쪽에서 한다. 무엇을 흐리게 할지는
/// 화면마다 다르고, 이 판까지 함께 흐려지면 안 되기 때문이다.
/// </summary>
public class BusyOverlay : Control
{
    static BusyOverlay()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(BusyOverlay), new FrameworkPropertyMetadata(typeof(BusyOverlay)));
    }

    public static readonly DependencyProperty IsActiveProperty = DependencyProperty.Register(
        nameof(IsActive), typeof(bool), typeof(BusyOverlay),
        new FrameworkPropertyMetadata(false));

    /// <summary>켜면 판이 나타나고 동그라미가 돌기 시작한다. 끄면 회전도 멈춘다.</summary>
    public bool IsActive
    {
        get => (bool)GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }

    public static readonly DependencyProperty MessageProperty = DependencyProperty.Register(
        nameof(Message), typeof(string), typeof(BusyOverlay),
        new FrameworkPropertyMetadata(string.Empty));

    /// <summary>동그라미 아래에 적을 한 줄. 비우면 그 자리가 사라진다.</summary>
    public string Message
    {
        get => (string)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }
}
