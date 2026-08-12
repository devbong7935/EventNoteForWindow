using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using EventNote.Core.Models;
using EventNote.Core.Text;

namespace EventNote.Main.Local.ViewModels;

/// <summary>표에 한 줄로 표시되는 하객. 값을 바꾸면 곧바로 모델에 반영된다.</summary>
public partial class GuestViewModel : ObservableObject
{
    public GuestViewModel(Guest model) => Model = model;

    public GuestViewModel() : this(new Guest()) { }

    /// <summary>저장 대상 원본.</summary>
    public Guest Model { get; }

    /// <summary>표에 보이는 순번. 목록이 바뀔 때마다 다시 매긴다.</summary>
    [ObservableProperty]
    private int _no;

    public string Name
    {
        get => Model.Name;
        set => SetProperty(Model.Name, value, Model, (m, v) => m.Name = v);
    }

    public long Amount
    {
        get => Model.Amount;
        set
        {
            if (!SetProperty(Model.Amount, value, Model, (m, v) => m.Amount = v)) return;
            OnPropertyChanged(nameof(AmountText));
            OnPropertyChanged(nameof(AmountInHangul));
        }
    }

    /// <summary>표에서 편집하는 금액 문자열. "50,000" 처럼 쉼표가 섞여도 받아준다.</summary>
    public string AmountText
    {
        get => Amount == 0 ? string.Empty : Amount.ToString("N0", CultureInfo.CurrentCulture);
        set => Amount = ParseAmount(value);
    }

    /// <summary>금액의 한글 표기(읽기 전용).</summary>
    public string AmountInHangul => KoreanCurrency.ToHangul(Amount);

    public string Relation
    {
        get => Model.Relation;
        set => SetProperty(Model.Relation, value, Model, (m, v) => m.Relation = v);
    }

    public string Affiliation
    {
        get => Model.Affiliation;
        set => SetProperty(Model.Affiliation, value, Model, (m, v) => m.Affiliation = v);
    }

    public int MealTickets
    {
        get => Model.MealTickets;
        set
        {
            if (!SetProperty(Model.MealTickets, value < 0 ? 0 : value, Model, (m, v) => m.MealTickets = v)) return;
            OnPropertyChanged(nameof(MealTicketsText));
        }
    }

    /// <summary>
    /// 표에서 편집하는 식권 매수. int 에 직접 묶으면 칸을 비웠을 때 변환에 실패해
    /// 빨간 테두리가 뜨므로, 문자열로 받아 비었거나 숫자가 아니면 0 으로 본다.
    /// </summary>
    public string MealTicketsText
    {
        get => MealTickets.ToString(CultureInfo.CurrentCulture);
        set => MealTickets = ParseCount(value);
    }

    public string Note
    {
        get => Model.Note;
        set => SetProperty(Model.Note, value, Model, (m, v) => m.Note = v);
    }

    /// <summary>검색어가 이 하객의 지정한 항목에 걸리는지 본다.</summary>
    public bool Matches(GuestSearchField field, string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword)) return true;

        var needle = keyword.Trim();
        return field switch
        {
            GuestSearchField.Name => Contains(Name, needle),
            GuestSearchField.Affiliation => Contains(Affiliation, needle),
            GuestSearchField.Relation => Contains(Relation, needle),
            GuestSearchField.Note => Contains(Note, needle),
            _ => Contains(Name, needle) || Contains(Affiliation, needle)
                 || Contains(Relation, needle) || Contains(Note, needle),
        };
    }

    private static bool Contains(string source, string needle)
        => !string.IsNullOrEmpty(source)
           && source.Contains(needle, StringComparison.CurrentCultureIgnoreCase);

    private static int ParseCount(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return 0;

        var digits = new string(text.Where(char.IsDigit).ToArray());
        return int.TryParse(digits, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : 0;
    }

    private static long ParseAmount(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return 0;

        // 숫자와 부호만 남기고 지운다. "5만" 같은 입력은 5 로 읽지 않고 무시한다.
        var digits = new string(text.Where(c => char.IsDigit(c) || c == '-').ToArray());
        return long.TryParse(digits, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value
            : 0;
    }
}

/// <summary>하객 검색 기준.</summary>
public enum GuestSearchField
{
    Name,
    Affiliation,
    Relation,
    Note,
    All,
}

public static class GuestSearchFieldExtensions
{
    public static string ToDisplayName(this GuestSearchField field) => field switch
    {
        GuestSearchField.Name => "이름",
        GuestSearchField.Affiliation => "소속",
        GuestSearchField.Relation => "관계",
        GuestSearchField.Note => "비고",
        _ => "전체",
    };
}
