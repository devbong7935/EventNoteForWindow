using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using EventNote.Main.Local.ViewModels;

namespace EventNote.Forms.UI.Views;

/// <summary>
/// 셸 창. 창 틀과 메뉴만 갖고, 본문은 Content 로 받아 끼운다.
/// 겉모습은 Themes/Views/MainWindow.xaml 이 맡고, 여기서는 수명주기와 단축키만 다룬다.
/// </summary>
public class MainWindow : Window
{
    private bool _closeConfirmed;

    static MainWindow()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(MainWindow), new FrameworkPropertyMetadata(typeof(MainWindow)));
    }

    public MainWindow()
    {
        // WindowStartupLocation 은 의존 속성이 아니어서 스타일 Setter 로는 지정할 수 없다.
        WindowStartupLocation = WindowStartupLocation.CenterScreen;

        // 파일 > 끝내기 메뉴가 쓰는 명령.
        CommandBindings.Add(new CommandBinding(ApplicationCommands.Close, (_, _) => Close()));

        InputBindings.Add(Shortcut(Key.N, ModifierKeys.Control, nameof(MainViewModel.AddEventCommand)));
        InputBindings.Add(Shortcut(Key.S, ModifierKeys.Control, nameof(MainViewModel.SaveNowCommand)));
        InputBindings.Add(Shortcut(Key.E, ModifierKeys.Control, nameof(MainViewModel.ExportCurrentCommand)));
        InputBindings.Add(Shortcut(Key.Insert, ModifierKeys.None, nameof(MainViewModel.AddGuestCommand)));

        Loaded += OnLoaded;
    }

    private MainViewModel? ViewModel => DataContext as MainViewModel;

    /// <summary>ViewModel 의 명령에 연결된 단축키 하나를 만든다.</summary>
    private static KeyBinding Shortcut(Key key, ModifierKeys modifiers, string commandPath)
    {
        var binding = new KeyBinding { Key = key, Modifiers = modifiers };
        BindingOperations.SetBinding(binding, InputBinding.CommandProperty, new Binding(commandPath));
        return binding;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (ViewModel is null) return;
        await ViewModel.LoadCommand.ExecuteAsync(null);
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        base.OnClosing(e);

        if (_closeConfirmed || ViewModel is null) return;

        // 물어보는 동안에는 일단 닫기를 막는다.
        // 닫기 버튼 · 파일 > 끝내기 · Alt+F4 모두 이 경로를 지난다.
        e.Cancel = true;
        _ = ConfirmThenCloseAsync();
    }

    private async Task ConfirmThenCloseAsync()
    {
        if (ViewModel is null || !await ViewModel.ConfirmCloseAsync()) return;

        _closeConfirmed = true;

        // OnClosing 을 처리하는 중에 Close 를 부르면 WPF 가 예외를 낸다.
        // 저장할 내용이 없으면 위 await 가 동기로 끝나 아직 그 안이므로, 한 차례 뒤로 미뤄서 닫는다.
        await Dispatcher.InvokeAsync(Close, DispatcherPriority.Background);
    }
}
