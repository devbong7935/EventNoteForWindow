namespace EventNote;

/// <summary>
/// 실행 진입점. App.xaml 이 없으므로 Main 을 직접 들고 있다.
/// </summary>
internal class Starter
{
    [STAThread]
    private static void Main(string[] args)
    {
        _ = new App().Run();
    }
}
