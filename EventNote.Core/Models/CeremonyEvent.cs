namespace EventNote.Core.Models;

/// <summary>하나의 경조사 행사. 하객 명부를 통째로 가지고 있다.</summary>
public class CeremonyEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>행사 이름. 예: 홍길동 · 김영희 결혼식.</summary>
    public string Title { get; set; } = string.Empty;

    public EventCategory Category { get; set; } = EventCategory.Wedding;

    /// <summary>혼주 / 상주 등 주최자.</summary>
    public string HostName { get; set; } = string.Empty;

    public DateTime EventDate { get; set; } = DateTime.Today;

    /// <summary>장소.</summary>
    public string Venue { get; set; } = string.Empty;

    /// <summary>
    /// 이 행사에서 식권을 쓰는지. 끄면 식권 · 식대 단가 · 식대 차감 계산이 화면과 엑셀에서 모두 빠진다.
    /// 장례식처럼 식권을 나눠 주지 않는 행사를 위한 것이다.
    /// 예전에 저장한 파일에는 이 값이 없으므로 기본값 true 로 읽혀 하던 대로 동작한다.
    /// </summary>
    public bool UsesMealTickets { get; set; } = true;

    /// <summary>식대 단가(원). 총계에서 식권 비용을 차감할 때 쓴다.</summary>
    public long MealPrice { get; set; }

    public string Memo { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime ModifiedAt { get; set; } = DateTime.Now;

    public List<Guest> Guests { get; set; } = new();

    public long TotalAmount => Guests.Sum(g => g.Amount);

    public int TotalTickets => Guests.Sum(g => g.MealTickets);

    /// <summary>식대를 차감한 금액. 식권을 쓰지 않는 행사면 차감할 것이 없다.</summary>
    public long NetAmount => UsesMealTickets ? TotalAmount - TotalTickets * MealPrice : TotalAmount;

    /// <summary>
    /// 목록과 엑셀에 쓰는 날짜 표기. 시각이 00:00 이면 시간을 지정하지 않은 것으로 보고 날짜만 보여준다.
    /// </summary>
    public string DateDisplay => EventDate.TimeOfDay == TimeSpan.Zero
        ? EventDate.ToString("yyyy-MM-dd (ddd)")
        : EventDate.ToString("yyyy-MM-dd (ddd) HH:mm");

    /// <summary>이 행사에서 온 사람을 부르는 말. 결혼식이면 "하객", 장례식이면 "조문객".</summary>
    public string GuestTerm => Category.ToGuestTerm();

    /// <summary>제목이 비어 있으면 주최자/종류로 대체 표기.</summary>
    public string DisplayTitle =>
        !string.IsNullOrWhiteSpace(Title) ? Title
        : !string.IsNullOrWhiteSpace(HostName) ? $"{HostName} {Category.ToDisplayName()}"
        : Category.ToDisplayName();
}
