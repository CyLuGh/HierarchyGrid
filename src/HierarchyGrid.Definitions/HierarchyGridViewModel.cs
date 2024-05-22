using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using DynamicData;
using DynamicData.Binding;
using LanguageExt;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Splat;
using RxUnit = System.Reactive.Unit;

namespace HierarchyGrid.Definitions
{
    public partial class HierarchyGridViewModel : ReactiveObject, IActivatableViewModel
    {
        public ViewModelActivator Activator { get; }
        public bool IsValid =>
            RowsHeadersWidth?.Any() == true && ColumnsHeadersHeight?.Any() == true;

        internal SourceCache<ProducerDefinition, int> ProducersCache { get; } =
            new(x => x.Position);
        internal SourceCache<ConsumerDefinition, int> ConsumersCache { get; } =
            new(x => x.Position);

        public bool HasData
        {
            [ObservableAsProperty]
            get;
        }

        [Reactive]
        public string StatusMessage { get; set; }

        [Reactive]
        public string EditionContent { get; set; } = string.Empty;

        internal AtomHashMap<(Guid, Guid), ResultSet> ResultSets { get; } =
            Prelude.AtomHashMap<(Guid, Guid), ResultSet>();

        internal ObservableUniqueCollection<PositionedCell> SelectedCells { get; } = new();

        public Seq<PositionedCell> Selections
        {
            get => SelectedCells.ToSeq();
            set
            {
                SelectedCells.Clear();

                if (!value.IsEmpty && SelectionMode != SelectionMode.None)
                {
                    var cells = MatchPositionedCells(value);
                    switch (SelectionMode)
                    {
                        case SelectionMode.Single:
                            SelectedCells.Add(cells.First());
                            break;

                        case SelectionMode.MultiSimple:
                        case SelectionMode.MultiExtended:
                            SelectedCells.AddRange(cells);
                            break;

                        default:
                            break;
                    }
                }
            }
        }

        private Subject<Seq<PositionedCell>> SelectionChangedSubject { get; } = new();

        public IObservable<Seq<PositionedCell>> SelectionChanged =>
            SelectionChangedSubject.AsObservable().Publish().RefCount();

        [Reactive]
        public Option<PositionedCell> EditedCell { get; internal set; }

        [Reactive]
        public Option<PositionedDefinition> HoveredDefinitionHeader { get; internal set; }

        public IObservable<Option<PositionedCell>> EditedCellChanged =>
            this.WhenAnyValue(x => x.EditedCell).Publish().RefCount();

        public bool IsEditing
        {
            [ObservableAsProperty]
            get;
        }

        public Interaction<Seq<PositionedCell>, RxUnit> DrawEditionTextBoxInteraction { get; } =
            new(RxApp.MainThreadScheduler);

        /// <summary>
        /// Cells with extra rendering elements
        /// </summary>
        [Reactive]
        public HashMap<PositionedCell, FocusCellInfo> FocusCells { get; set; }

        public ConcurrentBag<(
            ElementCoordinates Coord,
            PositionedDefinition Definition
        )> HeadersCoordinates { get; } = new();

        public ConcurrentBag<(
            ElementCoordinates Coord,
            PositionedCell Cell
        )> CellsCoordinates { get; } = new();

        public ConcurrentBag<(
            ElementCoordinates Coord,
            Guid Guid,
            Action Action
        )> GlobalHeadersCoordinates { get; } = new();

        [Reactive]
        public int HorizontalOffset { get; set; }

        [Reactive]
        public int VerticalOffset { get; set; }

        [Reactive]
        public double Scale { get; set; } = 1d;

        [Reactive]
        public double Width { get; set; } = double.NaN;

        [Reactive]
        public double Height { get; set; } = double.NaN;

        [Reactive]
        public int MaxHorizontalOffset { get; set; }

        [Reactive]
        public int MaxVerticalOffset { get; set; }

        [Reactive]
        public bool IsTransposed { get; set; }

        [Reactive]
        public bool EnableCrosshair { get; set; }

        [Reactive]
        public int HoveredColumn { get; set; } = -1;

        [Reactive]
        public int HoveredRow { get; set; } = -1;

        [Reactive]
        public SelectionMode SelectionMode { get; set; }

        [Reactive]
        public CellTextAlignment TextAlignment { get; set; } = CellTextAlignment.Right;

        [Reactive]
        public ITheme Theme { get; set; } = HierarchyGridTheme.Default;

        [Reactive]
        public Option<PositionedCell> HoveredCell { get; set; }

        [Reactive]
        public Guid HoveredElementId { get; private set; }

        public HierarchyDefinition[] ColumnsDefinitions =>
            IsTransposed
                ? ProducersCache.Items.Cast<HierarchyDefinition>().ToArray()
                : ConsumersCache.Items.Cast<HierarchyDefinition>().ToArray();

        public HierarchyDefinition[] RowsDefinitions =>
            IsTransposed
                ? ConsumersCache.Items.Cast<HierarchyDefinition>().ToArray()
                : ProducersCache.Items.Cast<HierarchyDefinition>().ToArray();

        public HierarchyGridState GetGridState() => new(this);

        public void SetGridState(HierarchyGridState state, bool useCompare = false)
        {
            if (state.Equals(default))
                return;

            try
            {
                var rowsFlat = RowsDefinitions.FlatList().ToArray();
                if (rowsFlat.Length == state.RowToggles.Length)
                    Parallel.For(
                        0,
                        state.RowToggles.Length,
                        i => rowsFlat[i].IsExpanded = state.RowToggles[i]
                    );
                else
                    rowsFlat
                        .AsParallel()
                        .ForAll(x =>
                        {
                            x.IsExpanded = true;
                        });

                var columnsFlat = ColumnsDefinitions.FlatList().ToArray();
                if (columnsFlat.Length == state.ColumnToggles.Length)
                    Parallel.For(
                        0,
                        state.ColumnToggles.Length,
                        i => columnsFlat[i].IsExpanded = state.ColumnToggles[i]
                    );
                else
                    columnsFlat
                        .AsParallel()
                        .ForAll(x =>
                        {
                            x.IsExpanded = true;
                        });

                VerticalOffset = state.VerticalOffset;
                HorizontalOffset = state.HorizontalOffset;

                SelectedCells.Clear();

                if (useCompare)
                {
                    SelectedCells.AddRange(MatchPositionedCells(state.Selections));
                }
                else
                {
                    SelectedCells.AddRange(state.Selections);
                }
            }
            catch (Exception)
            {
                VerticalOffset = 0;
                HorizontalOffset = 0;
            }

            Observable.Return(false).InvokeCommand(DrawGridCommand);
        }

        private IEnumerable<PositionedCell> MatchPositionedCells(IEnumerable<PositionedCell> cells)
        {
            var producers = ProducersCache.Items.FlatList().ToSeq();
            var consumers = ConsumersCache.Items.FlatList().ToSeq();

            return cells
                .AsParallel()
                .Select(pc =>
                {
                    var producer = producers.Find(p => p.CompareTo(pc.ProducerDefinition) == 0);
                    var consumer = consumers.Find(p => p.CompareTo(pc.ConsumerDefinition) == 0);

                    return from p in producer
                        from c in consumer
                        select new PositionedCell
                        {
                            ProducerDefinition = p,
                            ConsumerDefinition = c
                        };
                })
                .Somes();
        }

        public HierarchyGridState GridState
        {
            get => GetGridState();
            set => SetGridState(value);
        }

        public ReactiveCommand<bool, RxUnit> DrawGridCommand { get; private set; }
        public Interaction<RxUnit, RxUnit> DrawGridInteraction { get; } =
            new(RxApp.MainThreadScheduler);

        public ReactiveCommand<
            (Option<PositionedCell>, Option<PositionedDefinition>),
            RxUnit
        > HandleTooltipCommand { get; private set; }
        public ReactiveCommand<RxUnit, RxUnit> CloseTooltip { get; }
        public Interaction<RxUnit, RxUnit> CloseTooltipInteraction { get; } =
            new(RxApp.MainThreadScheduler);
        public Interaction<PositionedCell, RxUnit> ShowTooltipInteraction { get; } =
            new(RxApp.MainThreadScheduler);
        public Interaction<PositionedDefinition, RxUnit> ShowHeaderTooltipInteraction { get; } =
            new(RxApp.MainThreadScheduler);

        public ReactiveCommand<CopyMode, RxUnit> CopyToClipboardCommand { get; private set; }
        public Interaction<string, RxUnit> FillClipboardInteraction { get; } =
            new(RxApp.MainThreadScheduler);

        public ReactiveCommand<bool, RxUnit> ToggleStatesCommand { get; private set; }

        public ReactiveCommand<RxUnit, RxUnit> ToggleCrosshairCommand { get; private set; }
        public ReactiveCommand<RxUnit, RxUnit> ToggleTransposeCommand { get; private set; }
        public ReactiveCommand<RxUnit, RxUnit> ClearHighlightsCommand { get; private set; }

        public Queue<IDisposable> ResizeObservables { get; } = new();

        public HierarchyGridViewModel()
        {
            Activator = new ViewModelActivator();

            RegisterDefaultInteractions(this);

            CloseTooltip = ReactiveCommand.CreateFromObservable(
                () => CloseTooltipInteraction.Handle(RxUnit.Default)
            );
            InitializeCommands(this);

            this.WhenActivated(disposables =>
            {
                ProducersCache
                    .Connect()
                    .DisposeMany()
                    .Select(_ => ProducersCache.Items.Any())
                    .CombineLatest(
                        ConsumersCache
                            .Connect()
                            .DisposeMany()
                            .Select(_ => ConsumersCache.Items.Any())
                    )
                    .Select(t => t.First || t.Second)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Do(b =>
                    {
                        if (!b)
                            StatusMessage = "No data";
                    })
                    .ToPropertyEx(this, x => x.HasData, scheduler: RxApp.MainThreadScheduler)
                    .DisposeWith(disposables);

                EditedCellChanged
                    .Do(cell =>
                    {
                        EditionContent = cell.Some(c => c.ResultSet.Result)
                            .None(() => string.Empty);
                    })
                    .Select(o => o.IsSome)
                    .ToPropertyEx(
                        this,
                        x => x.IsEditing,
                        initialValue: false,
                        scheduler: RxApp.MainThreadScheduler
                    )
                    .DisposeWith(disposables);

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

                /* Don't allow horizontal offset to go above max offset */
                this.WhenAnyValue(x => x.HorizontalOffset)
                    .CombineLatest(
                        this.WhenAnyValue(x => x.MaxHorizontalOffset),
                        (ho, m) => ho > m && m > 0
                    )
                    .Throttle(TimeSpan.FromMilliseconds(5))
                    .Where(x => x)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .SubscribeSafe(_ => HorizontalOffset = MaxHorizontalOffset)
                    .DisposeWith(disposables);

                /* Don't allow vertical offset to go above max offset */
                this.WhenAnyValue(x => x.VerticalOffset)
                    .CombineLatest(
                        this.WhenAnyValue(x => x.MaxVerticalOffset),
                        (vo, m) => vo > m && m > 0
                    )
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

                this.WhenAnyValue(x => x.HoveredCell)
                    .DistinctUntilChanged()
                    .Where(x => x.IsNone)
                    .ToSignal()
                    .Merge(
                        this.WhenAnyValue(x => x.HoveredDefinitionHeader)
                            .DistinctUntilChanged()
                            .Where(x => x.IsNone)
                            .ToSignal()
                    )
                    .Throttle(TimeSpan.FromMilliseconds(50))
                    .InvokeCommand(CloseTooltip)
                    .DisposeWith(disposables);

                this.WhenAnyValue(x => x.HoveredCell)
                    .DistinctUntilChanged()
                    .CombineLatest(
                        this.WhenAnyValue(x => x.HoveredDefinitionHeader).DistinctUntilChanged()
                    )
                    .Throttle(TimeSpan.FromMilliseconds(1000))
                    .InvokeCommand(HandleTooltipCommand)
                    .DisposeWith(disposables);

                /* Redraw grid when scrolling or changing scale */
                var gridLayoutEventsObservable = this.WhenAnyValue(
                        x => x.HorizontalOffset,
                        x => x.VerticalOffset,
                        x => x.Scale,
                        x => x.Width,
                        x => x.Height
                    )
                    .Throttle(TimeSpan.FromMilliseconds(5))
                    .DistinctUntilChanged();

                var gridMouseEventsObservable = this.WhenAnyValue(
                        x => x.HoveredColumn,
                        x => x.HoveredRow,
                        x => x.HoveredElementId,
                        x => x.FocusCells,
                        x => x.EditedCell
                    )
                    .Throttle(TimeSpan.FromMilliseconds(2))
                    .DistinctUntilChanged();

                // Events starting a grid redraw
                Observable
                    .Merge(
                        this.WhenAnyValue(x => x.IsTransposed).Select(_ => false),
                        this.WhenAnyValue(x => x.Theme).WhereNotNull().Select(_ => false),
                        SelectionChanged.DistinctUntilChanged().Select(_ => false),
                        gridLayoutEventsObservable.Select(_ => false),
                        gridMouseEventsObservable.Select(_ => false),
                        ToggleCrosshairCommand!.Select(_ => false),
                        ClearHighlightsCommand!.Select(_ => false),
                        ToggleStatesCommand!.Select(_ => false)
                    )
                    .Throttle(TimeSpan.FromMilliseconds(10))
                    .InvokeCommand(DrawGridCommand)
                    .DisposeWith(disposables);

                SelectedCells
                    .ObserveCollectionChanges()
                    .Throttle(TimeSpan.FromMilliseconds(10))
                    .Subscribe(_ =>
                    {
                        SelectionChangedSubject.OnNext(Selections);
                        EditedCell = Option<PositionedCell>.None;
                    })
                    .DisposeWith(disposables);
            });
        }

        private static void RegisterDefaultInteractions(HierarchyGridViewModel @this)
        {
            @this.DrawGridInteraction.RegisterHandler(ctx => ctx.SetOutput(RxUnit.Default));
            @this.ShowTooltipInteraction.RegisterHandler(ctx => ctx.SetOutput(RxUnit.Default));
            @this.ShowHeaderTooltipInteraction.RegisterHandler(ctx =>
                ctx.SetOutput(RxUnit.Default)
            );
            @this.CloseTooltipInteraction.RegisterHandler(ctx => ctx.SetOutput(RxUnit.Default));
            @this.FillClipboardInteraction.RegisterHandler(ctx => ctx.SetOutput(RxUnit.Default));
            @this.DrawEditionTextBoxInteraction.RegisterHandler(ctx =>
                ctx.SetOutput(RxUnit.Default)
            );
        }

        private static void InitializeCommands(HierarchyGridViewModel @this)
        {
            @this.DrawGridCommand = ReactiveCommand.CreateFromTask<bool, RxUnit>(async invalidate =>
            {
                if (invalidate)
                    @this.ResultSets.Clear();

                await @this.DrawGridInteraction.Handle(RxUnit.Default);
                return RxUnit.Default;
            });

            @this.DrawGridCommand.ThrownExceptions.SubscribeSafe(e => @this.Log().Error(e));

            @this.HandleTooltipCommand = ReactiveCommand.CreateFromTask(
                async ((Option<PositionedCell>, Option<PositionedDefinition>) t) =>
                {
                    var (pCell, pDef) = t;

                    await pCell.IfSomeAsync(async cell =>
                        await @this.ShowTooltipInteraction.Handle(cell)
                    );

                    await pDef.IfSomeAsync(async definition =>
                        await @this.ShowHeaderTooltipInteraction.Handle(definition)
                    );
                }
            );
            @this.HandleTooltipCommand.ThrownExceptions.SubscribeSafe(e => @this.Log().Error(e));

            @this.ToggleCrosshairCommand = ReactiveCommand.Create(() =>
            {
                @this.EnableCrosshair = !@this.EnableCrosshair;
                return RxUnit.Default;
            });
            @this.ToggleCrosshairCommand.ThrownExceptions.SubscribeSafe(e => @this.Log().Error(e));

            @this.ToggleTransposeCommand = ReactiveCommand.Create(() =>
            {
                @this.IsTransposed = !@this.IsTransposed;
                return RxUnit.Default;
            });
            @this.ToggleTransposeCommand.ThrownExceptions.SubscribeSafe(e => @this.Log().Error(e));

            @this.ClearHighlightsCommand = ReactiveCommand.CreateFromObservable(
                () => Observable.Start(() => @this.ClearHighlights())
            );
            @this.ClearHighlightsCommand.ThrownExceptions.SubscribeSafe(e => @this.Log().Error(e));

            @this.CopyToClipboardCommand = ReactiveCommand.CreateFromTask(
                async (CopyMode mode) =>
                {
                    var content = @this.CreateClipboardContent(mode);
                    await @this.FillClipboardInteraction.Handle(content);
                }
            );
            @this.CopyToClipboardCommand.ThrownExceptions.SubscribeSafe(e => @this.Log().Error(e));

            @this.ToggleStatesCommand = ReactiveCommand.CreateFromObservable(
                (bool expanded) =>
                    Observable.Start(() =>
                    {
                        if (expanded)
                        {
                            @this.ColumnsDefinitions.ExpandAll();
                            @this.RowsDefinitions.ExpandAll();
                        }
                        else
                        {
                            @this.ColumnsDefinitions.FoldAll();
                            @this.RowsDefinitions.FoldAll();
                        }
                    })
            );
        }

        public void Set(HierarchyDefinitions hierarchyDefinitions, bool preserveSizes = false)
        {
            Clear(preserveSizes);

            ProducersCache.AddOrUpdate(hierarchyDefinitions.Producers);
            ConsumersCache.AddOrUpdate(hierarchyDefinitions.Consumers);

            RowsHeadersWidth = Enumerable
                .Range(0, RowsDefinitions.TotalDepth(true))
                .Select(_ => DefaultHeaderWidth)
                .ToArray();

            ColumnsHeadersHeight = Enumerable
                .Range(0, ColumnsDefinitions.TotalDepth(true))
                .Select(_ => DefaultHeaderHeight)
                .ToArray();

            var columnsCount = ColumnsDefinitions.TotalCount(true);
            if (!preserveSizes || columnsCount != ColumnsWidths.Count)
            {
                ColumnsWidths.Clear();
                for (int x = 0; x <= columnsCount; x++)
                    ColumnsWidths.Add(x, DefaultColumnWidth);
            }

            var rowsCount = RowsDefinitions.TotalCount(true);
            if (!preserveSizes || rowsCount != RowsHeights.Count)
            {
                RowsHeights.Clear();
                for (int x = 0; x <= rowsCount; x++)
                    RowsHeights.Add(x, DefaultRowHeight);
            }

            Observable.Return(true).InvokeCommand(DrawGridCommand);
        }

        public void Clear(bool preserveSizes = false)
        {
            ProducersCache.Clear();
            ConsumersCache.Clear();
            SelectedCells.Clear();

            if (!preserveSizes)
            {
                ColumnsWidths.Clear();
                RowsHeights.Clear();
            }

            HorizontalOffset = 0;
            VerticalOffset = 0;

            ClearCrosshair();
            ClearCoordinates();
        }

        public void ClearCrosshair()
        {
            HoveredColumn = -1;
            HoveredRow = -1;
            HoveredElementId = Guid.Empty;
        }

        public void ClearCoordinates()
        {
            HeadersCoordinates.Clear();
            CellsCoordinates.Clear();
            GlobalHeadersCoordinates.Clear();
        }

        public void ClearHighlights()
        {
            foreach (
                var hdef in ColumnsDefinitions
                    .FlatList()
                    .Concat(RowsDefinitions.FlatList())
                    .Where(x => x.IsHighlighted)
            )
            {
                hdef.IsHighlighted = false;
            }
        }

        public Seq<PositionedCell> DrawnCells { get; private set; }

        public Seq<PositionedCell> GetDrawnCells(double width, double height, bool invalidate)
        {
            DrawnCells = GetDrawnCells(
                HorizontalOffset,
                VerticalOffset,
                width,
                height,
                Scale,
                invalidate
            );
            return DrawnCells;
        }

        private Seq<PositionedCell> GetDrawnCells(
            int hIndex,
            int vIndex,
            double width,
            double height,
            double scale,
            bool invalidate
        )
        {
            static IEnumerable<(double coord, double size, int index, T definition)> FindCells<T>(
                int startIndex,
                double offset,
                double maxSpace,
                Dictionary<int, double> sizes,
                T[] definitions
            )
                where T : HierarchyDefinition
            {
                int index = 0;
                double space = offset;

                var frozens = definitions.Where(x => x.Frozen).ToArray();

                int cnt = 0;
                foreach (var frozen in frozens)
                {
                    var size = sizes[frozen.Position];
                    yield return (space, size, cnt++, frozen);
                    index++;
                    space += size;
                }

                while (space < maxSpace && startIndex + index < definitions.Length)
                {
                    var size = sizes[startIndex + index];
                    yield return (space, size, startIndex + index, definitions[startIndex + index]);
                    space += size;
                    index++;
                }
            }

            if (invalidate)
                ResultSets.Clear();

            var rowDefinitions = RowsDefinitions.Leaves().ToArray();
            var colDefinitions = ColumnsDefinitions.Leaves().ToArray();

            // Determine which cells can be drawn.
            var firstColumn = hIndex;
            var firstRow = vIndex;

            var availableWidth = width / scale;
            var availableHeight = height / scale;

            var columns = FindCells(
                    firstColumn,
                    RowsHeadersWidth?.Sum() ?? 0d,
                    availableWidth,
                    ColumnsWidths,
                    colDefinitions
                )
                .ToArray();
            var rows = FindCells(
                    firstRow,
                    ColumnsHeadersHeight?.Sum() ?? 0d,
                    availableHeight,
                    RowsHeights,
                    rowDefinitions
                )
                .ToArray();

            var pCells = columns
                .AsParallel()
                .SelectMany(c =>
                    rows.Select(r =>
                    {
                        var pCell = new PositionedCell
                        {
                            Left = c.coord,
                            Width = c.size,
                            Top = r.coord,
                            Height = r.size,
                            HorizontalPosition = c.index,
                            VerticalPosition = r.index,
                            ConsumerDefinition =
                                (IsTransposed ? r.definition : c.definition) as ConsumerDefinition,
                            ProducerDefinition =
                                (IsTransposed ? c.definition : r.definition) as ProducerDefinition
                        };

                        pCell.ResultSet = ResultSets.FindOrAdd(
                            (pCell.ProducerDefinition.Guid, pCell.ConsumerDefinition.Guid),
                            () =>
                                HierarchyDefinition.Resolve(
                                    pCell.ProducerDefinition,
                                    pCell.ConsumerDefinition
                                )
                        );

                        return pCell;
                    })
                )
                .ToSeq();

            return pCells.Strict();
        }

        public Option<PositionedCell> FindHoveredCell()
        {
            if (HoveredColumn == -1 || HoveredRow == -1)
                return Option<PositionedCell>.None;

            return CellsCoordinates
                .Select(t => Option<PositionedCell>.Some(t.Cell))
                .FirstOrDefault(
                    o =>
                        o.Match(
                            c =>
                                c.VerticalPosition == HoveredRow
                                && c.HorizontalPosition == HoveredColumn,
                            () => false
                        ),
                    Option<PositionedCell>.None
                );
        }

        internal void HandleMouseDown(
            double x,
            double y,
            bool isShiftPressed,
            bool isCtrlPressed,
            bool isRightClick = false,
            double screenScale = 1d
        )
        {
            if (!IsValid)
                return;

            EditedCell = Option<PositionedCell>.None;

            // Find corresponding element
            if (!isRightClick && x <= RowsHeadersWidth.Sum() && y <= ColumnsHeadersHeight.Sum())
            {
                /* Global header */
                FindGlobalAction(x, y)
                    .IfSome(a =>
                    {
                        a();
                        Observable.Return(false).InvokeCommand(DrawGridCommand);
                    });
            }
            else
            {
                var element = FindCoordinates(x, y, screenScale);
                element.Match(
                    c =>
                    {
                        c.Match(
                            cell => CellClick(cell, isShiftPressed, isCtrlPressed, isRightClick),
                            () => { }
                        );
                    },
                    d =>
                    {
                        if (!isRightClick)
                            d.Match(pdef => HeaderClick(pdef.Definition), () => { });
                    }
                );
            }
        }

        private void CellClick(
            PositionedCell cell,
            bool isShiftPressed,
            bool isCtrlPressed,
            bool isRightClick
        )
        {
            HandleSelection(cell, isShiftPressed, isCtrlPressed, isRightClick);
        }

        private void HandleSelection(
            PositionedCell cell,
            bool isShiftPressed,
            bool isCtrlPressed,
            bool isRightClick
        )
        {
            switch (SelectionMode)
            {
                case SelectionMode.Single:
                    HandleSingleSelection(cell);
                    break;

                case SelectionMode.MultiExtended:
                    HandleMultiExtendedSelection(cell, isShiftPressed, isCtrlPressed, isRightClick);
                    break;

                case SelectionMode.MultiSimple:
                    HandleMultiSimpleSelection(cell);
                    break;

                case SelectionMode.None:
                default:
                    SelectedCells.Clear();
                    break;
            }
        }

        private void HandleMultiExtendedSelection(
            PositionedCell cell,
            bool isShiftPressed,
            bool isCtrlPressed,
            bool isRightClick
        )
        {
            // Right clicking shouldn't reset current selection
            if (isRightClick && SelectedCells.Contains(cell))
                return;

            if (isCtrlPressed)
            {
                if (SelectedCells.Contains(cell))
                    SelectedCells.Remove(cell);
                else
                    SelectedCells.Add(cell);
            }
            else if (isShiftPressed && SelectedCells.Count > 0)
            {
                var lastSelection = SelectedCells.Last();
                var rows = Enumerable
                    .Range(
                        Math.Min(lastSelection.VerticalPosition, cell.VerticalPosition),
                        Math.Abs(lastSelection.VerticalPosition - cell.VerticalPosition) + 1
                    )
                    .ToArr();
                var columns = Enumerable
                    .Range(
                        Math.Min(lastSelection.HorizontalPosition, cell.HorizontalPosition),
                        Math.Abs(lastSelection.HorizontalPosition - cell.HorizontalPosition) + 1
                    )
                    .ToArr();

                var rangeCells = CellsCoordinates
                    .Where(t =>
                        rows.Contains(t.Cell.VerticalPosition)
                        && columns.Contains(t.Cell.HorizontalPosition)
                    )
                    .Select(t => t.Cell)
                    .ToList();

                /* Prevent double selection */
                SelectedCells.AddRange(rangeCells.Where(rc => !SelectedCells.Contains(rc)));
            }
            else
            {
                SelectedCells.Clear();
                SelectedCells.Add(cell);
            }
        }

        private void HandleMultiSimpleSelection(PositionedCell cell)
        {
            if (SelectedCells.Count > 1 && SelectedCells.Contains(cell))
                SelectedCells.Remove(cell);
            else
                SelectedCells.Add(cell);
        }

        private void HandleSingleSelection(PositionedCell cell)
        {
            SelectedCells.Clear();
            SelectedCells.Add(cell);
        }

        private void HeaderClick(HierarchyDefinition hdef)
        {
            if (hdef.HasChild && hdef.CanToggle)
                hdef.IsExpanded = !hdef.IsExpanded;
            else
                hdef.IsHighlighted = !hdef.IsHighlighted;

            Observable.Return(false).InvokeCommand(DrawGridCommand);
        }

        internal void HandleDoubleClick(double x, double y, double screenScale)
        {
            if (ColumnsDefinitions?.Length > 0 && RowsDefinitions?.Length > 0)
            {
                var cell = FindCoordinates(x, y, screenScale);
                EditedCell = cell.Match(pc => pc, _ => Option<PositionedCell>.None);
            }
        }

        internal void HandleMouseLeft()
        {
            HoveredCell = Option<PositionedCell>.None;
            HoveredElementId = Guid.Empty;
            Observable.Return(RxUnit.Default).InvokeCommand(CloseTooltip);
            ClearCrosshair();
        }

        internal void HandleMouseOver(double x, double y, double screenScale)
        {
            if (RowsHeadersWidth?.Any() != true || ColumnsHeadersHeight?.Any() != true)
            {
                HoveredCell = Option<PositionedCell>.None;
                HoveredDefinitionHeader = Option<PositionedDefinition>.None;
                HoveredElementId = Guid.Empty;
                return;
            }

            var element = FindCoordinates(x, y, screenScale);
            element.Match(
                cell =>
                {
                    HoveredCell = cell;
                    HoveredDefinitionHeader = Option<PositionedDefinition>.None;
                    HoveredElementId = Guid.Empty;

                    cell.Match(
                        s =>
                        {
                            HoveredColumn = s.HorizontalPosition;
                            HoveredRow = s.VerticalPosition;
                        },
                        () =>
                        {
                            HoveredColumn = -1;
                            HoveredRow = -1;
                        }
                    );
                },
                hdef =>
                {
                    HoveredCell = Option<PositionedCell>.None;
                    HoveredDefinitionHeader = hdef;
                    hdef.Match(
                        s =>
                        {
                            HoveredElementId = s.Definition.Guid;
                            if (
                                s.Definition is ConsumerDefinition consumer
                                && consumer.Count() == 1
                            )
                            {
                                HoveredColumn = ColumnsDefinitions.GetPosition(consumer);
                                HoveredRow = -1;
                            }
                            else if (
                                s.Definition is ProducerDefinition producer
                                && producer.Count() == 1
                            )
                            {
                                HoveredRow = RowsDefinitions.GetPosition(producer);
                                HoveredColumn = -1;
                            }
                            else
                            {
                                HoveredColumn = -1;
                                HoveredRow = -1;
                            }
                        },
                        () =>
                        {
                            HoveredElementId = GlobalHeadersCoordinates
                                .Find(t => t.Coord.Contains(x, y))
                                .Some(t => t.Guid)
                                .None(() => Guid.Empty);

                            HoveredColumn = -1;
                            HoveredRow = -1;
                        }
                    );
                }
            );
        }

        public Option<Action> FindGlobalAction(double x, double y) =>
            GlobalHeadersCoordinates
                .Find(t => t.Coord.Contains(x, y))
                .Match(s => s.Action, () => Option<Action>.None);

        public Either<Option<PositionedDefinition>, Option<PositionedCell>> FindCoordinates(
            double x,
            double y,
            double screenScale
        )
        {
            if (
                x <= RowsHeadersWidth.Sum() * screenScale
                || y <= ColumnsHeadersHeight.Sum() * screenScale
            )
            {
                return HeadersCoordinates
                    .AsParallel()
                    .Find(t => t.Coord.Contains(x, y))
                    .Match(s => s.Definition, () => Option<PositionedDefinition>.None);
            }
            else
            {
                return CellsCoordinates
                    .AsParallel()
                    .Find(t => t.Coord.Contains(x, y))
                    .Match(s => s.Cell, () => Option<PositionedCell>.None);
            }
        }
    }
}
