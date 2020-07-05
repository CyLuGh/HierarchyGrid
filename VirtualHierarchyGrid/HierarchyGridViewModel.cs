using DynamicData;
using HierarchyGrid.Definitions;
using MoreLinq;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace VirtualHierarchyGrid
{
    public partial class HierarchyGridViewModel : ReactiveObject, IActivatableViewModel
    {
        public ViewModelActivator Activator { get; }

        private SourceCache<ProducerDefinition, int> ProducersCache { get; } = new SourceCache<ProducerDefinition, int>(x => x.Position);
        private SourceCache<ConsumerDefinition, int> ConsumersCache { get; } = new SourceCache<ConsumerDefinition, int>(x => x.Position);

        [Reactive] public int HorizontalOffset { get; set; }
        [Reactive] public int VerticalOffset { get; set; }

        [Reactive] public int MaxHorizontalOffset { get; set; }
        [Reactive] public int MaxVerticalOffset { get; set; }

        [Reactive] public bool IsTransposed { get; set; }

        public HierarchyDefinition[] ColumnsDefinitions => IsTransposed ?
            ProducersCache.Items.Cast<HierarchyDefinition>().ToArray() : ConsumersCache.Items.Cast<HierarchyDefinition>().ToArray();

        public HierarchyDefinition[] RowsDefinitions => IsTransposed ?
            ConsumersCache.Items.Cast<HierarchyDefinition>().ToArray() : ProducersCache.Items.Cast<HierarchyDefinition>().ToArray();

        public ReactiveCommand<Unit, Unit> DrawGridCommand { get; private set; }

        public Interaction<Unit, Unit> DrawGridInteraction { get; }
            = new Interaction<Unit, Unit>(RxApp.MainThreadScheduler);

        public bool IsValid => RowsHeadersWidth?.Any() == true && ColumnsHeadersHeight?.Any() == true;

        public HierarchyGridViewModel()
        {
            Activator = new ViewModelActivator();

            DrawGridInteraction.RegisterHandler(ctx => ctx.SetOutput(Unit.Default));
            DrawGridCommand = ReactiveCommand.CreateFromObservable(() => DrawGridInteraction.Handle(Unit.Default));

            this.WhenActivated(disposables =>
            {
                this.WhenAnyValue(x => x.HorizontalOffset)
                    .CombineLatest(this.WhenAnyValue(x => x.VerticalOffset),
                    (ho, vo) => Unit.Default)
                    .Throttle(TimeSpan.FromMilliseconds(5))
                .InvokeCommand(DrawGridCommand)
                .DisposeWith(disposables);
            });
        }

        public void Set(HierarchyDefinitions hierarchyDefinitions)
        {
            Clear();

            ProducersCache.AddOrUpdate(hierarchyDefinitions.Producers);
            ConsumersCache.AddOrUpdate(hierarchyDefinitions.Consumers);

            RowsHeadersWidth = Enumerable.Range(0, RowsDefinitions.TotalDepth(true))
                .Select(_ => DEFAULT_HEADER_WIDTH)
                .ToArray();

            ColumnsHeadersHeight = Enumerable.Range(0, ColumnsDefinitions.TotalDepth(true))
                .Select(_ => DEFAULT_HEADER_HEIGHT)
                .ToArray();

            ColumnsDefinitions.Leaves().Select((_, i) => i)
                .ForEach(x => ColumnsWidths.Add(x, DEFAULT_COLUMN_WIDTH));

            RowsDefinitions.Leaves().Select((_, i) => i)
                .ForEach(x => RowsHeights.Add(x, DEFAULT_ROW_HEIGHT));

            Observable.Return(Unit.Default).InvokeCommand(DrawGridCommand);
        }

        public void Clear()
        {
            ProducersCache.Clear();
            ConsumersCache.Clear();

            ColumnsWidths.Clear();
            RowsHeights.Clear();

            HorizontalOffset = 0;
            VerticalOffset = 0;
        }
    }
}