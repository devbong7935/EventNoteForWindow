using System.Security.Cryptography;

namespace EventNote.Core.Security;

/// <summary>
/// 키 파생에 쓰는 앱 고유 비밀.
///
/// 실제 비밀 값은 파일이나 상수 문자열로 존재하지 않는다. 조각을 섞어 해시한 결과를
/// 필요할 때 잠깐 만들어 쓰고 곧바로 지운다. 덕분에 실행 파일을 문자열 검색해도 찾히지 않는다.
/// 물론 프로그램을 본격적으로 분석하는 상대까지 막는 장치는 아니다.
/// 그런 수준이 필요하면 사용자 비밀번호 기반으로 바꿔야 한다.
/// </summary>
internal static class AppSecret
{
    private static readonly byte[] Alpha =
    {
        0x7C, 0x1E, 0xA3, 0x55, 0xD0, 0x2B, 0x9F, 0x64,
        0x11, 0xE7, 0x38, 0xCA, 0x4D, 0x86, 0x50, 0xF2,
        0x0B, 0x93, 0x27, 0xBE, 0x6A, 0xD4, 0x1C, 0x78,
        0xA5, 0x3F, 0xE0, 0x59, 0x8C, 0xB1, 0x46, 0xDD,
    };

    private static readonly byte[] Beta =
    {
        0x35, 0xC8, 0x60, 0x1B, 0x9E, 0x47, 0xF3, 0x2A,
        0xD6, 0x0C, 0x81, 0x5F, 0xB4, 0x73, 0xE9, 0x18,
        0xA2, 0x6D, 0xCF, 0x04, 0x51, 0x8B, 0x36, 0xE2,
        0x1D, 0xC6, 0x7A, 0x9B, 0x40, 0x2F, 0xF8, 0x63,
    };

    private const byte Mask = 0x5A;

    /// <summary>32바이트 비밀을 만들어 넣는다.</summary>
    internal static void Write(Span<byte> destination)
    {
        if (destination.Length != 32)
            throw new ArgumentException("비밀 길이는 32바이트여야 합니다.", nameof(destination));

        Span<byte> material = stackalloc byte[Alpha.Length + Beta.Length];
        for (var i = 0; i < Alpha.Length; i++)
        {
            material[i] = (byte)(Alpha[i] ^ Mask);
            // Beta 는 뒤에서부터 섞는다.
            material[Alpha.Length + i] = (byte)(Beta[Beta.Length - 1 - i] ^ Alpha[i]);
        }

        try
        {
            SHA256.HashData(material, destination);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(material);
        }
    }
}
