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

        public Qualification Qualifier { [ObservableAsProperty] get; }
        public bool CanEdit { [ObservableAsProperty]get; }

        public ViewModelActivator Activator { get; }

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

                this.WhenAnyValue(x => x.ResultSet)
                    .CombineLatest(this.WhenAnyValue(x => x.IsHovered),
                    (rs, ho) => (rs, ho))
                    .Select(t =>
                    {
                        var (resultSet, isHovered) = t;
                        if (resultSet == null)
                            return Qualification.Empty;
                        return isHovered ? Qualification.Hovered : resultSet.Qualifier;
                    })
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .ToPropertyEx(this, x => x.Qualifier)
                    .DisposeWith(disposables);

                this.WhenAnyValue(x => x.ResultSet)
                    .Select(rs =>
                    {
                        if (rs == null)
                            return false;

                        return rs.Editor.Match(_ => rs.Qualifier != Qualification.ReadOnly, () => false);
                    })
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .ToPropertyEx(this, x => x.CanEdit)
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

        internal void Clear()
        {
            ResultSet = null;
            IsHovered = false;
            IsSelected = false;
        }
    }
}