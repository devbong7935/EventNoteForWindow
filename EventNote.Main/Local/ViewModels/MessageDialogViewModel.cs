using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EventNote.Support.UI.Units;

namespace EventNote.Main.Local.ViewModels;

/// <summary>메시지 대화상자의 성격. 왼쪽 아이콘 모양과 색이 이걸 따라간다.</summary>
public enum MessageDialogKind
{
    Info,
    Question,
    Error,
}

/// <summary>메시지 대화상자에서 사용자가 고른 답.</summary>
public enum MessageDialogResult
{
    Yes,
    No,
    Cancel,
}

/// <summary>대화상자 아래에 놓일 버튼 하나.</summary>
public sealed record DialogChoice(string Text, MessageDialogResult Result, AccentButtonKind Kind);

/// <summary>
/// MessageBox 를 대신하는 대화상자의 ViewModel.
/// 버튼은 개수도 문구도 고정이 아니다. 부르는 쪽이 필요한 만큼 넣는다.
/// </summary>
public partial class MessageDialogViewModel : ObservableObject
{
    private MessageDialogViewModel(
        string title, string message, MessageDialogKind kind, IReadOnlyList<DialogChoice> choices)
    {
        Title = title;
        Message = message;
        Kind = kind;
        Choices = choices;
    }

    /// <summary>창을 닫아달라는 요청. 고른 답을 함께 보낸다.</summary>
    public event EventHandler<MessageDialogResult>? RequestClose;

    public string Title { get; }

    public string Message { get; }

    public MessageDialogKind Kind { get; }

    public IReadOnlyList<DialogChoice> Choices { get; }

    /// <summary>Enter 로 고를 답. 목록의 첫 버튼이다.</summary>
    public DialogChoice DefaultChoice => Choices[0];

    /// <summary>Esc 로 고를 답. 취소가 없으면 마지막 버튼으로 물러난다.</summary>
    public DialogChoice CancelChoice =>
        Choices.FirstOrDefault(c => c.Result == MessageDialogResult.Cancel)
        ?? Choices.FirstOrDefault(c => c.Result == MessageDialogResult.No)
        ?? Choices[^1];

    public static MessageDialogViewModel Info(string message, string title)
        => new(title, message, MessageDialogKind.Info,
            new[] { new DialogChoice("확인", MessageDialogResult.Yes, AccentButtonKind.Primary) });

    public static MessageDialogViewModel Error(string message, string title)
        => new(title, message, MessageDialogKind.Error,
            new[] { new DialogChoice("확인", MessageDialogResult.Yes, AccentButtonKind.Primary) });

    public static MessageDialogViewModel Confirm(string message, string title)
        => new(title, message, MessageDialogKind.Question, new[]
        {
            new DialogChoice("예", MessageDialogResult.Yes, AccentButtonKind.Primary),
            new DialogChoice("아니오", MessageDialogResult.No, AccentButtonKind.Neutral),
        });

    /// <summary>
    /// 가져오기처럼 답이 셋인 경우. MessageBox 시절에는 예/아니오/취소 가 무슨 뜻인지
    /// 본문에 따로 적어 줘야 했지만, 이제는 버튼에 그대로 쓴다.
    /// </summary>
    public static MessageDialogViewModel Import(string message)
        => new("데이터 가져오기", message, MessageDialogKind.Question, new[]
        {
            new DialogChoice("현재 목록에 합치기", MessageDialogResult.Yes, AccentButtonKind.Primary),
            new DialogChoice("모두 지우고 교체", MessageDialogResult.No, AccentButtonKind.Danger),
            new DialogChoice("취소", MessageDialogResult.Cancel, AccentButtonKind.Neutral),
        });

    [RelayCommand]
    private void Choose(DialogChoice? choice)
        => RequestClose?.Invoke(this, choice?.Result ?? MessageDialogResult.Cancel);

    public void Cancel() => RequestClose?.Invoke(this, CancelChoice.Result);

    public void Accept() => RequestClose?.Invoke(this, DefaultChoice.Result);
}
