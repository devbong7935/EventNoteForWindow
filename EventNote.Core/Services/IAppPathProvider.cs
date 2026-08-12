namespace EventNote.Core.Services;

/// <summary>앱이 쓰는 폴더 경로를 알려준다. 테스트에서 갈아끼울 수 있도록 인터페이스로 둔다.</summary>
public interface IAppPathProvider
{
    /// <summary>데이터 폴더. 없으면 만들어서 돌려준다.</summary>
    string DataDirectory { get; }
}

public sealed class AppPathProvider : IAppPathProvider
{
    private readonly Lazy<string> _dataDirectory;

    public AppPathProvider(string? appName = null)
    {
        var name = string.IsNullOrWhiteSpace(appName) ? "EventNote" : appName;
        _dataDirectory = new Lazy<string>(() =>
        {
            var path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), name);
            Directory.CreateDirectory(path);
            return path;
        });
    }

    public string DataDirectory => _dataDirectory.Value;
}
