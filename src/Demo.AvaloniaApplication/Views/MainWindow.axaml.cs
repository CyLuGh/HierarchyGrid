using Avalonia.Controls;
using Avalonia.ReactiveUI;
using Demo.AvaloniaApplication.ViewModels;

namespace Demo.AvaloniaApplication.Views;

public partial class MainWindow : ReactiveWindow<MainViewModel>
{
    public MainWindow()
    {
        InitializeComponent();
    }
}
