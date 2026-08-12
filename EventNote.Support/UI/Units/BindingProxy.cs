using System.Windows;

namespace EventNote.Support.UI.Units;

/// <summary>
/// DataGrid 의 열(DataGridColumn)에 ViewModel 값을 물려 주기 위한 징검다리.
///
/// 열은 시각 트리에 들어가지 않아 DataContext 를 물려받지 못한다. 그래서 열의 Visibility 같은
/// 속성에 {Binding 어쩌고} 를 바로 걸면 아무 일도 일어나지 않는다.
/// Freezable 은 리소스에 담겨도 상속 컨텍스트를 타므로, 여기에 DataContext 를 담아 두고
/// 열에서는 이 리소스를 Source 로 삼아 값을 가져온다.
///
///   &lt;DataGrid.Resources&gt;
///       &lt;units:BindingProxy x:Key="Proxy" Data="{Binding}" /&gt;
///   &lt;/DataGrid.Resources&gt;
///   ...
///   Visibility="{Binding Data.UsesMealTickets, Source={StaticResource Proxy}, Converter={StaticResource BoolToVis}}"
/// </summary>
public sealed class BindingProxy : Freezable
{
    public static readonly DependencyProperty DataProperty = DependencyProperty.Register(
        nameof(Data), typeof(object), typeof(BindingProxy), new UIPropertyMetadata(null));

    /// <summary>열에서 꺼내 쓸 값. 보통 화면의 DataContext 를 통째로 담는다.</summary>
    public object? Data
    {
        get => GetValue(DataProperty);
        set => SetValue(DataProperty, value);
    }

    protected override Freezable CreateInstanceCore() => new BindingProxy();
}
