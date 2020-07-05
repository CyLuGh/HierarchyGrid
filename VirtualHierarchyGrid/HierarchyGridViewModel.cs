using DynamicData;
using HierarchyGrid.Definitions;
using MoreLinq;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Splat;
using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
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

        public ConcurrentDictionary<(int producerPosition, int consumerPosition), ResultSet> ResultSets { get; }
            = new ConcurrentDictionary<(int producerPosition, int consumerPosition), ResultSet>();

        public ObservableCollection<(int producerPosition, int consumerPosition)> Selections { get; }
            = new ObservableCollection<(int producerPosition, int consumerPosition)>();

        [Reactive] public int HorizontalOffset { get; set; }
        [Reactive] public int VerticalOffset { get; set; }

        [Reactive] public int MaxHorizontalOffset { get; set; }
        [Reactive] public int MaxVerticalOffset { get; set; }

        [Reactive] public bool IsTransposed { get; set; }

        [Reactive] public bool EnableCrosshair { get; set; }
        [Reactive] public int HoveredColumn { get; set; }
        [Reactive] public int HoveredRow { get; set; }

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

                this.WhenAnyValue(x => x.HorizontalOffset)
                    .CombineLatest(this.WhenAnyValue(x => x.MaxHorizontalOffset),
                    (ho, m) => ho > m && m > 0)
                    .Throttle(TimeSpan.FromMilliseconds(5))
                    .Where(x => x)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .SubscribeSafe(_ => HorizontalOffset = MaxHorizontalOffset)
                    .DisposeWith(disposables);

                this.WhenAnyValue(x => x.VerticalOffset)
                    .CombineLatest(this.WhenAnyValue(x => x.MaxVerticalOffset),
                    (vo, m) => vo > m && m > 0)
                    .Throttle(TimeSpan.FromMilliseconds(5))
                    .Where(x => x)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .SubscribeSafe(_ => VerticalOffset = MaxVerticalOffset)
                    .DisposeWith(disposables);

                Observable.FromEventPattern<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventArgs>(
                    h => Selections.CollectionChanged += h,
                    h => Selections.CollectionChanged -= h)
                    .SubscribeSafe(_ =>
                    {
                        this.Log().Debug("Collection changed");
                    })
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

            Observable.Return(Unit.Default)
                .ObserveOn(RxApp.TaskpoolScheduler)
                .Do(_ =>
                {
                    var consumers = hierarchyDefinitions.Consumers.FlatList().ToArray();
                    hierarchyDefinitions.Producers.FlatList().AsParallel().ForAll(producer =>
                    {
                        consumers.ForEach(consumer =>
                        {
                            var resultSet = new ResultSet { Result = $"{producer.Content} x {consumer.Content}" };
                            ResultSets.TryAdd((producer.Position, consumer.Position), resultSet);
                        });
                    });
                })
                .InvokeCommand(DrawGridCommand);
        }

        public void Clear()
        {
            ProducersCache.Clear();
            ConsumersCache.Clear();

            Selections.Clear();

            ResultSets.Clear();

            ColumnsWidths.Clear();
            RowsHeights.Clear();

            HorizontalOffset = 0;
            VerticalOffset = 0;
        }
    }
}