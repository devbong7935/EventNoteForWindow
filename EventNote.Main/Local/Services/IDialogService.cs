using EventNote.Main.Local.ViewModels;

namespace EventNote.Main.Local.Services;

/// <summary>가져온 데이터를 기존 목록에 어떻게 반영할지.</summary>
public enum ImportMode
{
    /// <summary>기존 목록에 합친다. 같은 행사는 파일 내용으로 갱신한다.</summary>
    Merge,

    /// <summary>기존 목록을 지우고 파일 내용으로 바꾼다.</summary>
    Replace,

    /// <summary>가져오지 않는다.</summary>
    Cancel,
}

/// <summary>ViewModel 이 창/대화상자를 직접 만들지 않도록 감싼 서비스.</summary>
public interface IDialogService
{
    /// <summary>행사 추가/편집 창을 띄운다. 저장을 누르면 true.</summary>
    bool ShowEventEditor(EventEditorViewModel viewModel);

    /// <summary>예/아니오 확인.</summary>
    bool Confirm(string message, string title = "확인");

    void Info(string message, string title = "알림");

    void Error(string message, string title = "오류");

    /// <summary>저장 위치를 묻는다. 취소하면 null.</summary>
    string? AskSaveFilePath(string defaultFileName, string filter = "Excel 통합 문서 (*.xlsx)|*.xlsx");

    /// <summary>열 파일을 묻는다. 취소하면 null.</summary>
    string? AskOpenFilePath(string filter, string title = "파일 열기");

    /// <summary>합칠지 교체할지 묻는다.</summary>
    ImportMode AskImportMode(string message);
}
