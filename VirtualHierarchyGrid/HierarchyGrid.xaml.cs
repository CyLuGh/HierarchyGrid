using HierarchyGrid.Definitions;
using ReactiveUI;
using Splat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
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
        }
    }
}