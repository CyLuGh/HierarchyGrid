using HierarchyGrid.Definitions;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;

namespace VirtualHierarchyGrid
{
    public class HierarchyGridCellViewModel : ReactiveObject, IActivatableViewModel
    {
        //[Reactive] public ProducerDefinition Producer { get; set; }
        //[Reactive] public ConsumerDefinition Consumer { get; set; }

        public int ColumnIndex { get; set; }
        public int RowIndex { get; set; }

        [Reactive] public ResultSet ResultSet { get; set; }

        [Reactive] public bool IsHovered { get; set; }
        [Reactive] public bool IsSelected { get; set; }

        public ViewModelActivator Activator { get; }

        private ReactiveCommand<(ProducerDefinition, ConsumerDefinition), ResultSet> ResolveCommand { get; set; }
        public HierarchyGridViewModel HierarchyGridViewModel { get; }

        public HierarchyGridCellViewModel(HierarchyGridViewModel hierarchyGridViewModel)
        {
            Activator = new ViewModelActivator();
            HierarchyGridViewModel = hierarchyGridViewModel;

            InitializeCommands(this);

            this.WhenActivated(disposables =>
            {
                HierarchyGridViewModel.WhenAnyValue(x => x.HoveredRow)
                    .CombineLatest(HierarchyGridViewModel.WhenAnyValue(x => x.HoveredColumn),
                    hierarchyGridViewModel.WhenAnyValue(x => x.EnableCrosshair),
                    (row, col, ec) => (row, col, ec))
                    .Subscribe(t =>
                    {
                        var (row, col, ec) = t;
                        if (ec)
                            IsHovered = row == RowIndex || col == ColumnIndex;
                    })
                    .DisposeWith(disposables);

                Observable.FromEventPattern<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventArgs>(
                    h => hierarchyGridViewModel.Selections.CollectionChanged += h,
                    h => hierarchyGridViewModel.Selections.CollectionChanged -= h)
                    .SubscribeSafe(e =>
                    {
                        IsSelected = hierarchyGridViewModel.Selections.Any(x => x.row == RowIndex && x.col == ColumnIndex);
                    })
                    .DisposeWith(disposables);
            });
        }

        private static void InitializeCommands(HierarchyGridCellViewModel hierarchyGridCellViewModel)
        {
            //hierarchyGridCellViewModel.ResolveCommand = ReactiveCommand.CreateFromObservable<(ProducerDefinition, ConsumerDefinition), ResultSet>(t =>
            //     Observable.Start(() =>
            //     {
            //         return new ResultSet { Result = "Resolved" };
            //     }));
            //hierarchyGridCellViewModel.ResolveCommand
            //    .ObserveOn(RxApp.MainThreadScheduler)
            //    .SubscribeSafe(r =>
            //    {
            //        hierarchyGridCellViewModel.Result = r.Result;
            //    });
        }
    }
}