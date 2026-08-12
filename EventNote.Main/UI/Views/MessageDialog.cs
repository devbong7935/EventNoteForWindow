using System.Windows;
using System.Windows.Input;
using EventNote.Main.Local.ViewModels;
using EventNote.Support.UI.Shell;

namespace EventNote.Main.UI.Views;

/// <summary>
/// MessageBox 를 대신하는 대화상자. 겉모습은 Themes/Views/MessageDialog.xaml 이 맡는다.
/// 버튼이 ItemsControl 로 만들어져 IsDefault / IsCancel 을 달 자리가 없으므로
/// Enter · Esc 는 여기서 직접 받는다.
/// </summary>
public class MessageDialog : ChromeWindow
{
    private MessageDialogViewModel? _viewModel;

    static MessageDialog()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(MessageDialog), new FrameworkPropertyMetadata(typeof(MessageDialog)));
    }

    public MessageDialog()
    {
        // WindowStartupLocation 은 의존 속성이 아니어서 스타일 Setter 로는 지정할 수 없다.
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        DataContextChanged += OnDataContextChanged;
    }

    /// <summary>사용자가 고른 답. 창을 닫기 전에 채워 둔다.</summary>
    public MessageDialogResult Result { get; private set; } = MessageDialogResult.Cancel;

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);
        if (_viewModel is null || e.Handled) return;

        switch (e.Key)
        {
            case Key.Enter:
                _viewModel.Accept();
                e.Handled = true;
                break;

            case Key.Escape:
                _viewModel.Cancel();
                e.Handled = true;
                break;
        }
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (_viewModel is not null) _viewModel.RequestClose -= OnRequestClose;

        _viewModel = e.NewValue as MessageDialogViewModel;
        if (_viewModel is not null) _viewModel.RequestClose += OnRequestClose;
    }

    private void OnRequestClose(object? sender, MessageDialogResult result)
    {
        Result = result;
        DialogResult = true;
        Close();
    }
}
