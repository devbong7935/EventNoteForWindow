using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using EventNote.Core.Models;

namespace EventNote.Main.Local.ViewModels;

/// <summary>왼쪽 행사 목록의 한 항목이자, 그 행사의 하객 명부를 들고 있는 컨테이너.</summary>
public partial class EventItemViewModel : ObservableObject
{
    public EventItemViewModel(CeremonyEvent model)
    {
        Model = model;
        Guests = new ObservableCollection<GuestViewModel>(
            model.Guests.Select(g => new GuestViewModel(g)));
        Renumber();
    }

    /// <summary>저장 대상 원본.</summary>
    public CeremonyEvent Model { get; }

    public ObservableCollection<GuestViewModel> Guests { get; }

    public string Title => Model.DisplayTitle;

    public string Glyph => Model.Category.ToGlyph();

    public string CategoryName => Model.Category.ToDisplayName();

    public string DateText => Model.DateDisplay;

    public string VenueText => string.IsNullOrWhiteSpace(Model.Venue) ? "-" : Model.Venue;

    /// <summary>목록에 작게 보여줄 요약. 예: 12명 · 1,250,000원.</summary>
    public string SummaryText => $"{Guests.Count:N0}명 · {Guests.Sum(g => g.Amount):N0}원";

    public long TotalAmount => Guests.Sum(g => g.Amount);

    public int TotalTickets => Guests.Sum(g => g.MealTickets);

    /// <summary>이 행사가 식권을 쓰는지.</summary>
    public bool UsesMealTickets => Model.UsesMealTickets;

    /// <summary>식대를 뺀 금액. 식권을 쓰지 않으면 차감할 것이 없다.</summary>
    public long NetAmount => UsesMealTickets ? TotalAmount - TotalTickets * Model.MealPrice : TotalAmount;

    /// <summary>표의 No 열을 1부터 다시 매긴다.</summary>
    public void Renumber()
    {
        for (var i = 0; i < Guests.Count; i++) Guests[i].No = i + 1;
    }

    /// <summary>행사 정보가 바뀐 뒤 목록 표시를 갱신한다.</summary>
    public void RefreshHeader()
    {
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(Glyph));
        OnPropertyChanged(nameof(CategoryName));
        OnPropertyChanged(nameof(DateText));
        OnPropertyChanged(nameof(VenueText));
        OnPropertyChanged(nameof(UsesMealTickets));
        RefreshTotals();
    }

    /// <summary>합계 표시를 갱신한다.</summary>
    public void RefreshTotals()
    {
        OnPropertyChanged(nameof(SummaryText));
        OnPropertyChanged(nameof(TotalAmount));
        OnPropertyChanged(nameof(TotalTickets));
        OnPropertyChanged(nameof(NetAmount));
    }

    /// <summary>저장 직전에 화면상의 하객 순서를 모델에 반영한다.</summary>
    public CeremonyEvent ToModel()
    {
        Model.Guests = Guests.Select(g => g.Model).ToList();
        return Model;
    }
}
