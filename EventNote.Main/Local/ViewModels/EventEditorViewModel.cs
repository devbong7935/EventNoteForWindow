using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EventNote.Core.Models;

namespace EventNote.Main.Local.ViewModels;

/// <summary>콤보 박스에 한글 이름으로 보여주기 위한 행사 종류 항목.</summary>
public sealed record CategoryOption(EventCategory Value, string Name);

/// <summary>행사 추가 / 편집 창의 ViewModel.</summary>
public partial class EventEditorViewModel : ObservableObject
{
    /// <summary>분 콤보의 기본 눈금. 목록에 없는 값이 들어오면 그 값을 끼워 넣는다.</summary>
    private static readonly int[] DefaultMinuteSteps = { 0, 5, 10, 15, 20, 25, 30, 35, 40, 45, 50, 55 };

    private EventEditorViewModel(bool isNew)
    {
        IsNew = isNew;
        CategoryOptions = EventCategoryExtensions.All
            .Select(c => new CategoryOption(c, c.ToDisplayName()))
            .ToList();
        _selectedCategory = CategoryOptions[0];

        Hours = Enumerable.Range(0, 24).ToList();
        Minutes = new ObservableCollection<int>(DefaultMinuteSteps);
    }

    /// <summary>창을 닫아달라는 요청. true 면 저장.</summary>
    public event EventHandler<bool>? RequestClose;

    public bool IsNew { get; }

    public string DialogTitle => IsNew ? "행사 추가" : "행사 편집";

    public IReadOnlyList<CategoryOption> CategoryOptions { get; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _title = string.Empty;

    [ObservableProperty]
    private CategoryOption _selectedCategory;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _hostName = string.Empty;

    [ObservableProperty]
    private DateTime _eventDate = DateTime.Today;

    /// <summary>시각 선택용 0~23.</summary>
    public IReadOnlyList<int> Hours { get; }

    /// <summary>시각 선택용 분.</summary>
    public ObservableCollection<int> Minutes { get; }

    [ObservableProperty]
    private int _selectedHour;

    [ObservableProperty]
    private int _selectedMinute;

    [ObservableProperty]
    private string _venue = string.Empty;

    /// <summary>식권을 쓰는 행사인지. 끄면 식대 단가 입력칸이 사라진다.</summary>
    [ObservableProperty]
    private bool _usesMealTickets = true;

    [ObservableProperty]
    private string _mealPriceText = string.Empty;

    [ObservableProperty]
    private string _memo = string.Empty;

    /// <summary>사용자가 식권 여부를 직접 건드렸는지. 건드린 뒤에는 종류를 바꿔도 손대지 않는다.</summary>
    private bool _mealTicketChoiceTouched;

    /// <summary>종류에 맞춰 기본값을 넣는 중. 이때의 변경은 '사용자가 건드림'으로 세지 않는다.</summary>
    private bool _applyingCategoryDefault;

    public static EventEditorViewModel ForNew()
    {
        // 새 행사는 낮 12시를 기본값으로 둔다. 대부분의 경조사가 점심 무렵이다.
        var vm = new EventEditorViewModel(isNew: true);
        vm.SetTime(new TimeSpan(12, 0, 0));
        vm.ApplyCategoryDefault();
        return vm;
    }

    public static EventEditorViewModel ForEdit(CeremonyEvent model)
    {
        var vm = new EventEditorViewModel(isNew: false)
        {
            Title = model.Title,
            HostName = model.HostName,
            EventDate = model.EventDate.Date,
            Venue = model.Venue,
            MealPriceText = model.MealPrice == 0 ? string.Empty : model.MealPrice.ToString("N0"),
            Memo = model.Memo,
        };
        vm.SelectedCategory = vm.CategoryOptions.First(c => c.Value == model.Category);
        vm.SetTime(model.EventDate.TimeOfDay);

        // 이미 저장된 값이 있으므로 종류를 바꿔도 마음대로 뒤집지 않는다.
        vm.UsesMealTickets = model.UsesMealTickets;
        vm._mealTicketChoiceTouched = true;
        return vm;
    }

    /// <summary>입력값을 모델에 옮겨 담는다.</summary>
    public void ApplyTo(CeremonyEvent model)
    {
        model.Title = Title.Trim();
        model.Category = SelectedCategory.Value;
        model.HostName = HostName.Trim();
        model.EventDate = EventDate.Date.AddHours(SelectedHour).AddMinutes(SelectedMinute);
        model.Venue = Venue.Trim();
        model.UsesMealTickets = UsesMealTickets;
        model.MealPrice = ParseMoney(MealPriceText);
        model.Memo = Memo.Trim();
        model.ModifiedAt = DateTime.Now;
    }

    /// <summary>사용자가 식권 여부를 직접 정하지 않았으면 종류에 맞는 기본값을 넣는다.</summary>
    private void ApplyCategoryDefault()
    {
        if (_mealTicketChoiceTouched) return;

        _applyingCategoryDefault = true;
        try
        {
            UsesMealTickets = SelectedCategory.Value.UsesMealTicketsByDefault();
        }
        finally
        {
            _applyingCategoryDefault = false;
        }
    }

    partial void OnSelectedCategoryChanged(CategoryOption value) => ApplyCategoryDefault();

    partial void OnUsesMealTicketsChanged(bool value)
    {
        if (!_applyingCategoryDefault) _mealTicketChoiceTouched = true;
    }

    /// <summary>시/분 콤보를 주어진 시각으로 맞춘다. 눈금에 없는 분이면 목록에 끼워 넣는다.</summary>
    private void SetTime(TimeSpan time)
    {
        SelectedHour = time.Hours;

        if (!Minutes.Contains(time.Minutes))
        {
            var index = Minutes.Count(m => m < time.Minutes);
            Minutes.Insert(index, time.Minutes);
        }

        SelectedMinute = time.Minutes;
    }

    private bool CanSave() => !string.IsNullOrWhiteSpace(Title) || !string.IsNullOrWhiteSpace(HostName);

    [RelayCommand(CanExecute = nameof(CanSave))]
    private void Save() => RequestClose?.Invoke(this, true);

    [RelayCommand]
    private void Cancel() => RequestClose?.Invoke(this, false);

    private static long ParseMoney(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return 0;
        var digits = new string(text.Where(char.IsDigit).ToArray());
        return long.TryParse(digits, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : 0;
    }
}
