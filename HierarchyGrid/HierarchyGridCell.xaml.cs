using HierarchyGrid.Definitions;
using LanguageExt;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Linq;
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

namespace HierarchyGrid
{
    public partial class HierarchyGridCell
    {
        public HierarchyGridCell()
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

        private void PopulateFromViewModel(HierarchyGridCellViewModel hgcvm, CompositeDisposable disposables)
        {
            //_txtContent.Text = hgcvm.Result.Match(s => s, () => string.Empty);

            //hgcvm.WhenAnyValue(o => o.Result)
            //    .Throttle(TimeSpan.FromMilliseconds(45))
            //    .Select(r => r.Match(s => s, () => string.Empty))
            //    .ObserveOn(RxApp.MainThreadScheduler)
            //    .BindTo(this, x => x._txtContent.Text)
            //    .DisposeWith(disposables);

            //this.OneWayBind(hgcvm,
            //    vm => vm.Result,
            //    v => v._txtContent.Text)
            //    .DisposeWith(disposables);
        }
    }
}