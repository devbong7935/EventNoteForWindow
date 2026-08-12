using System.Text.Json;
using System.Text.Json.Serialization;

namespace EventNote.Core.Serialization;

/// <summary>암호화 전(그리고 복호화 후) 단계에서 쓰는 공통 직렬화 설정.</summary>
public static class EventJson
{
    public static JsonSerializerOptions Options { get; } = new()
    {
        WriteIndented = false,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>사람이 읽을 수 있게 저장하던 예전 파일을 읽을 때 쓴다.</summary>
    public static JsonSerializerOptions LegacyOptions { get; } = new(Options) { WriteIndented = true };

    public static byte[] Serialize<T>(T value) => JsonSerializer.SerializeToUtf8Bytes(value, Options);

    public static T? Deserialize<T>(ReadOnlySpan<byte> utf8Json) => JsonSerializer.Deserialize<T>(utf8Json, Options);
}
