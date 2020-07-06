using HierarchyGrid.Definitions;
using ReactiveUI;
using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace VirtualHierarchyGrid
{
    public partial class HierarchyGridCell
    {
        private static Thickness UnselectedThickness { get; } = new Thickness(1);
        private static Thickness SelectedThickness { get; } = new Thickness(2);

        public HierarchyGridCell()
        {
            InitializeComponent();

            this.WhenActivated(disposables =>
            {
                this.WhenAnyValue(x => x.ViewModel)
                    .WhereNotNull()
                    .Do(vm => PopulateFromViewModel(this, vm, disposables))
                    .SubscribeSafe()
                    .DisposeWith(disposables);
            });
        }

        private static void PopulateFromViewModel(HierarchyGridCell cell, HierarchyGridCellViewModel vm, CompositeDisposable disposables)
        {
            cell.OneWayBind(vm,
                vm => vm.ResultSet,
                v => v.TextBlockResult.Text,
                r => r?.Result)
                .DisposeWith(disposables);

            cell.OneWayBind(vm,
                vm => vm.IsHovered,
                v => v.CellBorder.Background,
                hovered => hovered ? Brushes.LightBlue : Brushes.LightGoldenrodYellow)
                .DisposeWith(disposables);

            cell.OneWayBind(vm,
                vm => vm.IsSelected,
                v => v.CellBorder.BorderBrush,
                selected => selected ? Brushes.Blue : Brushes.Gray)
                .DisposeWith(disposables);

            cell.OneWayBind(vm,
                vm => vm.IsSelected,
                v => v.CellBorder.BorderThickness,
                selected => selected ? SelectedThickness : UnselectedThickness)
                .DisposeWith(disposables);

            cell.Events().MouseEnter
                .Subscribe(_ =>
                {
                    vm.IsHovered = true;
                    vm.HierarchyGridViewModel.HoveredColumn = vm.ColumnIndex;
                    vm.HierarchyGridViewModel.HoveredRow = vm.RowIndex;
                })
                .DisposeWith(disposables);

            cell.Events().MouseLeave
                .Subscribe(_ =>
                {
                    vm.IsHovered = false;
                    vm.HierarchyGridViewModel.HoveredColumn = -1;
                    vm.HierarchyGridViewModel.HoveredRow = -1;
                })
                .DisposeWith(disposables);

            cell.Events().MouseLeftButtonDown
                .Subscribe(e =>
                {
                    if (vm.IsSelected)
                        vm.HierarchyGridViewModel.Selections.Remove((vm.RowIndex, vm.ColumnIndex));
                    else
                    {
                        if (!vm.HierarchyGridViewModel.EnableMultiSelection)
                            vm.HierarchyGridViewModel.Selections.Clear();
                        vm.HierarchyGridViewModel.Selections.Add((vm.RowIndex, vm.ColumnIndex));
                    }
                })
                .DisposeWith(disposables);
        }
    }
}