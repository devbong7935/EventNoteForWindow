using EventNote.Core.Security;
using EventNote.Core.Services;
using EventNote.Core.Updating;
using EventNote.Local.Services;
using EventNote.Main.Local.Services;
using Microsoft.Extensions.DependencyInjection;

namespace EventNote.Properties;

/// <summary>저장소 · 내보내기 · 대화상자 등 화면이 아닌 서비스를 등록한다.</summary>
internal static class ServiceModules
{
    /// <summary>
    /// 업데이트 매니페스트 주소. 저장소에 커밋해 둔 정적 파일이라 주소가 영구히 고정된다.
    /// 설치 파일 자체의 주소는 이 파일 안에 들어 있으므로 여기서 알 필요가 없다.
    /// </summary>
    private const string ManifestUrl =
        "https://raw.githubusercontent.com/devbong7935/EventNoteForWindow/main/update/version.json";

    public static IServiceCollection AddServiceModules(this IServiceCollection services)
    {
        // 저장소 / 내보내기
        services.AddSingleton<IAppPathProvider>(_ => new AppPathProvider("EventNote"));
        services.AddSingleton<IDataProtector, AesGcmDataProtector>();
        services.AddSingleton<IEventRepository, SecureEventRepository>();
        services.AddSingleton<IEventArchiveService, EventArchiveService>();
        services.AddSingleton<IExcelExportService, ClosedXmlExportService>();

        // 자동 업데이트
        services.AddSingleton(new UpdateOptions { ManifestUrl = new Uri(ManifestUrl) });
        services.AddSingleton<IUpdateService, HttpUpdateService>();

        // UI 서비스
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<IThemeService, ThemeService>();
        services.AddSingleton<IAppLifetime, AppLifetime>();

        return services;
    }
}
