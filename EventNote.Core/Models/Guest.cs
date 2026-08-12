namespace EventNote.Core.Models;

/// <summary>행사에 참석한 하객 한 명과 그가 낸 경조사금.</summary>
public class Guest
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>이름.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>금액(원).</summary>
    public long Amount { get; set; }

    /// <summary>관계(분류). 집계표의 분류 기준이 된다. 예: 신랑측, 신부측, 가족, 직장.</summary>
    public string Relation { get; set; } = string.Empty;

    /// <summary>소속. 예: OO회사 개발팀.</summary>
    public string Affiliation { get; set; } = string.Empty;

    /// <summary>식권 매수.</summary>
    public int MealTickets { get; set; }

    /// <summary>비고.</summary>
    public string Note { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public Guest Clone() => (Guest)MemberwiseClone();
}
