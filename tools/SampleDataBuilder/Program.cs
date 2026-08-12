using System.Text.Json;
using EventNote.Core.Models;
using EventNote.Core.Security;
using EventNote.Core.Serialization;
using EventNote.Core.Services;

namespace EventNote.Tools.SampleDataBuilder;

/// <summary>
/// samples/sample-data.json 을 읽어 앱이 가져올 수 있는 .enote 파일로 굽는다.
///
///   dotnet run --project tools/SampleDataBuilder
///   dotnet run --project tools/SampleDataBuilder -- <입력.json> <출력.enote>
/// </summary>
internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        var root = FindRepositoryRoot();
        var inputPath = args.Length > 0
            ? Path.GetFullPath(args[0])
            : Path.Combine(root, "samples", "sample-data.json");
        var outputPath = args.Length > 1
            ? Path.GetFullPath(args[1])
            : Path.Combine(root, "samples", "예시데이터.enote");

        if (!File.Exists(inputPath))
        {
            Console.Error.WriteLine($"입력 파일이 없습니다: {inputPath}");
            return 1;
        }

        List<CeremonyEvent>? events;
        try
        {
            // 앱 저장소와 같은 직렬화 설정을 쓴다. 종류(Category)는 "Wedding" 처럼 이름으로 적으면 된다.
            events = EventJson.Deserialize<List<CeremonyEvent>>(await File.ReadAllBytesAsync(inputPath));
        }
        catch (JsonException ex)
        {
            Console.Error.WriteLine($"JSON 을 해석하지 못했습니다: {ex.Message}");
            return 1;
        }

        if (events is null || events.Count == 0)
        {
            Console.Error.WriteLine("행사가 하나도 없습니다.");
            return 1;
        }

        var archive = new EventArchiveService(new AesGcmDataProtector());
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        await archive.ExportAsync(events, outputPath);

        // 저장소와 같은 원자적 쓰기를 쓰는 탓에 직전 내용이 .bak 으로 남는다.
        // 예시 파일에는 쓸모없으니 치운다.
        var stale = outputPath + ".bak";
        if (File.Exists(stale)) File.Delete(stale);

        Console.WriteLine($"입력  {inputPath}");
        Console.WriteLine($"출력  {outputPath}  ({new FileInfo(outputPath).Length:N0} bytes)");
        Console.WriteLine();

        // 앱이 쓰는 가져오기 경로로 곧바로 다시 읽어 본다. 여기서 통과하면 앱에서도 열린다.
        var reloaded = await archive.ImportAsync(outputPath);

        foreach (var e in reloaded.Events)
        {
            Console.WriteLine($"  {e.Category.ToGlyph()} {e.DisplayTitle}  ({e.DateDisplay})");
            Console.WriteLine(
                $"     하객 {e.Guests.Count}명 · 금액 {e.TotalAmount:N0}원 · 식권 {e.TotalTickets}장 · 식대 차감 후 {e.NetAmount:N0}원");
        }

        var ok = reloaded.Events.Count == events.Count
                 && reloaded.Events.Sum(e => e.Guests.Count) == events.Sum(e => e.Guests.Count)
                 && reloaded.Events.Sum(e => e.TotalAmount) == events.Sum(e => e.TotalAmount);

        Console.WriteLine();
        Console.WriteLine(ok
            ? $"검증 통과 — 행사 {reloaded.Manifest.EventCount}건 / 하객 {reloaded.Manifest.GuestCount}명 그대로 복원됨"
            : "검증 실패 — 다시 읽은 내용이 입력과 다릅니다");

        return ok ? 0 : 1;
    }

    /// <summary>실행 위치가 bin 아래여도 samples 폴더를 찾을 수 있게 위로 훑는다.</summary>
    private static string FindRepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "EventNote.sln"))) return dir.FullName;
            dir = dir.Parent;
        }

        return Directory.GetCurrentDirectory();
    }
}
