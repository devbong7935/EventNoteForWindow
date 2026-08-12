using System.Windows;
using System.Windows.Threading;
using EventNote.Forms.UI.Views;
using EventNote.Main.UI.Views;
using EventNote.Properties;
using Microsoft.Extensions.DependencyInjection;

namespace EventNote;

/// <summary>
/// 앱 본체. 리소스 병합, 모듈 등록, 셸 생성을 여기서 담당한다.
/// </summary>
internal class App : Application
{
    private ServiceProvider? _services;

    /// <summary>뷰의 코드 비하인드에서 필요할 때 쓰는 서비스 공급자.</summary>
    public static IServiceProvider Services =>
        ((App)Current)._services ?? throw new InvalidOperationException("서비스가 아직 준비되지 않았습니다.");

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        LoadResources();

        _services = new ServiceCollection()
            .AddServiceModules()
            .AddViewModules()
            .BuildServiceProvider();

        DispatcherUnhandledException += OnUnhandledException;

        var shell = CreateShell();
        MainWindow = shell;
        shell.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _services?.Dispose();
        base.OnExit(e);
    }

    /// <summary>셸 창을 만들고 본문과 ViewModel 을 끼워 넣는다.</summary>
    protected virtual Window CreateShell()
    {
        var shell = Services.GetRequiredService<MainWindow>();
        shell.Content = Services.GetRequiredService<MainContent>();

        new WireDataContext().Apply(shell, Services);

        return shell;
    }

    /// <summary>앱 전체에서 쓰는 색과 기본 컨트롤 외형.</summary>
    private void LoadResources()
    {
        Resources.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/EventNote.Support;component/Themes/AppTheme.xaml", UriKind.Absolute),
        });
    }

    private void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show(
            $"예기치 않은 오류가 발생했습니다.\n\n{e.Exception.Message}",
            "경조사 명부",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        e.Handled = true;
    }
}
