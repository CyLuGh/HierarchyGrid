using HierarchyGrid.Definitions;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace VirtualHierarchyGrid
{
    public class HierarchyGridHeaderViewModel : ReactiveObject, IActivatableViewModel
    {
        [Reactive] public object Content { get; set; }
        [Reactive] public bool IsChecked { get; set; }

        [Reactive] public bool IsHovered { get; set; }
        [Reactive] public bool IsHighlighted { get; set; }

        [Reactive] public bool CanToggle { get; set; }

        public Qualification Qualification { [ObservableAsProperty] get; }

        public int? ColumnIndex { get; set; }
        public int? RowIndex { get; set; }

        public HierarchyGridHeaderViewModel(HierarchyGridViewModel hierarchyGridViewModel)
        {
            Activator = new ViewModelActivator();
            HierarchyGridViewModel = hierarchyGridViewModel;

            this.WhenActivated(disposables =>
            {
                HierarchyGridViewModel.WhenAnyValue(x => x.HoveredRow)
                    .CombineLatest(HierarchyGridViewModel.WhenAnyValue(x => x.HoveredColumn),
                    (row, col) => (row, col))
                    .Subscribe(t =>
                    {
                        var (row, col) = t;

                        if (RowIndex.HasValue && RowIndex == row)
                            IsHovered = true;
                        else if (ColumnIndex.HasValue && ColumnIndex == col)
                            IsHovered = true;
                        else
                            IsHovered = false;
                    })
                    .DisposeWith(disposables);

                this.WhenAnyValue(x => x.IsHovered)
                    .CombineLatest(this.WhenAnyValue(x => x.IsHighlighted),
                    (isHovered, isHighlighted) =>
                    {
                        if (isHovered)
                            return Qualification.Hovered;
                        if (isHighlighted)
                            return Qualification.Highlighted;

                        return Qualification.Unset;
                    })
                    .ToPropertyEx(this, x => x.Qualification, scheduler: RxApp.MainThreadScheduler)
                    .DisposeWith(disposables);
            });
        }

        public HierarchyGridViewModel HierarchyGridViewModel { get; }

        public ViewModelActivator Activator { get; }
    }
}