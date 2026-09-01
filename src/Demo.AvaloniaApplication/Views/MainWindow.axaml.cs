using Demo.AvaloniaApplication.ViewModels;
using ReactiveUI.Avalonia;

namespace Demo.AvaloniaApplication.Views;

public partial class MainWindow : ReactiveWindow<MainViewModel>
{
    public MainWindow()
    {
        InitializeComponent();
    }
}
