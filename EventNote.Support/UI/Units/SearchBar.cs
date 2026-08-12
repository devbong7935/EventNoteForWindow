using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace EventNote.Support.UI.Units;

/// <summary>
/// "검색 기준 콤보 + 검색어 입력 + 검색 버튼" 한 벌. 목록 화면 어디서든 재사용한다.
/// Enter 키를 누르면 SearchCommand 가 실행된다.
/// </summary>
[TemplatePart(Name = PartEditor, Type = typeof(TextBox))]
public class SearchBar : Control
{
    private const string PartEditor = "PART_Editor";

    private TextBox? _editor;

    /// <summary>한글처럼 여러 번 눌러 한 글자를 만드는 중인지.</summary>
    private bool _composing;

    static SearchBar()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(SearchBar), new FrameworkPropertyMetadata(typeof(SearchBar)));
    }

    public static readonly DependencyProperty FieldItemsSourceProperty = DependencyProperty.Register(
        nameof(FieldItemsSource), typeof(IEnumerable), typeof(SearchBar),
        new FrameworkPropertyMetadata(null));

    /// <summary>검색 기준 목록(이름/소속/관계 등).</summary>
    public IEnumerable? FieldItemsSource
    {
        get => (IEnumerable?)GetValue(FieldItemsSourceProperty);
        set => SetValue(FieldItemsSourceProperty, value);
    }

    public static readonly DependencyProperty SelectedFieldProperty = DependencyProperty.Register(
        nameof(SelectedField), typeof(object), typeof(SearchBar),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    /// <summary>선택된 검색 기준.</summary>
    public object? SelectedField
    {
        get => GetValue(SelectedFieldProperty);
        set => SetValue(SelectedFieldProperty, value);
    }

    public static readonly DependencyProperty FieldDisplayMemberPathProperty = DependencyProperty.Register(
        nameof(FieldDisplayMemberPath), typeof(string), typeof(SearchBar),
        new FrameworkPropertyMetadata(string.Empty));

    /// <summary>검색 기준 항목에서 화면에 보여줄 속성 이름.</summary>
    public string FieldDisplayMemberPath
    {
        get => (string)GetValue(FieldDisplayMemberPathProperty);
        set => SetValue(FieldDisplayMemberPathProperty, value);
    }

    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
        nameof(Text), typeof(string), typeof(SearchBar),
        new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    /// <summary>검색어.</summary>
    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public static readonly DependencyProperty PlaceholderProperty = DependencyProperty.Register(
        nameof(Placeholder), typeof(string), typeof(SearchBar),
        new FrameworkPropertyMetadata(string.Empty));

    /// <summary>입력 전 흐리게 보여줄 안내 문구.</summary>
    public string Placeholder
    {
        get => (string)GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    public static readonly DependencyProperty SearchCommandProperty = DependencyProperty.Register(
        nameof(SearchCommand), typeof(ICommand), typeof(SearchBar),
        new FrameworkPropertyMetadata(null));

    public ICommand? SearchCommand
    {
        get => (ICommand?)GetValue(SearchCommandProperty);
        set => SetValue(SearchCommandProperty, value);
    }

    public static readonly DependencyProperty SearchButtonTextProperty = DependencyProperty.Register(
        nameof(SearchButtonText), typeof(string), typeof(SearchBar),
        new FrameworkPropertyMetadata("검색"));

    public string SearchButtonText
    {
        get => (string)GetValue(SearchButtonTextProperty);
        set => SetValue(SearchButtonTextProperty, value);
    }

    public static readonly DependencyProperty FieldSelectorWidthProperty = DependencyProperty.Register(
        nameof(FieldSelectorWidth), typeof(double), typeof(SearchBar),
        new FrameworkPropertyMetadata(84d));

    public double FieldSelectorWidth
    {
        get => (double)GetValue(FieldSelectorWidthProperty);
        set => SetValue(FieldSelectorWidthProperty, value);
    }

    private static readonly DependencyPropertyKey IsPlaceholderVisiblePropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(IsPlaceholderVisible), typeof(bool), typeof(SearchBar),
            new FrameworkPropertyMetadata(true));

    public static readonly DependencyProperty IsPlaceholderVisibleProperty =
        IsPlaceholderVisiblePropertyKey.DependencyProperty;

    /// <summary>
    /// 안내 문구를 지금 보여야 하는지.
    ///
    /// Text 가 비었는지만 보면 한글에서 어긋난다. IME 로 글자를 조합하는 동안에는
    /// 화면에 "ㄱ" 이 찍혀 있어도 TextBox.Text 는 아직 빈 문자열이라, 안내 문구가
    /// 입력한 글자 위에 겹쳐 남는다. 영문은 한 번에 확정되므로 티가 나지 않았다.
    /// 그래서 조합 중인지까지 함께 본다.
    /// </summary>
    public bool IsPlaceholderVisible => (bool)GetValue(IsPlaceholderVisibleProperty);

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        Detach(_editor);
        _editor = GetTemplateChild(PartEditor) as TextBox;
        Attach(_editor);

        UpdatePlaceholder();
    }

    private void Attach(TextBox? editor)
    {
        if (editor is null) return;

        editor.KeyDown += OnEditorKeyDown;
        editor.TextChanged += OnEditorTextChanged;
        editor.LostKeyboardFocus += OnEditorLostKeyboardFocus;

        // 조합의 시작 · 진행 · 확정을 모두 듣는다. TextChanged 만으로는 조합 중을 알 수 없다.
        TextCompositionManager.AddTextInputStartHandler(editor, OnComposing);
        TextCompositionManager.AddTextInputUpdateHandler(editor, OnComposing);
        TextCompositionManager.AddTextInputHandler(editor, OnComposed);
    }

    private void Detach(TextBox? editor)
    {
        if (editor is null) return;

        editor.KeyDown -= OnEditorKeyDown;
        editor.TextChanged -= OnEditorTextChanged;
        editor.LostKeyboardFocus -= OnEditorLostKeyboardFocus;

        TextCompositionManager.RemoveTextInputStartHandler(editor, OnComposing);
        TextCompositionManager.RemoveTextInputUpdateHandler(editor, OnComposing);
        TextCompositionManager.RemoveTextInputHandler(editor, OnComposed);
    }

    /// <summary>검색어 입력란에 포커스를 준다.</summary>
    public void FocusEditor() => _editor?.Focus();

    private void OnEditorKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;

        Execute();
        e.Handled = true;
    }

    private void OnComposing(object sender, TextCompositionEventArgs e)
    {
        _composing = !string.IsNullOrEmpty(e.TextComposition.CompositionText);
        UpdatePlaceholder();
    }

    private void OnComposed(object sender, TextCompositionEventArgs e)
    {
        // 글자가 확정됐다. 이어지는 TextChanged 가 실제 내용으로 다시 판단한다.
        _composing = false;
        UpdatePlaceholder();
    }

    private void OnEditorTextChanged(object sender, TextChangedEventArgs e) => UpdatePlaceholder();

    private void OnEditorLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        // 조합 도중에 다른 곳을 눌러 빠져나가면 확정 알림이 오지 않는다. 여기서 풀어 준다.
        _composing = false;
        UpdatePlaceholder();
    }

    private void UpdatePlaceholder()
        => SetValue(IsPlaceholderVisiblePropertyKey,
            !_composing && string.IsNullOrEmpty(_editor?.Text));

    private void Execute()
    {
        var command = SearchCommand;
        if (command is not null && command.CanExecute(Text)) command.Execute(Text);
    }
}
