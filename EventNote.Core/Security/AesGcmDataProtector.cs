using System.Security.Cryptography;

namespace EventNote.Core.Security;

/// <summary>
/// 앱 전용 봉투 형식. AES-256-GCM 으로 암호화하며 위·변조도 함께 검출한다.
///
/// 바이트 배치
///   0..3   매직 "ENTE"
///   4      형식 버전
///   5..20  솔트 16
///   21..32 논스 12
///   33..48 인증 태그 16
///   49..   암호문
///
/// 키는 앱에 심어 둔 비밀에서 파생한다. 비밀번호를 묻지 않는 대신,
/// 파일을 텍스트 편집기나 엑셀로 열어도 내용을 알아볼 수 없다.
/// 다만 이 프로그램 자체를 분석하는 사람까지 막아주지는 못한다.
/// </summary>
public sealed class AesGcmDataProtector : IDataProtector
{
    private const byte FormatVersion = 1;
    private const int MagicLength = 4;
    private const int SaltLength = 16;
    private const int NonceLength = 12;
    private const int TagLength = 16;
    private const int KeyLength = 32;
    private const int HeaderLength = MagicLength + 1 + SaltLength + NonceLength + TagLength;

    private static ReadOnlySpan<byte> Magic => "ENTE"u8;

    /// <summary>키 파생에 쓰는 부가 정보. 버전을 바꾸면 예전 파일과 섞이지 않는다.</summary>
    private static ReadOnlySpan<byte> KeyInfo => "EventNote.Store.v1"u8;

    public byte[] Protect(ReadOnlySpan<byte> plaintext)
    {
        var envelope = new byte[HeaderLength + plaintext.Length];

        Magic.CopyTo(envelope);
        envelope[MagicLength] = FormatVersion;

        var salt = envelope.AsSpan(MagicLength + 1, SaltLength);
        var nonce = envelope.AsSpan(MagicLength + 1 + SaltLength, NonceLength);
        var tag = envelope.AsSpan(MagicLength + 1 + SaltLength + NonceLength, TagLength);
        var ciphertext = envelope.AsSpan(HeaderLength);

        RandomNumberGenerator.Fill(salt);
        RandomNumberGenerator.Fill(nonce);

        Span<byte> key = stackalloc byte[KeyLength];
        DeriveKey(salt, key);

        try
        {
            using var aes = new AesGcm(key, TagLength);
            aes.Encrypt(nonce, plaintext, ciphertext, tag);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
        }

        return envelope;
    }

    public byte[] Unprotect(ReadOnlySpan<byte> envelope)
    {
        if (!IsProtected(envelope))
            throw new InvalidDataException("이 프로그램의 데이터 파일 형식이 아닙니다.");

        if (envelope[MagicLength] != FormatVersion)
            throw new InvalidDataException($"지원하지 않는 파일 버전입니다. (버전 {envelope[MagicLength]})");

        var salt = envelope.Slice(MagicLength + 1, SaltLength);
        var nonce = envelope.Slice(MagicLength + 1 + SaltLength, NonceLength);
        var tag = envelope.Slice(MagicLength + 1 + SaltLength + NonceLength, TagLength);
        var ciphertext = envelope[HeaderLength..];

        var plaintext = new byte[ciphertext.Length];

        Span<byte> key = stackalloc byte[KeyLength];
        DeriveKey(salt, key);

        try
        {
            using var aes = new AesGcm(key, TagLength);
            aes.Decrypt(nonce, ciphertext, tag, plaintext);
        }
        catch (CryptographicException ex)
        {
            // 태그 검증 실패. 파일이 깨졌거나 중간에 손을 댄 경우다.
            throw new InvalidDataException("파일이 손상되었거나 내용이 변경되었습니다.", ex);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
        }

        return plaintext;
    }

    public bool IsProtected(ReadOnlySpan<byte> data)
        => data.Length >= HeaderLength && data[..MagicLength].SequenceEqual(Magic);

    private static void DeriveKey(ReadOnlySpan<byte> salt, Span<byte> destination)
    {
        Span<byte> secret = stackalloc byte[32];
        AppSecret.Write(secret);

        try
        {
            // 비밀은 이미 앱 안에 있으므로 반복 연산(PBKDF2)은 의미가 없다.
            // 솔트마다 다른 키를 뽑는 용도로 HKDF 를 쓴다.
            HKDF.DeriveKey(HashAlgorithmName.SHA256, secret, destination, salt, KeyInfo);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(secret);
        }
    }
}
