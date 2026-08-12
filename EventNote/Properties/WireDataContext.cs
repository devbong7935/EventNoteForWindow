using System.Windows;
using EventNote.Forms.UI.Views;
using EventNote.Main.Local.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace EventNote.Properties;

/// <summary>
/// 뷰와 ViewModel 을 짝지어 준다.
/// 뷰가 자기 ViewModel 을 직접 알지 않도록, 연결은 여기 한 곳에 모은다.
/// </summary>
internal sealed class WireDataContext
{
    private readonly Dictionary<Type, Type> _pairs = [];

    public WireDataContext()
    {
        Register<MainWindow, MainViewModel>(); // ⬅️ 여기에 추가
    }

    private void Register<TView, TViewModel>() where TView : FrameworkElement
        => _pairs[typeof(TView)] = typeof(TViewModel);

    /// <summary>등록된 짝이 있으면 DataContext 를 붙인다.</summary>
    public void Apply(FrameworkElement view, IServiceProvider services)
    {
        if (_pairs.TryGetValue(view.GetType(), out var viewModel))
        {
            view.DataContext = services.GetRequiredService(viewModel);
        }
    }
}
