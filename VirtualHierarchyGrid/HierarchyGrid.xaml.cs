using HierarchyGrid.Definitions;
using ReactiveUI;
using Splat;
using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace VirtualHierarchyGrid
{
    public partial class HierarchyGrid : IEnableLogger
    {
        public HierarchyGrid()
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

        private static void PopulateFromViewModel(HierarchyGrid hierarchyGrid, HierarchyGridViewModel vm, CompositeDisposable disposables)
        {
            vm.DrawGridInteraction.RegisterHandler(ctx =>
                {
                    hierarchyGrid.DrawGrid(hierarchyGrid.RenderSize);
                    ctx.SetOutput(Unit.Default);
                })
                .DisposeWith(disposables);

            hierarchyGrid.HierarchyGridCanvas.Events()
                .SizeChanged
                .Throttle(TimeSpan.FromMilliseconds(200))
                .ObserveOn(RxApp.MainThreadScheduler)
                .SubscribeSafe(e =>
                {
                    hierarchyGrid.DrawGrid(e.NewSize);
                })
                .DisposeWith(disposables);

            hierarchyGrid.Bind(vm,
                vm => vm.HorizontalOffset,
                v => v.HorizontalScrollBar.Value,
                hierarchyGrid.HorizontalScrollBar.Events().Scroll,
                vmToViewConverter: i => Convert.ToDouble(i),
                viewToVmConverter: d => Convert.ToInt32(d))
                .DisposeWith(disposables);

            hierarchyGrid.Bind(vm,
                vm => vm.VerticalOffset,
                v => v.VerticalScrollBar.Value,
                hierarchyGrid.VerticalScrollBar.Events().Scroll,
                vmToViewConverter: i => Convert.ToDouble(i),
                viewToVmConverter: d => Convert.ToInt32(d))
                .DisposeWith(disposables);

            hierarchyGrid.OneWayBind(vm,
                vm => vm.MaxHorizontalOffset,
                v => v.HorizontalScrollBar.Maximum)
                .DisposeWith(disposables);

            hierarchyGrid.OneWayBind(vm,
                vm => vm.MaxVerticalOffset,
                v => v.VerticalScrollBar.Maximum)
                .DisposeWith(disposables);
        }
    }
}