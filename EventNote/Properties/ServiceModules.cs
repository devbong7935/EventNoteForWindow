using EventNote.Core.Security;
using EventNote.Core.Services;
using EventNote.Local.Services;
using EventNote.Main.Local.Services;
using Microsoft.Extensions.DependencyInjection;

namespace EventNote.Properties;

/// <summary>저장소 · 내보내기 · 대화상자 등 화면이 아닌 서비스를 등록한다.</summary>
internal static class ServiceModules
{
    public static IServiceCollection AddServiceModules(this IServiceCollection services)
    {
        // 저장소 / 내보내기
        services.AddSingleton<IAppPathProvider>(_ => new AppPathProvider("EventNote"));
        services.AddSingleton<IDataProtector, AesGcmDataProtector>();
        services.AddSingleton<IEventRepository, SecureEventRepository>();
        services.AddSingleton<IEventArchiveService, EventArchiveService>();
        services.AddSingleton<IExcelExportService, ClosedXmlExportService>();

        // UI 서비스
        services.AddSingleton<IDialogService, DialogService>();

        return services;
    }
}
