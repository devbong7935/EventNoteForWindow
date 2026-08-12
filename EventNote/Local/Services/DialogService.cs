using System.IO;
using System.Windows;
using EventNote.Main.Local.Services;
using EventNote.Main.Local.ViewModels;
using EventNote.Main.UI.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;

namespace EventNote.Local.Services;

/// <inheritdoc />
public sealed class DialogService : IDialogService
{
    private readonly IServiceProvider _services;

    public DialogService(IServiceProvider services) => _services = services;

    public bool ShowEventEditor(EventEditorViewModel viewModel)
    {
        var window = _services.GetRequiredService<EventEditorWindow>();
        window.Owner = ActiveOwner;
        window.DataContext = viewModel;
        return window.ShowDialog() == true;
    }

    public bool Confirm(string message, string title = "확인")
        => Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;

    public void Info(string message, string title = "알림")
        => Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);

    public void Error(string message, string title = "오류")
        => Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);

    private static MessageBoxResult Show(string message, string title, MessageBoxButton button, MessageBoxImage icon)
    {
        var owner = ActiveOwner;
        return owner is null
            ? MessageBox.Show(message, title, button, icon)
            : MessageBox.Show(owner, message, title, button, icon);
    }

    public string? AskSaveFilePath(string defaultFileName, string filter = "Excel 통합 문서 (*.xlsx)|*.xlsx")
    {
        var dialog = new SaveFileDialog
        {
            FileName = defaultFileName,
            Filter = filter,
            DefaultExt = Path.GetExtension(defaultFileName),
            AddExtension = true,
            OverwritePrompt = true,
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        };

        return dialog.ShowDialog(ActiveOwner) == true ? dialog.FileName : null;
    }

    public string? AskOpenFilePath(string filter, string title = "파일 열기")
    {
        var dialog = new OpenFileDialog
        {
            Filter = filter,
            Title = title,
            CheckFileExists = true,
            Multiselect = false,
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        };

        return dialog.ShowDialog(ActiveOwner) == true ? dialog.FileName : null;
    }

    public ImportMode AskImportMode(string message)
        => Show(message, "데이터 가져오기", MessageBoxButton.YesNoCancel, MessageBoxImage.Question) switch
        {
            MessageBoxResult.Yes => ImportMode.Merge,
            MessageBoxResult.No => ImportMode.Replace,
            _ => ImportMode.Cancel,
        };

    private static Window? ActiveOwner =>
        Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive)
        ?? Application.Current.MainWindow;
}
