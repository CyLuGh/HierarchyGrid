using DynamicData;
using HierarchyGrid.Definitions;
using LanguageExt.Common;
using MoreLinq;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
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

        public ConcurrentDictionary<(int row, int col), ResultSet> ResultSets { get; }
            = new ConcurrentDictionary<(int row, int col), ResultSet>();

        public ObservableCollection<(int row, int col)> Selections { get; }
            = new ObservableCollection<(int row, int col)>();

        [Reactive] public int HorizontalOffset { get; set; }
        [Reactive] public int VerticalOffset { get; set; }

        [Reactive] public double Scale { get; set; } = 1d;

        [Reactive] public int MaxHorizontalOffset { get; set; }
        [Reactive] public int MaxVerticalOffset { get; set; }

        [Reactive] public bool IsTransposed { get; set; }

        [Reactive] public bool EnableCrosshair { get; set; }
        [Reactive] public int HoveredColumn { get; set; } = -1;
        [Reactive] public int HoveredRow { get; set; } = -1;

        [Reactive] public bool EnableMultiSelection { get; set; }
        [Reactive] public bool IsEditing { get; set; }

        public HierarchyDefinition[] ColumnsDefinitions => IsTransposed ?
            ProducersCache.Items.Cast<HierarchyDefinition>().ToArray() : ConsumersCache.Items.Cast<HierarchyDefinition>().ToArray();

        public HierarchyDefinition[] RowsDefinitions => IsTransposed ?
            ConsumersCache.Items.Cast<HierarchyDefinition>().ToArray() : ProducersCache.Items.Cast<HierarchyDefinition>().ToArray();

        public ReactiveCommand<Unit, Unit> DrawGridCommand { get; private set; }

        public Interaction<Unit, Unit> DrawGridInteraction { get; }
            = new Interaction<Unit, Unit>(RxApp.MainThreadScheduler);

        public bool IsValid => RowsHeadersWidth?.Any() == true && ColumnsHeadersHeight?.Any() == true;

        public ReactiveCommand<(int row, int column, ResultSet rs), Unit> EditCommand { get; }

        public Interaction<(int row, int column, ResultSet rs), Unit> EditInteraction { get; }
            = new Interaction<(int row, int column, ResultSet rs), Unit>(RxApp.MainThreadScheduler);

        public ReactiveCommand<Unit, Unit> EndEditionCommand { get; }

        public Interaction<Unit, Unit> EndEditionInteraction { get; }
            = new Interaction<Unit, Unit>(RxApp.MainThreadScheduler);

        public HierarchyGridViewModel()
        {
            Activator = new ViewModelActivator();

            DrawGridInteraction.RegisterHandler(ctx => ctx.SetOutput(Unit.Default));
            DrawGridCommand = ReactiveCommand.CreateFromObservable(() => DrawGridInteraction.Handle(Unit.Default));

            EditInteraction.RegisterHandler(ctx => ctx.SetOutput(Unit.Default));
            EditCommand = ReactiveCommand.CreateFromObservable<(int, int, ResultSet), Unit>(t => EditInteraction.Handle(t));

            EndEditionInteraction.RegisterHandler(ctx => ctx.SetOutput(Unit.Default));
            EndEditionCommand = ReactiveCommand.CreateFromObservable(() => EndEditionInteraction.Handle(Unit.Default));

            this.WhenActivated(disposables =>
            {
                /* Don't allow scale < 0.75 */
                this.WhenAnyValue(x => x.Scale)
                    .Where(x => x < 0.75)
                    .SubscribeSafe(_ => Scale = 0.75)
                    .DisposeWith(disposables);

                /* Don't allow scale > 1 */
                this.WhenAnyValue(x => x.Scale)
                    .Where(x => x > 1)
                    .SubscribeSafe(_ => Scale = 1)
                    .DisposeWith(disposables);

                /* Redraw grid when scrolling or changing scale */
                this.WhenAnyValue(x => x.HorizontalOffset)
                    .CombineLatest(this.WhenAnyValue(x => x.VerticalOffset),
                    this.WhenAnyValue(x => x.Scale).DistinctUntilChanged(),
                    (ho, vo, sc) => Unit.Default)
                    .Throttle(TimeSpan.FromMilliseconds(15))
                .InvokeCommand(DrawGridCommand)
                .DisposeWith(disposables);

                /* Don't allow horizontal offset to go abose max offset */
                this.WhenAnyValue(x => x.HorizontalOffset)
                    .CombineLatest(this.WhenAnyValue(x => x.MaxHorizontalOffset),
                    (ho, m) => ho > m && m > 0)
                    .Throttle(TimeSpan.FromMilliseconds(5))
                    .Where(x => x)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .SubscribeSafe(_ => HorizontalOffset = MaxHorizontalOffset)
                    .DisposeWith(disposables);

                /* Don't allow vertical offset to go abose max offset */
                this.WhenAnyValue(x => x.VerticalOffset)
                    .CombineLatest(this.WhenAnyValue(x => x.MaxVerticalOffset),
                    (vo, m) => vo > m && m > 0)
                    .Throttle(TimeSpan.FromMilliseconds(5))
                    .Where(x => x)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .SubscribeSafe(_ => VerticalOffset = MaxVerticalOffset)
                    .DisposeWith(disposables);

                /* Don't allow negative horizontal offset */
                this.WhenAnyValue(x => x.HorizontalOffset)
                    .Where(x => x < 0)
                    .SubscribeSafe(_ => HorizontalOffset = 0)
                    .DisposeWith(disposables);

                /* Don't allow negative vertical offset */
                this.WhenAnyValue(x => x.VerticalOffset)
                    .Where(x => x < 0)
                    .SubscribeSafe(_ => VerticalOffset = 0)
                    .DisposeWith(disposables);

                /* Clear selection when changing selection mode */
                this.WhenAnyValue(x => x.EnableMultiSelection)
                    .SubscribeSafe(_ => Selections.Clear())
                    .DisposeWith(disposables);

                /* Toggle edit mode on */
                EditCommand.SubscribeSafe(_ => IsEditing = true)
                    .DisposeWith(disposables);

                /* Toggle edit mode off */
                DrawGridCommand.SubscribeSafe(_ => IsEditing = false)
                    .DisposeWith(disposables);

                /* Clear textbox when exiting edition mode */
                this.WhenAnyValue(x => x.IsEditing)
                    .DistinctUntilChanged()
                    .Where(x => !x)
                    .Select(_ => Unit.Default)
                    .InvokeCommand(EndEditionCommand)
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
                        consumers.ForEach(consumer => ResultSets.TryAdd((producer.Position, consumer.Position), HierarchyDefinition.Resolve(producer, consumer)))
                    );
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

            HoveredRow = -1;
            HoveredColumn = -1;
        }
    }
}