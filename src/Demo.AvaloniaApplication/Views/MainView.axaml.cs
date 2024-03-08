using Avalonia.Controls;
using Avalonia.ReactiveUI;
using Demo.AvaloniaApplication.ViewModels;
using ReactiveUI;
using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace Demo.AvaloniaApplication.Views;

public partial class MainView : ReactiveUserControl<MainViewModel>
{
    public MainView()
    {
        InitializeComponent();

        this.WhenActivated( disposables =>
        {
            this.WhenAnyValue( x => x.ViewModel )
                .WhereNotNull()
                .Do( vm => PopulateFromViewModel( this , vm , disposables ) )
                .Subscribe()
                .DisposeWith( disposables );
        } );
    }

    private static void PopulateFromViewModel( MainView view , MainViewModel viewModel , CompositeDisposable disposables )
    {
        view.OneWayBind( viewModel ,
            vm => vm.DemoViewModel ,
            v => v.HierarchyGrid.ViewModel )
            .DisposeWith( disposables );

        view.BindCommand( viewModel ,
            vm => vm.BuildSampleDefinitions ,
            v => v.ButtonFill )
            .DisposeWith( disposables );
    }
}
