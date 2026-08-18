using System.Windows;
using System.Windows.Controls;

namespace EventNote.Support.UI.Units;

/// <summary>
/// 창 맨 위에 직접 그리는 제목줄. 왼쪽부터 마크 · 제목 · Content(메뉴 자리),
/// 오른쪽 끝에 최소화 · 최대화 · 닫기 버튼이 붙는다.
/// ChromeWindow 안에서만 쓴다. 창 버튼은 SystemCommands 로 창까지 올라간다.
/// </summary>
public class TitleBar : ContentControl
{
    static TitleBar()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(TitleBar), new FrameworkPropertyMetadata(typeof(TitleBar)));
    }

    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
        nameof(Title), typeof(string), typeof(TitleBar),
        new FrameworkPropertyMetadata(string.Empty));

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public static readonly DependencyProperty VersionProperty = DependencyProperty.Register(
        nameof(Version), typeof(string), typeof(TitleBar),
        new FrameworkPropertyMetadata(string.Empty));

    /// <summary>
    /// 메뉴 뒤에 흐리게 붙는 버전 문구. 비워 두면 자리를 차지하지 않는다.
    ///
    /// 메뉴가 든 Content 안이 아니라 그 다음 열에 그린다. Content 는 클릭을 메뉴가
    /// 가져가도록 되어 있어, 거기에 두면 그 자리를 잡고 창을 끌 수 없게 된다.
    /// 메뉴가 몇 개로 늘어나든 이 문구는 마지막 메뉴 바로 뒤를 따라간다.
    /// </summary>
    public string Version
    {
        get => (string)GetValue(VersionProperty);
        set => SetValue(VersionProperty, value);
    }

    public static readonly DependencyProperty ShowMarkProperty = DependencyProperty.Register(
        nameof(ShowMark), typeof(bool), typeof(TitleBar),
        new FrameworkPropertyMetadata(true));

    /// <summary>왼쪽 끝 봉투 마크를 보일지.</summary>
    public bool ShowMark
    {
        get => (bool)GetValue(ShowMarkProperty);
        set => SetValue(ShowMarkProperty, value);
    }

    public static readonly DependencyProperty ShowMinimizeProperty = DependencyProperty.Register(
        nameof(ShowMinimize), typeof(bool), typeof(TitleBar),
        new FrameworkPropertyMetadata(true));

    public bool ShowMinimize
    {
        get => (bool)GetValue(ShowMinimizeProperty);
        set => SetValue(ShowMinimizeProperty, value);
    }

    public static readonly DependencyProperty ShowMaximizeProperty = DependencyProperty.Register(
        nameof(ShowMaximize), typeof(bool), typeof(TitleBar),
        new FrameworkPropertyMetadata(true));

    public bool ShowMaximize
    {
        get => (bool)GetValue(ShowMaximizeProperty);
        set => SetValue(ShowMaximizeProperty, value);
    }
}
