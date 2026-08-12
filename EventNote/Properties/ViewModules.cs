using EventNote.Forms.UI.Views;
using EventNote.Main.Local.ViewModels;
using EventNote.Main.UI.Views;
using Microsoft.Extensions.DependencyInjection;

namespace EventNote.Properties;

/// <summary>화면과 ViewModel 을 등록한다.</summary>
internal static class ViewModules
{
    public static IServiceCollection AddViewModules(this IServiceCollection services)
    {
        // 셸
        services.AddSingleton<MainWindow>();

        // 본문
        services.AddSingleton<MainContent>();
        services.AddSingleton<MainViewModel>();

        // 대화상자는 띄울 때마다 새로 만든다.
        services.AddTransient<EventEditorWindow>();

        return services;
    }
}
