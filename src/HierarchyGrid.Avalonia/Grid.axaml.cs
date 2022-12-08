using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using HierarchyGrid.Definitions;
using ReactiveUI;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace HierarchyGrid.Avalonia;

public partial class Grid : ReactiveUserControl<HierarchyGridViewModel>
{
    public Grid()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        this.WhenActivated( disposables =>
        {
            this.WhenAnyValue( x => x.ViewModel )
                .WhereNotNull()
                .Do( vm => PopulateFromViewModel( this , vm , disposables ) )
                .Subscribe()
                .DisposeWith( disposables );
        } );
        AvaloniaXamlLoader.Load( this );
    }

    private static void PopulateFromViewModel( Grid view , HierarchyGridViewModel viewModel , CompositeDisposable disposables )
    {
        var hGrid = view.FindControl<ContentControl>( "HierarchyGrid" );

        if ( hGrid != null )
        {
            var page = new GridControl( viewModel );
            hGrid.Content = page;
        }
    }
}