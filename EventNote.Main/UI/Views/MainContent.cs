using System.Windows;
using System.Windows.Controls;

namespace EventNote.Main.UI.Views;

/// <summary>
/// 셸 창 안에 들어가는 본문. 행사 목록 · 하객 명부 · 집계를 한 화면에 보여 준다.
/// 겉모습은 Themes/Views/MainContent.xaml 이 맡는다.
/// </summary>
public class MainContent : ContentControl
{
    static MainContent()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(MainContent), new FrameworkPropertyMetadata(typeof(MainContent)));
    }
}
