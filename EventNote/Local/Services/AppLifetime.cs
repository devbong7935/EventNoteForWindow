using System.Windows;
using EventNote.Main.Local.Services;

namespace EventNote.Local.Services;

/// <inheritdoc />
public sealed class AppLifetime : IAppLifetime
{
    public void Shutdown()
    {
        // Window.Close 가 아니라 Shutdown 을 부른다. Close 로 끝내면 MainWindow.OnClosing 이
        // "종료할까요?" 를 한 번 더 묻는데, 업데이트 설치는 이미 확인을 받고 오는 길이다.
        Application.Current.Shutdown();
    }
}
