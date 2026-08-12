using System.Windows;
using EventNote.Main.Local.ViewModels;

namespace EventNote.Main.UI.Views;

/// <summary>
/// 행사 추가 / 편집 대화상자. ViewModel 의 닫기 요청을 DialogResult 로 옮겨준다.
/// 겉모습은 Themes/Views/EventEditorWindow.xaml 이 맡는다.
/// </summary>
public class EventEditorWindow : Window
{
    private EventEditorViewModel? _viewModel;

    static EventEditorWindow()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(EventEditorWindow), new FrameworkPropertyMetadata(typeof(EventEditorWindow)));
    }

    public EventEditorWindow()
    {
        // WindowStartupLocation 은 의존 속성이 아니어서 스타일 Setter 로는 지정할 수 없다.
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (_viewModel is not null) _viewModel.RequestClose -= OnRequestClose;

        _viewModel = e.NewValue as EventEditorViewModel;
        if (_viewModel is not null) _viewModel.RequestClose += OnRequestClose;
    }

    private void OnRequestClose(object? sender, bool accepted)
    {
        DialogResult = accepted;
        Close();
    }
}
