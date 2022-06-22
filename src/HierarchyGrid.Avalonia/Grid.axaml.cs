using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace HierarchyGrid.Avalonia;

public partial class Grid : UserControl // TODO ReactiveUserControl<HierarchyGridViewModel>
{
    public Grid()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load( this );
    }
}