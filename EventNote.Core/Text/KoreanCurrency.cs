using System.Text;

namespace EventNote.Core.Text;

/// <summary>금액을 한글 표기로 바꾼다. 예: 150000 → 십오만원.</summary>
public static class KoreanCurrency
{
    private static readonly string[] Digits = { "", "일", "이", "삼", "사", "오", "육", "칠", "팔", "구" };
    private static readonly string[] SmallUnits = { "", "십", "백", "천" };
    private static readonly string[] LargeUnits = { "", "만", "억", "조", "경" };

    /// <summary>예: 50000 → "오만원", 0 → "".</summary>
    public static string ToHangul(long amount, string suffix = "원")
    {
        if (amount == 0) return string.Empty;

        var negative = amount < 0;
        var value = Math.Abs(amount);

        // 4자리씩 끊어 만/억/조 단위를 붙인다.
        var groups = new List<int>();
        while (value > 0)
        {
            groups.Add((int)(value % 10000));
            value /= 10000;
        }

        var sb = new StringBuilder();
        for (var i = groups.Count - 1; i >= 0; i--)
        {
            if (groups[i] == 0) continue;
            sb.Append(ConvertGroup(groups[i]));
            sb.Append(LargeUnits[Math.Min(i, LargeUnits.Length - 1)]);
        }

        if (negative) sb.Insert(0, "마이너스 ");
        sb.Append(suffix);
        return sb.ToString();
    }

    /// <summary>영수증/장부용 정식 표기. 예: "일금 오만원정".</summary>
    public static string ToFormalHangul(long amount)
        => amount == 0 ? string.Empty : $"일금 {ToHangul(amount)}정";

    private static string ConvertGroup(int group)
    {
        var sb = new StringBuilder();
        for (var pos = 3; pos >= 0; pos--)
        {
            var digit = group / (int)Math.Pow(10, pos) % 10;
            if (digit == 0) continue;

            // 십/백/천 앞의 "일"은 생략한다. (15 → 십오, 1 → 일)
            if (!(digit == 1 && pos > 0)) sb.Append(Digits[digit]);
            sb.Append(SmallUnits[pos]);
        }

        return sb.ToString();
    }
}
