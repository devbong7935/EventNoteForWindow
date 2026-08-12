namespace EventNote.Core.Models;

/// <summary>경조사 종류.</summary>
public enum EventCategory
{
    Wedding,        // 결혼식
    Funeral,        // 장례식
    FirstBirthday,  // 돌잔치
    SixtiethBirthday, // 환갑
    SeventiethBirthday, // 칠순
    EightiethBirthday,  // 팔순
    Opening,        // 개업식
    Etc,            // 기타
}

public static class EventCategoryExtensions
{
    public static string ToDisplayName(this EventCategory category) => category switch
    {
        EventCategory.Wedding => "결혼식",
        EventCategory.Funeral => "장례식",
        EventCategory.FirstBirthday => "돌잔치",
        EventCategory.SixtiethBirthday => "환갑",
        EventCategory.SeventiethBirthday => "칠순",
        EventCategory.EightiethBirthday => "팔순",
        EventCategory.Opening => "개업식",
        _ => "기타",
    };

    /// <summary>목록에서 행사 아이콘으로 사용할 이모지.</summary>
    public static string ToGlyph(this EventCategory category) => category switch
    {
        EventCategory.Wedding => "💍",
        EventCategory.Funeral => "🕯",
        EventCategory.FirstBirthday => "🎂",
        EventCategory.SixtiethBirthday => "🎉",
        EventCategory.SeventiethBirthday => "🎉",
        EventCategory.EightiethBirthday => "🎉",
        EventCategory.Opening => "🎊",
        _ => "📌",
    };

    /// <summary>
    /// 온 사람을 부르는 말. 장례식에 "하객"은 축하하러 온 손님이라는 뜻이라 쓸 수 없다.
    /// 돌잔치 · 환갑 · 개업식처럼 축하하는 자리는 모두 하객으로 묶는다.
    /// </summary>
    public static string ToGuestTerm(this EventCategory category)
        => category == EventCategory.Funeral ? "조문객" : "하객";

    /// <summary>
    /// 새 행사를 만들 때 식권을 쓸지 말지의 기본값.
    /// 장례식은 보통 식권을 나눠 주지 않는다. 어디까지나 기본값이라 사용자가 바꿀 수 있다.
    /// </summary>
    public static bool UsesMealTicketsByDefault(this EventCategory category)
        => category != EventCategory.Funeral;

    public static IReadOnlyList<EventCategory> All { get; } = Enum.GetValues<EventCategory>();
}
