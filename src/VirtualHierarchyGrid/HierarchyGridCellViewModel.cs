using HierarchyGrid.Definitions;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;

namespace VirtualHierarchyGrid
{
    public class HierarchyGridCellViewModel : ReactiveObject, IActivatableViewModel
    {
        public int ColumnIndex { get; set; }
        public int RowIndex { get; set; }

        [Reactive] public ResultSet ResultSet { get; set; }

        [Reactive] public bool IsHovered { get; set; }
        [Reactive] public bool IsHighlighted { get; set; }
        [Reactive] public bool IsSelected { get; set; }

        public Qualification Qualifier { [ObservableAsProperty] get; }
        public bool CanEdit { [ObservableAsProperty]get; }

        public ViewModelActivator Activator { get; }

        public HierarchyGridViewModel HierarchyGridViewModel { get; }

        public ReactiveCommand<Unit, Unit> ShowContextMenuCommand { get; private set; }

        public Interaction<(string, ICommand)[], Unit> ShowContextMenuInteraction { get; }
            = new Interaction<(string, ICommand)[], Unit>(RxApp.MainThreadScheduler);

        public HierarchyGridCellViewModel(HierarchyGridViewModel hierarchyGridViewModel)
        {
            Activator = new ViewModelActivator();
            HierarchyGridViewModel = hierarchyGridViewModel;

            InitializeCommands(this);

            this.WhenActivated(disposables =>
            {
                ShowContextMenuInteraction
                    .RegisterHandler(ctx => ctx.SetOutput(Unit.Default))
                    .DisposeWith(disposables);

                HierarchyGridViewModel
                    .WhenAnyValue(x => x.HoveredRow)
                    .CombineLatest(HierarchyGridViewModel.WhenAnyValue(x => x.HoveredColumn),
                    hierarchyGridViewModel.WhenAnyValue(x => x.EnableCrosshair),
                    (row, col, ec) => (row, col, ec))
                    .SubscribeSafe(t =>
                    {
                        var (row, col, ec) = t;
                        if (ec)
                            IsHovered = row == RowIndex || col == ColumnIndex;
                    })
                    .DisposeWith(disposables);

                hierarchyGridViewModel.Highlights.Connect()
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .SubscribeSafe(_ =>
                    {
                        IsHighlighted = hierarchyGridViewModel.Highlights.Items
                                            .Any(x => (x.isRow && x.pos == RowIndex) || (!x.isRow && x.pos == ColumnIndex));
                    })
                    .DisposeWith(disposables);

                hierarchyGridViewModel.SelectedPositions.Connect()
                    .SubscribeSafe(_ =>
                    {
                        IsSelected = hierarchyGridViewModel.SelectedPositions.Lookup((RowIndex, ColumnIndex)).HasValue;
                    })
                    .DisposeWith(disposables);

                this.WhenAnyValue(x => x.ResultSet)
                    .CombineLatest(this.WhenAnyValue(x => x.IsHovered),
                    this.WhenAnyValue(x => x.IsHighlighted),
                    (rs, ho, hi) => (rs, ho, hi))
                    .Select(t =>
                    {
                        var (resultSet, isHovered, isHighlighted) = t;
                        if (resultSet == null)
                            return Qualification.Empty;
                        return isHovered ? Qualification.Hovered
                            : (isHighlighted ? Qualification.Highlighted : resultSet.Qualifier);
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

        private static void InitializeCommands(HierarchyGridCellViewModel @this)
        {
            @this.ShowContextMenuCommand =
                ReactiveCommand.CreateFromObservable(() =>
                {
                    var commands = @this.ResultSet.ContextCommands.Some(o => o)
                        .None(() => new (string, ICommand)[0]);
                    return @this.ShowContextMenuInteraction.Handle(commands);
                });
        }

        internal void Clear()
        {
            ResultSet = null;
            IsHovered = false;
            IsSelected = false;
            IsHighlighted = false;
        }
    }
}