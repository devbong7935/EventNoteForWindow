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
        => Show(MessageDialogViewModel.Confirm(message, title)) == MessageDialogResult.Yes;

    public void Info(string message, string title = "알림")
        => Show(MessageDialogViewModel.Info(message, title));

    public void Error(string message, string title = "오류")
        => Show(MessageDialogViewModel.Error(message, title));

    public ImportMode AskImportMode(string message)
        => Show(MessageDialogViewModel.Import(message)) switch
        {
            MessageDialogResult.Yes => ImportMode.Merge,
            MessageDialogResult.No => ImportMode.Replace,
            _ => ImportMode.Cancel,
        };

    /// <summary>MessageBox 대신 쓰는 앱 자체 대화상자. 창을 띄우고 고른 답을 돌려준다.</summary>
    private MessageDialogResult Show(MessageDialogViewModel viewModel)
    {
        var dialog = _services.GetRequiredService<MessageDialog>();
        dialog.Owner = ActiveOwner;
        dialog.DataContext = viewModel;
        dialog.ShowDialog();
        return dialog.Result;
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

    private static Window? ActiveOwner =>
        Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive)
        ?? Application.Current.MainWindow;
}
