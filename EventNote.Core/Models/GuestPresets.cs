namespace EventNote.Core.Models;

/// <summary>입력 편의를 위한 기본 후보 값들.</summary>
public static class GuestPresets
{
    /// <summary>관계(분류) 콤보 박스 기본 항목.</summary>
    public static IReadOnlyList<string> Relations { get; } = new[]
    {
        "신랑측", "신부측", "가족", "친척", "친구", "직장", "학교", "교회", "단체", "이웃", "기타",
    };

    /// <summary>금액 빠른 입력 후보.</summary>
    public static IReadOnlyList<long> Amounts { get; } = new long[]
    {
        30_000, 50_000, 100_000, 200_000, 300_000, 500_000, 1_000_000,
    };
}
