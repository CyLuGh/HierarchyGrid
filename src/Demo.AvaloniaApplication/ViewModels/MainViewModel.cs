using HierarchyGrid.Definitions;
using ReactiveUI;
using System;
using System.Reactive;
using System.Reactive.Disposables;

namespace Demo.AvaloniaApplication.ViewModels;

public class MainViewModel : ViewModelBase
{
    public HierarchyGridViewModel DemoViewModel { get; } = new HierarchyGridViewModel { SelectionMode = SelectionMode.Single };

    public ReactiveCommand<Unit , HierarchyDefinitions> BuildSampleDefinitions { get; }

    public MainViewModel()
    {
        BuildSampleDefinitions = ReactiveCommand.CreateRunInBackground( () =>
        {
            var dg = new DataGenerator();
            return dg.GenerateSample();
        } );

        this.WhenActivated( disposables =>
        {
            BuildSampleDefinitions
                .WhereNotNull()
                .Subscribe( defs => { DemoViewModel.Set( defs ); } )
                .DisposeWith( disposables );
        } );
    }
}
