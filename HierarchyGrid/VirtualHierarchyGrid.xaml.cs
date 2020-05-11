using HierarchyGrid.Definitions;
using MoreLinq;
using ReactiveUI;
using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls.Primitives;

namespace HierarchyGrid
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class VirtualHierarchyGrid
    {
        public VirtualHierarchyGrid()
        {
            InitializeComponent();

            this.WhenActivated(disposables =>
            {
                this.WhenAnyValue(x => x.ViewModel)
                    .WhereNotNull()
                    .Do(vm => PopulateFromViewModel(vm, disposables))
                    .SubscribeSafe()
                    .DisposeWith(disposables);
            });
        }

        private void PopulateFromViewModel(HierarchyGridViewModel hgvm, CompositeDisposable disposables)
        {
            this.Bind(hgvm,
                vm => vm.VScrollPos,
                v => v.VScrollVGrid.Value,
                VScrollVGrid.Events().Scroll,
                vmToViewConverter: i => Convert.ToDouble(i),
                viewToVmConverter: d => Convert.ToInt32(d))
                .DisposeWith(disposables);

            //this.Bind(hgvm,
            //    vm => vm.HScrollPos,
            //    v => v.HScrollVGrid.Value,
            //    vmToViewConverter: i => Convert.ToDouble(i),
            //    viewToVmConverter: d => Convert.ToInt32(d))
            //    .DisposeWith(disposables);

            //this.VScrollVGrid.Events().Scroll.SubscribeSafe(e =>
            //{
            //    var test = VScrollVGrid.Value;
            //    hgvm.VScrollPos = Convert.ToInt32(VScrollVGrid.Value);
            //})
            //.DisposeWith(disposables);

            // TODO
            //ScrollGrid.Events().MouseWheel
            ///ScrollGrid.Events().SizeChanged
            ///

            this.OneWayBind(hgvm,
                vm => vm.ScaleFactor,
                v => v.ScaleTransform.ScaleX)
                .DisposeWith(disposables);

            this.OneWayBind(hgvm,
               vm => vm.ScaleFactor,
               v => v.ScaleTransform.ScaleY)
               .DisposeWith(disposables);

            this.OneWayBind(hgvm,
                vm => vm.ColumnsOnly,
                v => v.GridHeaders.Visibility,
                b => b ? Visibility.Collapsed : Visibility.Visible)
                .DisposeWith(disposables);

            this.OneWayBind(hgvm,
                vm => vm.ColumnsOnly,
                v => v.SuperSplitter.Visibility,
                b => b ? Visibility.Collapsed : Visibility.Visible)
                .DisposeWith(disposables);

            // SuperSplitter drag completed event

            this.OneWayBind(hgvm,
              vm => vm.ColumnsOnly,
              v => v.VRowHeadersGrid.Visibility,
              b => b ? Visibility.Collapsed : Visibility.Visible)
              .DisposeWith(disposables);

            hgvm.DrawGridInteraction.RegisterHandler(ctx =>
            {
                hgvm.ColumnsElements.FlatList().Size(DefaultColumnWidth);
                hgvm.RowsElements.FlatList().Size(DefaultRowHeight);

                for (int i = 0; i < hgvm.RowsElements.TotalDepth(); i++)
                    RowHeadersWidth.Add(DefaultRowHeaderWidth);

                UpdateSize(hgvm.RowsElements, hgvm.ColumnsElements, false);

                DrawHeaders(hgvm.RowsElements, hgvm.RowLevels, ScrollGrid.RowDefinitions[0].ActualHeight * 1 / ScaleFactor, hgvm.VScrollPos, false);
                DrawHeaders(hgvm.ColumnsElements, hgvm.ColumnLevels, ScrollGrid.ActualWidth * 1 / ScaleFactor - VGrid.ColumnDefinitions[0].Width.Value, hgvm.HScrollPos, true);

                ctx.SetOutput(Unit.Default);
            });
        }
    }
}