using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Shell;
using System.Windows.Threading;

namespace EventNote.Support.UI.Shell;

/// <summary>
/// 제목줄을 직접 그리는 창의 바탕. WindowChrome 으로 기본 제목줄만 걷어내고
/// 창 자체는 그대로 둔다. 그래서 끌어서 옮기기 · 더블클릭 최대화 · 화면 가장자리 스냅 ·
/// 가장자리 크기 조절이 전부 Windows 가 해 주던 그대로 동작한다.
///
/// 제목줄 안에서 실제로 눌러야 하는 것(메뉴, 창 버튼)에는
/// WindowChrome.IsHitTestVisibleInChrome="True" 를 달아야 클릭이 먹는다.
/// 그 표시가 없으면 Windows 가 그 자리를 제목줄로 보고 클릭을 가져간다.
/// </summary>
public class ChromeWindow : Window
{
    private const int MonitorDefaultToNearest = 0x0002;

    /// <summary>제목줄 높이. Themes 의 TitleBar 높이와 반드시 같아야 한다.</summary>
    public const double TitleBarHeight = 40;

    protected ChromeWindow()
    {
        WindowChrome.SetWindowChrome(this, new WindowChrome
        {
            CaptionHeight = TitleBarHeight,
            ResizeBorderThickness = new Thickness(6),
            CornerRadius = default,
            GlassFrameThickness = new Thickness(0),
            UseAeroCaptionButtons = false,
        });

        CommandBindings.Add(new CommandBinding(
            SystemCommands.MinimizeWindowCommand,
            (_, _) => SystemCommands.MinimizeWindow(this),
            (_, e) => e.CanExecute = ResizeMode != ResizeMode.NoResize));

        CommandBindings.Add(new CommandBinding(
            SystemCommands.MaximizeWindowCommand,
            (_, _) => SystemCommands.MaximizeWindow(this),
            (_, e) => e.CanExecute = CanResize));

        CommandBindings.Add(new CommandBinding(
            SystemCommands.RestoreWindowCommand,
            (_, _) => SystemCommands.RestoreWindow(this),
            (_, e) => e.CanExecute = CanResize));

        CommandBindings.Add(new CommandBinding(
            SystemCommands.CloseWindowCommand,
            (_, _) => Close()));
    }

    private static readonly DependencyPropertyKey ChromePaddingPropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(ChromePadding), typeof(Thickness), typeof(ChromeWindow),
            new FrameworkPropertyMetadata(default(Thickness)));

    public static readonly DependencyProperty ChromePaddingProperty = ChromePaddingPropertyKey.DependencyProperty;

    /// <summary>
    /// 최대화했을 때 화면 밖으로 밀려나는 만큼의 여백.
    ///
    /// Windows 는 최대화한 창을 작업 영역보다 테두리 두께(보통 8px)만큼 크게 잡는다.
    /// 원래는 그 여백이 창 테두리에 가려지지만, 우리는 제목줄을 client 영역 맨 위까지
    /// 직접 그리므로 제목줄 위쪽과 좌우가 잘려 나간다.
    ///
    /// 창 템플릿의 바깥 Border 가 이 값을 Padding 으로 받으면 내용이 딱 작업 영역에 맞는다.
    /// Margin 이 아니라 Padding 이어야 한다. Margin 으로 밀면 그만큼 배경이 안 칠해진 채 남는다.
    ///
    /// WM_GETMINMAXINFO 로 최대화 크기 자체를 제한하는 방법도 있지만, 그 메시지는
    /// WPF Window 가 나중에 다시 처리하면서 값을 되돌려 놓아 이 창에서는 듣지 않았다.
    /// </summary>
    public Thickness ChromePadding => (Thickness)GetValue(ChromePaddingProperty);

    private bool CanResize => ResizeMode is ResizeMode.CanResize or ResizeMode.CanResizeWithGrip;

    protected override void OnStateChanged(EventArgs e)
    {
        base.OnStateChanged(e);

        // 상태가 바뀐 직후에는 창 위치가 아직 옮겨지기 전일 수 있다. 한 차례 뒤로 미뤄 잰다.
        Dispatcher.BeginInvoke(UpdateChromePadding, DispatcherPriority.Loaded);
    }

    private void UpdateChromePadding()
    {
        if (WindowState != WindowState.Maximized)
        {
            SetValue(ChromePaddingPropertyKey, default(Thickness));
            return;
        }

        SetValue(ChromePaddingPropertyKey, MeasureOverflow());
    }

    /// <summary>창 사각형이 모니터 작업 영역 밖으로 얼마나 나갔는지 재서 DIP 로 돌려준다.</summary>
    private Thickness MeasureOverflow()
    {
        var handle = new WindowInteropHelper(this).Handle;
        if (handle == IntPtr.Zero || !GetWindowRect(handle, out var window)) return default;

        var monitor = MonitorFromWindow(handle, MonitorDefaultToNearest);
        if (monitor == IntPtr.Zero) return default;

        var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
        if (!GetMonitorInfo(monitor, ref info)) return default;

        // 화면 픽셀로 잰 값이다. 배율이 걸린 화면에서는 DIP 로 바꿔야 여백이 두 배가 되지 않는다.
        var transform = PresentationSource.FromVisual(this)?.CompositionTarget?.TransformFromDevice;
        var scaleX = transform?.M11 ?? 1;
        var scaleY = transform?.M22 ?? 1;

        return new Thickness(
            Math.Max(0, info.Work.Left - window.Left) * scaleX,
            Math.Max(0, info.Work.Top - window.Top) * scaleY,
            Math.Max(0, window.Right - info.Work.Right) * scaleX,
            Math.Max(0, window.Bottom - info.Work.Bottom) * scaleY);
    }

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, int flags);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(IntPtr hwnd, out NativeRect rect);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MonitorInfo
    {
        public int Size;
        public NativeRect Monitor;
        public NativeRect Work;
        public int Flags;
    }
}
