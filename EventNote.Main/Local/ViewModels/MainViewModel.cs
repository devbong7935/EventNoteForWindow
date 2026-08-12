using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Data;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EventNote.Core.Models;
using EventNote.Core.Services;
using EventNote.Main.Local.Services;
using EventNote.Support.Local.Theming;

namespace EventNote.Main.Local.ViewModels;

/// <summary>검색 기준 콤보 항목.</summary>
public sealed record SearchFieldOption(GuestSearchField Value, string Name);

/// <summary>메인 화면. 행사 목록과 선택된 행사의 하객 명부를 함께 다룬다.</summary>
public partial class MainViewModel : ObservableObject
{
    private readonly IEventRepository _repository;
    private readonly IExcelExportService _excel;
    private readonly IEventArchiveService _archive;
    private readonly IDialogService _dialogs;
    private readonly IThemeService _theme;
    private readonly DispatcherTimer _autoSaveTimer;

    /// <summary>
    /// 바쁘다는 표시를 조금 늦춰 띄우기 위한 타이머.
    /// 행사가 몇 건 없으면 불러오기가 눈 깜짝할 새에 끝나는데, 그때마다 화면이 흐려졌다
    /// 돌아오면 그게 더 어수선하다. 이 시간 안에 끝난 일은 표시 없이 지나간다.
    /// </summary>
    private readonly DispatcherTimer _busyDelay;

    /// <summary>불러오는 중에는 변경 알림을 무시해 불필요한 저장을 막는다.</summary>
    private bool _suppressChangeTracking;

    public MainViewModel(
        IEventRepository repository,
        IExcelExportService excel,
        IEventArchiveService archive,
        IDialogService dialogs,
        IThemeService theme)
    {
        _repository = repository;
        _excel = excel;
        _archive = archive;
        _dialogs = dialogs;
        _theme = theme;

        // 속성이 아니라 필드에 직접 넣는다. 여기서 변경 알림이 돌면 시작하자마자 테마를 다시 입힌다.
        _isDarkTheme = theme.Current == AppThemeMode.Dark;

        SearchFields = Enum.GetValues<GuestSearchField>()
            .Select(f => new SearchFieldOption(f, f.ToDisplayName()))
            .ToList();
        _selectedSearchField = SearchFields[0];

        RelationPresets = GuestPresets.Relations;

        _autoSaveTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.5) };
        _autoSaveTimer.Tick += OnAutoSaveTick;

        _busyDelay = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        _busyDelay.Tick += OnBusyDelayTick;

        // 행사가 하나도 없으면 '전체 내보내기'도 눌리지 않아야 한다.
        Events.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(HasAnyEvent));
            ExportAllCommand.NotifyCanExecuteChanged();
        };
    }

    public ObservableCollection<EventItemViewModel> Events { get; } = new();

    public ObservableCollection<RelationSummary> Summary { get; } = new();

    public IReadOnlyList<SearchFieldOption> SearchFields { get; }

    /// <summary>관계 열 콤보 박스의 후보 값.</summary>
    public IReadOnlyList<string> RelationPresets { get; }

    /// <summary>데이터 파일 위치. 도움말에서 보여준다.</summary>
    public string StorePath => _repository.StorePath;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedEvent))]
    [NotifyPropertyChangedFor(nameof(SelectedEventHeader))]
    [NotifyPropertyChangedFor(nameof(SelectedEventSubHeader))]
    [NotifyPropertyChangedFor(nameof(UsesMealTickets))]
    [NotifyCanExecuteChangedFor(nameof(EditEventCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteEventCommand))]
    [NotifyCanExecuteChangedFor(nameof(AddGuestCommand))]
    [NotifyCanExecuteChangedFor(nameof(ExportCurrentCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteGuestsCommand))]
    [NotifyCanExecuteChangedFor(nameof(SearchCommand))]
    private EventItemViewModel? _selectedEvent;

    /// <summary>표에서 지금 선택된 하객. 여러 줄을 골랐을 때는 그중 하나가 들어온다.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DeleteGuestsCommand))]
    private GuestViewModel? _selectedGuest;

    [ObservableProperty]
    private ICollectionView? _guestsView;

    [ObservableProperty]
    private SearchFieldOption _selectedSearchField;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _statusText = "준비";

    [ObservableProperty]
    private bool _isBusy;

    /// <summary>
    /// 화면을 덮는 진행 표시를 지금 보여야 하는지. IsBusy 가 잠깐 켜졌다 꺼지는 경우는 걸러진다.
    /// </summary>
    [ObservableProperty]
    private bool _showBusyOverlay;

    /// <summary>설정 메뉴의 다크 모드 토글. 켜고 끄면 그 자리에서 화면 전체 색이 바뀐다.</summary>
    [ObservableProperty]
    private bool _isDarkTheme;

    public bool HasSelectedEvent => SelectedEvent is not null;

    /// <summary>
    /// 선택한 행사가 식권을 쓰는지. 식권 열 · 식권합계 열 · 식권 타일 · 식대 차감 타일이 이 값에 따라 나타났다 사라진다.
    /// </summary>
    public bool UsesMealTickets => SelectedEvent?.UsesMealTickets ?? false;

    /// <summary>행사를 고르고 표에서 하객까지 선택했을 때만 삭제할 수 있다.</summary>
    public bool CanDeleteGuests => SelectedEvent is not null && SelectedGuest is not null;

    public bool HasAnyEvent => Events.Count > 0;

    public string SelectedEventHeader => SelectedEvent?.Title ?? "행사를 선택하세요";

    public string SelectedEventSubHeader => SelectedEvent is null
        ? "왼쪽 목록에서 행사를 고르거나 새로 추가하세요."
        : $"{SelectedEvent.CategoryName} · {SelectedEvent.DateText} · {SelectedEvent.VenueText}";

    // 집계 타일 -------------------------------------------------------------

    public string TotalAmountText => (SelectedEvent?.TotalAmount ?? 0).ToString("N0");

    public string TotalGuestCountText => (SelectedEvent?.Guests.Count ?? 0).ToString("N0");

    public string TotalTicketText => (SelectedEvent?.TotalTickets ?? 0).ToString("N0");

    public string NetAmountText => (SelectedEvent?.NetAmount ?? 0).ToString("N0");

    /// <summary>화면에 반영되지 않은 변경이 있는지.</summary>
    public bool IsDirty { get; private set; }

    // 명령 -----------------------------------------------------------------

    [RelayCommand]
    private async Task LoadAsync()
    {
        try
        {
            IsBusy = true;
            _suppressChangeTracking = true;

            var stored = await _repository.LoadAsync();

            ReplaceEvents(stored);
            StatusText = Events.Count == 0
                ? "행사를 추가해 시작하세요."
                : $"행사 {Events.Count}건을 불러왔습니다.";
        }
        catch (Exception ex)
        {
            StatusText = "데이터를 읽지 못했습니다.";
            _dialogs.Error($"저장된 내용을 읽지 못했습니다.\n\n{ex.Message}");
        }
        finally
        {
            _suppressChangeTracking = false;
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void AddEvent()
    {
        var editor = EventEditorViewModel.ForNew();
        if (!_dialogs.ShowEventEditor(editor)) return;

        var model = new CeremonyEvent();
        editor.ApplyTo(model);

        var item = new EventItemViewModel(model);
        Events.Insert(0, item);
        SelectedEvent = item;
        MarkDirty();
        StatusText = $"'{item.Title}' 행사를 추가했습니다.";
    }

    [RelayCommand(CanExecute = nameof(HasSelectedEvent))]
    private void EditEvent()
    {
        if (SelectedEvent is null) return;

        var editor = EventEditorViewModel.ForEdit(SelectedEvent.Model);
        if (!_dialogs.ShowEventEditor(editor)) return;

        editor.ApplyTo(SelectedEvent.Model);
        SelectedEvent.RefreshHeader();
        OnPropertyChanged(nameof(SelectedEventHeader));
        OnPropertyChanged(nameof(SelectedEventSubHeader));
        OnPropertyChanged(nameof(UsesMealTickets));
        RefreshTotals();
        MarkDirty();
        StatusText = "행사 정보를 수정했습니다.";
    }

    [RelayCommand(CanExecute = nameof(HasSelectedEvent))]
    private void DeleteEvent()
    {
        if (SelectedEvent is null) return;

        var target = SelectedEvent;
        var message = $"'{target.Title}' 행사를 삭제할까요?\n하객 {target.Guests.Count}명의 기록도 함께 지워집니다.";
        if (!_dialogs.Confirm(message, "행사 삭제")) return;

        var index = Events.IndexOf(target);
        Events.Remove(target);
        SelectedEvent = Events.Count == 0 ? null : Events[Math.Min(index, Events.Count - 1)];
        MarkDirty();
        StatusText = "행사를 삭제했습니다.";
    }

    [RelayCommand(CanExecute = nameof(HasSelectedEvent))]
    private void AddGuest()
    {
        if (SelectedEvent is null) return;

        // 검색 중이면 새로 넣은 빈 행이 걸러져 보이지 않는다. 먼저 필터를 푼다.
        if (!string.IsNullOrWhiteSpace(SearchText)) SearchText = string.Empty;

        // 항상 빈 행으로 추가한다. 직전 하객의 값을 물려받지 않는다.
        SelectedEvent.Guests.Add(new GuestViewModel());
        StatusText = "하객을 추가했습니다. 표에서 바로 입력하세요.";
    }

    [RelayCommand(CanExecute = nameof(CanDeleteGuests))]
    private void DeleteGuests(IList? selection)
    {
        if (SelectedEvent is null) return;

        // 명령 인자로 표의 선택 항목이 넘어오지만, 없으면 현재 행 하나만 지운다.
        var targets = selection?.OfType<GuestViewModel>().ToList()
                      ?? new List<GuestViewModel>();
        if (targets.Count == 0 && SelectedGuest is not null) targets.Add(SelectedGuest);
        if (targets.Count == 0) return;

        var label = targets.Count == 1
            ? $"'{Display(targets[0])}' 님을"
            : $"선택한 {targets.Count}명을";
        if (!_dialogs.Confirm($"{label} 삭제할까요?", "하객 삭제")) return;

        foreach (var target in targets) SelectedEvent.Guests.Remove(target);
        StatusText = $"하객 {targets.Count}명을 삭제했습니다.";

        static string Display(GuestViewModel guest)
            => string.IsNullOrWhiteSpace(guest.Name) ? "(이름 없음)" : guest.Name;
    }

    [RelayCommand(CanExecute = nameof(HasSelectedEvent))]
    private void Search() => ApplyFilter();

    [RelayCommand]
    private void ClearSearch()
    {
        SearchText = string.Empty;
        ApplyFilter();
    }

    [RelayCommand(CanExecute = nameof(HasSelectedEvent))]
    private async Task ExportCurrentAsync()
    {
        if (SelectedEvent is null) return;

        var model = SelectedEvent.ToModel();
        var path = _dialogs.AskSaveFilePath(_excel.SuggestFileName(model));
        if (path is null) return;

        await RunExportAsync(() => _excel.ExportAsync(model, path), path);
    }

    [RelayCommand(CanExecute = nameof(HasAnyEvent))]
    private async Task ExportAllAsync()
    {
        if (Events.Count == 0) return;

        var path = _dialogs.AskSaveFilePath($"경조사_전체명부_{DateTime.Today:yyyyMMdd}.xlsx");
        if (path is null) return;

        var models = Events.Select(e => e.ToModel()).ToList();
        await RunExportAsync(() => _excel.ExportAsync(models, path), path);
    }

    /// <summary>행사 정보와 하객 명부 전체를 앱 전용 암호화 파일 하나로 내보낸다.</summary>
    [RelayCommand(CanExecute = nameof(HasAnyEvent))]
    private async Task ExportArchiveAsync()
    {
        if (Events.Count == 0) return;

        var path = _dialogs.AskSaveFilePath(_archive.SuggestFileName(), _archive.FileFilter);
        if (path is null) return;

        try
        {
            IsBusy = true;
            StatusText = "데이터 파일을 만드는 중...";

            var models = Events.Select(e => e.ToModel()).ToList();
            await _archive.ExportAsync(models, path);

            // 일은 여기서 끝났다. 아래 알림창이 뜨기 전에 진행 표시부터 걷는다.
            // 안 그러면 흐려진 화면 위에 "다 됐습니다" 라고 적힌 진행 표시가 남는다.
            IsBusy = false;
            StatusText = $"데이터를 내보냈습니다 · {Path.GetFileName(path)}";

            var guestCount = models.Sum(m => m.Guests.Count);
            _dialogs.Info(
                $"행사 {models.Count}건 · 하객 {guestCount}명을 내보냈습니다.\n\n{path}",
                "데이터 내보내기 완료");
        }
        catch (Exception ex)
        {
            StatusText = "내보내기 실패";
            _dialogs.Error($"데이터를 내보내지 못했습니다.\n\n{ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>다른 컴퓨터에서 내보낸 .enote 파일을 읽어 들인다.</summary>
    [RelayCommand]
    private async Task ImportArchiveAsync()
    {
        var path = _dialogs.AskOpenFilePath(_archive.FileFilter, "데이터 가져오기");
        if (path is null) return;

        ArchiveContent content;
        try
        {
            IsBusy = true;
            content = await _archive.ImportAsync(path);
        }
        catch (Exception ex)
        {
            StatusText = "가져오기 실패";
            _dialogs.Error($"파일을 읽지 못했습니다.\n\n{ex.Message}");
            return;
        }
        finally
        {
            IsBusy = false;
        }

        if (content.Events.Count == 0)
        {
            _dialogs.Info("이 파일에는 행사가 들어 있지 않습니다.");
            return;
        }

        var mode = Events.Count == 0 ? ImportMode.Replace : AskHowToImport(content.Manifest);
        if (mode == ImportMode.Cancel)
        {
            StatusText = "가져오기를 취소했습니다.";
            return;
        }

        var models = mode == ImportMode.Replace ? content.Events.ToList() : Merge(content.Events);
        ReplaceEvents(models);
        await SaveNowAsync();

        StatusText = mode == ImportMode.Replace
            ? $"행사 {content.Events.Count}건으로 교체했습니다."
            : $"행사 {content.Events.Count}건을 합쳤습니다. (현재 {Events.Count}건)";
    }

    [RelayCommand]
    private async Task SaveNowAsync()
    {
        _autoSaveTimer.Stop();

        try
        {
            await _repository.SaveAsync(Events.Select(e => e.ToModel()));
            IsDirty = false;
            StatusText = $"저장됨 · {DateTime.Now:HH:mm:ss}";
        }
        catch (Exception ex)
        {
            StatusText = "저장 실패";
            _dialogs.Error($"저장하지 못했습니다.\n\n{ex.Message}");
        }
    }

    /// <summary>
    /// 창을 닫아도 되는지 사용자에게 묻는다. 닫기로 하면 자동 저장 타이머를 기다리지 않고
    /// 마지막 변경까지 저장한 뒤 true 를 돌려준다.
    /// </summary>
    public async Task<bool> ConfirmCloseAsync()
    {
        if (!_dialogs.Confirm("경조사 명부를 종료할까요?", "종료 확인")) return false;

        if (IsDirty) await SaveNowAsync();
        return true;
    }

    [RelayCommand]
    private void OpenDataFolder()
    {
        var folder = Path.GetDirectoryName(_repository.StorePath);
        if (folder is null || !Directory.Exists(folder)) return;

        Process.Start(new ProcessStartInfo("explorer.exe", $"\"{folder}\"") { UseShellExecute = true });
    }

    [RelayCommand]
    private void ShowAbout()
        => _dialogs.Info(
            "경조사 명부 (EventNote)\n\n" +
            "행사별로 하객 명부와 경조사금을 정리하고 엑셀로 내보냅니다.\n\n" +
            $"데이터 파일: {_repository.StorePath}\n" +
            "이 파일은 앱 전용 형식으로 암호화되어 있어 메모장·엑셀 등으로는 내용을 볼 수 없습니다.\n\n" +
            "다른 컴퓨터로 옮기려면 [파일 > 데이터 내보내기]로 .enote 파일을 만든 뒤,\n" +
            "그쪽 컴퓨터에서 [파일 > 데이터 가져오기]로 여세요.",
            "도움말");

    // 내부 처리 -------------------------------------------------------------

    private ImportMode AskHowToImport(ArchiveManifest manifest)
    {
        var source = string.IsNullOrWhiteSpace(manifest.SourceMachine) ? "알 수 없음" : manifest.SourceMachine;
        // 어느 버튼이 무슨 뜻인지는 버튼 문구가 직접 말한다. 본문에서 다시 설명하지 않는다.
        return _dialogs.AskImportMode(
            $"불러올 파일에는 행사 {manifest.EventCount}건 · 하객 {manifest.GuestCount}명이 들어 있습니다.\n" +
            $"내보낸 시각: {manifest.ExportedAt:yyyy-MM-dd HH:mm} (컴퓨터: {source})\n\n" +
            $"현재 이 프로그램에는 행사 {Events.Count}건이 있습니다.\n" +
            "합치면 같은 행사는 파일 내용으로 바뀝니다.");
    }

    /// <summary>같은 행사(Id 기준)는 가져온 내용으로 바꾸고, 없던 행사는 더한다.</summary>
    private List<CeremonyEvent> Merge(IReadOnlyList<CeremonyEvent> imported)
    {
        var merged = Events.Select(e => e.ToModel()).ToList();

        foreach (var incoming in imported)
        {
            var index = merged.FindIndex(m => m.Id == incoming.Id);
            if (index >= 0) merged[index] = incoming;
            else merged.Add(incoming);
        }

        return merged;
    }

    /// <summary>행사 목록을 통째로 갈아 끼운다. 날짜 최신순으로 정렬한다.</summary>
    private void ReplaceEvents(IEnumerable<CeremonyEvent> models)
    {
        // 먼저 선택을 풀어 이전 행사에 걸린 변경 알림을 정리한다.
        SelectedEvent = null;
        Events.Clear();

        foreach (var model in models.OrderByDescending(e => e.EventDate).ThenBy(e => e.DisplayTitle))
            Events.Add(new EventItemViewModel(model));

        SelectedEvent = Events.FirstOrDefault();
    }

    partial void OnSelectedEventChanged(EventItemViewModel? oldValue, EventItemViewModel? newValue)
    {
        if (oldValue is not null) Detach(oldValue);
        if (newValue is not null) Attach(newValue);

        // 다른 행사로 옮기면 이전 행사에서 고른 하객은 더 이상 유효하지 않다.
        SelectedGuest = null;

        GuestsView = newValue is null ? null : CollectionViewSource.GetDefaultView(newValue.Guests);
        ApplyFilter();
        RefreshSummary();
    }

    partial void OnIsDarkThemeChanged(bool value)
        => _theme.Apply(value ? AppThemeMode.Dark : AppThemeMode.Light);

    partial void OnIsBusyChanged(bool value)
    {
        _busyDelay.Stop();

        // 끝났으면 바로 걷는다. 시작은 잠깐 기다렸다가 아직도 바쁠 때만 띄운다.
        if (value) _busyDelay.Start();
        else ShowBusyOverlay = false;
    }

    private void OnBusyDelayTick(object? sender, EventArgs e)
    {
        _busyDelay.Stop();
        ShowBusyOverlay = IsBusy;
    }

    partial void OnSelectedSearchFieldChanged(SearchFieldOption value) => ApplyFilter();

    partial void OnSearchTextChanged(string value)
    {
        // 검색어를 비우면 즉시 전체 목록으로 되돌린다.
        if (string.IsNullOrWhiteSpace(value)) ApplyFilter();
    }

    private void Attach(EventItemViewModel item)
    {
        item.Guests.CollectionChanged += OnGuestsCollectionChanged;
        foreach (var guest in item.Guests) guest.PropertyChanged += OnGuestPropertyChanged;
    }

    private void Detach(EventItemViewModel item)
    {
        item.Guests.CollectionChanged -= OnGuestsCollectionChanged;
        foreach (var guest in item.Guests) guest.PropertyChanged -= OnGuestPropertyChanged;
    }

    private void OnGuestsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        foreach (var guest in e.OldItems?.OfType<GuestViewModel>() ?? Enumerable.Empty<GuestViewModel>())
            guest.PropertyChanged -= OnGuestPropertyChanged;

        foreach (var guest in e.NewItems?.OfType<GuestViewModel>() ?? Enumerable.Empty<GuestViewModel>())
            guest.PropertyChanged += OnGuestPropertyChanged;

        SelectedEvent?.Renumber();
        RefreshSummary();
        MarkDirty();
    }

    private void OnGuestPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(GuestViewModel.No)) return;

        RefreshSummary();
        MarkDirty();
    }

    private void ApplyFilter()
    {
        if (GuestsView is null) return;

        var field = SelectedSearchField.Value;
        var keyword = SearchText;

        GuestsView.Filter = string.IsNullOrWhiteSpace(keyword)
            ? null
            : item => item is GuestViewModel guest && guest.Matches(field, keyword);

        GuestsView.Refresh();
    }

    private void RefreshSummary()
    {
        Summary.Clear();
        if (SelectedEvent is not null)
        {
            foreach (var line in EventSummary.ByRelation(SelectedEvent.Guests.Select(g => g.Model)))
                Summary.Add(line);
        }

        SelectedEvent?.RefreshTotals();
        RefreshTotals();
    }

    private void RefreshTotals()
    {
        OnPropertyChanged(nameof(TotalAmountText));
        OnPropertyChanged(nameof(TotalGuestCountText));
        OnPropertyChanged(nameof(TotalTicketText));
        OnPropertyChanged(nameof(NetAmountText));
    }

    private async Task RunExportAsync(Func<Task> export, string path)
    {
        try
        {
            IsBusy = true;
            StatusText = "엑셀 파일을 만드는 중...";
            await export();

            // 아래 확인창이 뜨기 전에 진행 표시를 걷는다. finally 까지 두면 흐려진 화면 위에
            // 이미 끝났다는 문구를 띄운 진행 표시가 그대로 남는다.
            IsBusy = false;
            StatusText = $"엑셀로 저장했습니다 · {Path.GetFileName(path)}";

            if (_dialogs.Confirm($"엑셀 파일을 저장했습니다.\n\n{path}\n\n지금 열어볼까요?", "내보내기 완료"))
                Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            StatusText = "내보내기 실패";
            _dialogs.Error($"엑셀로 내보내지 못했습니다.\n\n{ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void MarkDirty()
    {
        if (_suppressChangeTracking) return;

        IsDirty = true;
        _autoSaveTimer.Stop();
        _autoSaveTimer.Start();
    }

    private async void OnAutoSaveTick(object? sender, EventArgs e)
    {
        _autoSaveTimer.Stop();
        await SaveNowAsync();
    }
}
