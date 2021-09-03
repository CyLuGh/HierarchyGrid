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
using System.Windows.Shapes;

using ReactiveMarbles.ObservableEvents;

namespace VirtualHierarchyGrid
{
    public partial class HierarchyGridHeader
    {
        internal static Brush HeaderBackgroundBrush { get; set; }
        internal static Brush HeaderForegroundBrush { get; set; }
        internal static Brush HeaderBorderBrush { get; set; }
        internal static Brush HeaderHoverBorderBrush { get; set; }

        internal static Brush HeaderHighlightBorderBrush { get; set; }

        static HierarchyGridHeader()
        {
            var rect = new Rectangle();

            HeaderBackgroundBrush = (Brush)rect.TryFindResource("HeaderBackground") ?? Brushes.Gray;
            HeaderForegroundBrush = (Brush)rect.TryFindResource("HeaderForeground") ?? Brushes.Black;
            HeaderBorderBrush = (Brush)rect.TryFindResource("HeaderBorderBrush") ?? Brushes.DarkGray;
            HeaderHoverBorderBrush = (Brush)rect.TryFindResource("HeaderHoverBorderBrush") ?? Brushes.DarkGray;

            rect = null;
        }

        public HierarchyGridHeader()
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

        private static void PopulateFromViewModel(HierarchyGridHeader header, HierarchyGridHeaderViewModel vm, CompositeDisposable disposables)
        {
            header.Foreground = HeaderForegroundBrush;

            header.OneWayBind(vm,
                vm => vm.CanToggle,
                v => v.StatusPresenter.Visibility,
                b => b ? Visibility.Visible : Visibility.Collapsed)
                .DisposeWith(disposables);

            header.OneWayBind(vm,
                vm => vm.IsChecked,
                v => v.StatusPresenter.Content,
                b => b ? header.TryFindResource("ExpandedIcon") : header.TryFindResource("FoldedIcon"))
                .DisposeWith(disposables);

            header.OneWayBind(vm,
                vm => vm.Content,
                v => v.Presenter.Content)
                .DisposeWith(disposables);

            header.OneWayBind(vm,
               vm => vm.IsHovered,
               v => v.HeaderBorder.BorderBrush,
               hovered => hovered ? HeaderHoverBorderBrush : HeaderBorderBrush)
               .DisposeWith(disposables);

            header.OneWayBind(vm,
                vm => vm.IsHighlighted,
                v => v.Foreground,
                highlighted => highlighted ? HierarchyGridCell.CellHighlightForeground : HeaderForegroundBrush)
                .DisposeWith(disposables);

            header.OneWayBind(vm,
                vm => vm.Qualification,
                v => v.Foreground,
                qual => qual switch
                {
                    Qualification.Hovered => HierarchyGridCell.CellHoverForeground,
                    Qualification.Highlighted => HierarchyGridCell.CellHighlightForeground,
                    _ => HeaderForegroundBrush
                })
                .DisposeWith(disposables);

            header.OneWayBind(vm,
                vm => vm.Qualification,
                v => v.HeaderBorder.Background,
                qual => qual switch
                    {
                        Qualification.Hovered => HierarchyGridCell.CellHoverBackground,
                        Qualification.Highlighted => HierarchyGridCell.CellHighlightBackground,
                        _ => HeaderBackgroundBrush
                    })
                .DisposeWith(disposables);

            header.Events().MouseEnter
               .Subscribe(_ =>
               {
                   vm.IsHovered = true;
                   if (vm.RowIndex.HasValue)
                       vm.HierarchyGridViewModel.HoveredRow = vm.RowIndex.Value;
                   if (vm.ColumnIndex.HasValue)
                       vm.HierarchyGridViewModel.HoveredColumn = vm.ColumnIndex.Value;
               })
               .DisposeWith(disposables);

            header.Events().MouseLeave
                .Subscribe(_ =>
                {
                    vm.IsHovered = false;
                    vm.HierarchyGridViewModel.HoveredColumn = -1;
                    vm.HierarchyGridViewModel.HoveredRow = -1;
                })
                .DisposeWith(disposables);
        }
    }
}