using HierarchyGrid.Definitions;
using ReactiveUI;
using Splat;
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
using System.Windows.Shapes;

namespace VirtualHierarchyGrid
{
    public partial class HierarchyGridCell : IEnableLogger
    {
        internal static Thickness UnselectedThickness { get; } = new Thickness(1);
        internal static Thickness SelectedThickness { get; } = new Thickness(2);

        internal static Brush CellBackground { get; set; }
        internal static Brush CellForeground { get; set; }
        internal static Brush CellBorderBrush { get; set; }
        internal static Brush CellSelectedBorder { get; set; }
        internal static Brush CellHighlightBackground { get; set; }
        internal static Brush CellHighlightForeground { get; set; }
        internal static Brush CellHoverBackground { get; set; }
        internal static Brush CellHoverForeground { get; set; }
        internal static Brush CellErrorBackground { get; set; }
        internal static Brush CellErrorForeground { get; set; }
        internal static Brush CellWarningBackground { get; set; }
        internal static Brush CellWarningForeground { get; set; }
        internal static Brush CellRemarkBackground { get; set; }
        internal static Brush CellRemarkForeground { get; set; }
        internal static Brush CellReadOnlyBackground { get; set; }
        internal static Brush CellReadOnlyForeground { get; set; }
        internal static Brush EmptyBrush { get; set; }

        static HierarchyGridCell()
        {
            var rect = new Rectangle();
            CellBackground = (Brush)rect.TryFindResource("CellBackground") ?? Brushes.White;
            CellForeground = (Brush)rect.TryFindResource("CellForeground") ?? Brushes.Black;
            CellBorderBrush = (Brush)rect.TryFindResource("CellBorder") ?? Brushes.DarkGray;

            CellSelectedBorder = (Brush)rect.TryFindResource("CellSelectedBorder") ?? Brushes.BlueViolet;

            CellHighlightBackground = (Brush)rect.TryFindResource("CellHighlightBackground") ?? Brushes.LightBlue;
            CellHighlightForeground = (Brush)rect.TryFindResource("CellHighlightForeground") ?? Brushes.Black;

            CellHoverBackground = (Brush)rect.TryFindResource("CellHoverBackground") ?? Brushes.LightSeaGreen;
            CellHoverForeground = (Brush)rect.TryFindResource("CellHoverForeground") ?? Brushes.Black;

            CellErrorBackground = (Brush)rect.TryFindResource("CellErrorBackground") ?? Brushes.IndianRed;
            CellErrorForeground = (Brush)rect.TryFindResource("CellErrorForeground") ?? Brushes.Black;

            CellWarningBackground = (Brush)rect.TryFindResource("CellWarningBackground") ?? Brushes.YellowGreen;
            CellWarningForeground = (Brush)rect.TryFindResource("CellWarningForeground") ?? Brushes.Black;

            CellRemarkBackground = (Brush)rect.TryFindResource("CellRemarkBackground") ?? Brushes.GreenYellow;
            CellRemarkForeground = (Brush)rect.TryFindResource("CellRemarkForeground") ?? Brushes.Black;

            CellReadOnlyBackground = (Brush)rect.TryFindResource("CellReadOnlyBackground") ?? Brushes.GreenYellow;
            CellReadOnlyForeground = (Brush)rect.TryFindResource("CellReadOnlyForeground") ?? Brushes.Black;

            EmptyBrush = (Brush)rect.TryFindResource("EmptyBrush") ?? Brushes.Transparent;

            rect = null;
        }

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
                vm => vm.Qualifier,
                v => v.CellBorder.Background,
                q => q switch
                    {
                        Qualification.Error => CellErrorBackground,
                        Qualification.Warning => CellWarningBackground,
                        Qualification.Remark => CellRemarkBackground,
                        Qualification.ReadOnly => CellReadOnlyBackground,
                        Qualification.Highlighted => CellHighlightBackground,
                        Qualification.Hovered => CellHoverBackground,
                        Qualification.Empty => EmptyBrush,
                        Qualification.Custom => vm.ResultSet.CustomColor.Some(c => (Brush)new SolidColorBrush(Color.FromArgb(c.a, c.r, c.g, c.b)))
                                                                        .None(() => CellBackground),
                        _ => CellBackground
                    }
                )
                .DisposeWith(disposables);

            cell.OneWayBind(vm,
                vm => vm.Qualifier,
                v => v.Foreground,
                q => q switch
                    {
                        Qualification.Error => CellErrorForeground,
                        Qualification.Warning => CellWarningForeground,
                        Qualification.Remark => CellRemarkForeground,
                        Qualification.ReadOnly => CellReadOnlyForeground,
                        Qualification.Highlighted => CellHighlightForeground,
                        Qualification.Hovered => CellHoverForeground,
                        Qualification.Empty => Brushes.Transparent,
                        _ => CellForeground
                    }
                )
                .DisposeWith(disposables);

            cell.OneWayBind(vm,
                vm => vm.IsSelected,
                v => v.CellBorder.BorderBrush,
                selected => selected ? CellSelectedBorder : CellBorderBrush)
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

            cell.Events().MouseDoubleClick
                .Subscribe(e =>
                {
                    if (e.ChangedButton == MouseButton.Left)
                    {
                        if (!vm.HierarchyGridViewModel.EnableMultiSelection)
                            vm.HierarchyGridViewModel.Selections.Clear();
                        vm.HierarchyGridViewModel.Selections.Add((vm.RowIndex, vm.ColumnIndex));

                        if (vm.CanEdit)
                            Observable.Return((vm.RowIndex, vm.ColumnIndex, vm.ResultSet))
                                      .InvokeCommand(vm.HierarchyGridViewModel, x => x.EditCommand);
                    }
                })
                .DisposeWith(disposables);

            cell.Events().MouseLeftButtonDown
                .Subscribe(e =>
                {
                    vm.HierarchyGridViewModel.IsEditing = false;
                    if (e.ClickCount == 1)
                    {
                        if (vm.IsSelected)
                        {
                            if (Keyboard.Modifiers == ModifierKeys.Control)
                                vm.HierarchyGridViewModel.Selections.Remove((vm.RowIndex, vm.ColumnIndex));
                        }
                        else
                        {
                            if (!vm.HierarchyGridViewModel.EnableMultiSelection)
                                vm.HierarchyGridViewModel.Selections.Clear();
                            vm.HierarchyGridViewModel.Selections.Add((vm.RowIndex, vm.ColumnIndex));
                        }
                    }
                })
                .DisposeWith(disposables);
        }
    }
}