namespace EventNote.Core.Security;

/// <summary>데이터를 앱 전용 형식으로 봉인하고 되돌린다.</summary>
public interface IDataProtector
{
    /// <summary>평문을 암호화된 봉투 바이트로 만든다.</summary>
    byte[] Protect(ReadOnlySpan<byte> plaintext);

    /// <summary>봉투 바이트를 평문으로 되돌린다. 형식이 다르거나 훼손됐으면 <see cref="InvalidDataException"/>.</summary>
    byte[] Unprotect(ReadOnlySpan<byte> envelope);

    /// <summary>이 바이트열이 우리 형식인지 빠르게 확인한다.</summary>
    bool IsProtected(ReadOnlySpan<byte> data);
}
