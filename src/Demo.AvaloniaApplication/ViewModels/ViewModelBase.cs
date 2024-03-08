using ReactiveUI;

namespace Demo.AvaloniaApplication.ViewModels;

public class ViewModelBase : ReactiveObject, IActivatableViewModel
{
    public ViewModelActivator Activator { get; } = new();
}
