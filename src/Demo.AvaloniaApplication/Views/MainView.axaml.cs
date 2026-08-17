using System;
using Avalonia.Controls;
using Demo.AvaloniaApplication.ViewModels;
using ReactiveUI;
using ReactiveUI.Avalonia;
using ReactiveUI.Primitives;
using ReactiveUI.Primitives.Disposables;

namespace Demo.AvaloniaApplication.Views;

public partial class MainView : ReactiveUserControl<MainViewModel>
{
    public MainView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            this.WhenAnyValue(x => x.ViewModel)
                .Where(x => x is not null)
                .Do(vm => PopulateFromViewModel(this, vm, disposables))
                .Subscribe()
                .DisposeWith(disposables);
        });
    }

    private static void PopulateFromViewModel(
        MainView view,
        MainViewModel viewModel,
        MultipleDisposable disposables
    )
    {
        view.OneWayBind(viewModel, vm => vm.DemoViewModel, v => v.HierarchyGrid.ViewModel)
            .DisposeWith(disposables);

        view.OneWayBind(viewModel, vm => vm.TestViewModel, v => v.HierarchyGridTest.ViewModel)
            .DisposeWith(disposables);

        view.BindCommand(viewModel, vm => vm.BuildSampleDefinitions, v => v.ButtonFill)
            .DisposeWith(disposables);

        view.BindCommand(viewModel, vm => vm.BuildTestDefinitions, v => v.ButtonFillTest)
            .DisposeWith(disposables);

        view.BindCommand(viewModel, vm => vm.SwitchTestTheme, v => v.ButtonSwitchTheme)
            .DisposeWith(disposables);

        view.BindCommand(viewModel, vm => vm.CycleRowHeights, v => v.ButtonToggleRowHeight)
            .DisposeWith(disposables);

        view.BindCommand(viewModel, vm => vm.CycleFontSizes, v => v.ButtonCycleFontSize)
            .DisposeWith(disposables);

        view.BindCommand(viewModel, vm => vm.TransposeGrid, v => v.ButtonTranspose)
            .DisposeWith(disposables);
    }
}
