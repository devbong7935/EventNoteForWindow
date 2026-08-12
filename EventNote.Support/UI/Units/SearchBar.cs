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

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        if (_editor is not null) _editor.KeyDown -= OnEditorKeyDown;
        _editor = GetTemplateChild(PartEditor) as TextBox;
        if (_editor is not null) _editor.KeyDown += OnEditorKeyDown;
    }

    /// <summary>검색어 입력란에 포커스를 준다.</summary>
    public void FocusEditor() => _editor?.Focus();

    private void OnEditorKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;

        Execute();
        e.Handled = true;
    }

    private void Execute()
    {
        var command = SearchCommand;
        if (command is not null && command.CanExecute(Text)) command.Execute(Text);
    }
}
