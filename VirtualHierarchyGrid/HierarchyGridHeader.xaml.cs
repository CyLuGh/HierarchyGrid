using HierarchyGrid.Definitions;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Text;
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
    public partial class HierarchyGridHeader
    {
        private static Brush HeaderBackgroundBrush { get; set; }
        private static Brush HeaderForegroundBrush { get; set; }
        private static Brush HeaderBorderBrush { get; set; }

        static HierarchyGridHeader()
        {
            var rect = new Rectangle();

            HeaderBackgroundBrush = (Brush)rect.TryFindResource("HeaderBackground") ?? Brushes.Gray;
            HeaderForegroundBrush = (Brush)rect.TryFindResource("HeaderForeground") ?? Brushes.Black;
            HeaderBorderBrush = (Brush)rect.TryFindResource("HeaderBorder") ?? Brushes.DarkGray;

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
                vm => vm.Content,
                v => v.Presenter.Content)
                .DisposeWith(disposables);

            header.OneWayBind(vm,
               vm => vm.IsHovered,
               v => v.HeaderBorder.BorderBrush,
               hovered => hovered ? GruvBoxBrushes.DarkBlue : HeaderBorderBrush)
               .DisposeWith(disposables);

            header.OneWayBind(vm,
               vm => vm.IsHovered,
               v => v.HeaderBorder.Background,
               hovered => hovered ? GruvBoxBrushes.LightBlue : HeaderBackgroundBrush)
               .DisposeWith(disposables);

            header.Events().MouseEnter
               .Subscribe(_ =>
               {
                   vm.IsHovered = true;
                   if ( vm.RowIndex.HasValue)
                    vm.HierarchyGridViewModel.HoveredRow = vm.RowIndex.Value;
                   if ( vm.ColumnIndex.HasValue)
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