using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LanguageExt;
using ReactiveUI;
using ReactiveUI.Primitives;
using ReactiveUI.Primitives.Disposables;
using ReactiveUI.Primitives.Signals;
using ReactiveUI.SourceGenerators;
using Splat;
using RxCommand = ReactiveUI.ReactiveCommand<
    ReactiveUI.Primitives.RxVoid,
    ReactiveUI.Primitives.RxVoid
>;
using RxUnit = ReactiveUI.Primitives.RxVoid;

namespace HierarchyGrid.Definitions;

public partial class HierarchyGridViewModel : ReactiveObject, IActivatableViewModel
{
    public ViewModelActivator Activator { get; } = new();
    public bool IsValid => RowsHeadersWidth.Length > 0 && ColumnsHeadersHeight.Length > 0;

    [Reactive]
    public partial Seq<ProducerDefinition> Producers { get; private set; }

    [Reactive]
    public partial Seq<ConsumerDefinition> Consumers { get; private set; }

    /// <summary>
    /// Indicates whether the grid has at least one producer and one consumer definition
    /// </summary>
    [ObservableAsProperty(PropertyName = "HasData")]
    private IObservable<bool> HasDataObservable =>
        this.WhenAnyValue(x => x.Producers)
            .Select(seq => seq.Length > 0)
            .CombineLatest(
                this.WhenAnyValue(x => x.Consumers).Select(seq => seq.Length > 0),
                (a, b) => (First: a, Second: b)
            )
            .Select(t => t is { First: true, Second: true })
            .ObserveOn(RxSchedulers.MainThreadScheduler)
            .Do(b =>
            {
                if (!b && string.IsNullOrEmpty(StatusMessage))
                    StatusMessage = "No data";
            })
            .ObserveOn(RxSchedulers.MainThreadScheduler);

    /// <summary>
    /// Message displayed when the grid has no data
    /// </summary>
    [Reactive]
    public partial string? StatusMessage { get; set; }

    [Reactive]
    public partial string EditionContent { get; set; }

    /// <summary>
    /// Stores the mapping of producer and consumer pairs to their associated <see cref="ResultSet"/> objects.
    /// </summary>
    private AtomHashMap<CellId, ResultSet> ResultSets { get; } =
        Prelude.AtomHashMap<CellId, ResultSet>();

    internal ObservableUniqueCollection<PositionedCell> SelectedCells { get; } = [];

    public Seq<PositionedCell> Selections
    {
        get => SelectedCells.ToSeq();
        set
        {
            SelectedCells.Clear();

            if (value.IsEmpty || SelectionMode == SelectionMode.None)
                return;

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
            }
        }
    }

    private Signal<Seq<PositionedCell>> SelectionChangedSubject { get; } = new();

    public IObservable<Seq<PositionedCell>> SelectionChanged =>
        SelectionChangedSubject.AsObservable().Publish().RefCount();

    /// <summary>
    /// Represents the cell currently being edited within the hierarchy grid.
    /// </summary>
    [Reactive]
    public partial Option<PositionedCell> EditedCell { get; internal set; }

    [Reactive]
    public partial Option<PositionedDefinition> HoveredDefinitionHeader { get; internal set; }

    /// <summary>
    /// An observable stream that signals whenever the currently edited cell changes.
    /// </summary>
    public IObservable<Option<PositionedCell>> EditedCellChanged =>
        this.WhenAnyValue(x => x.EditedCell).Publish().RefCount();

    /// <summary>
    /// Indicates whether the grid is currently in editing mode. True when <see cref="EditedCell"/> is some
    /// and its associated <see cref="ResultSet"/> has a defined editor.
    /// </summary>
    [ObservableAsProperty(PropertyName = "IsEditing")]
    private IObservable<bool> IsEditingObservable =>
        EditedCellChanged
            .Select(cell =>
            {
                /* Editor is none if no editing has been defined or if the cell is locked */
                var editor = from c in cell from e in c.ResultSet.Editor select e;
                editor.IfSome(_ =>
                    EditionContent = cell.Some(c => c.ResultSet.Result).None(() => string.Empty)
                );
                return editor.IsSome;
            })
            .ObserveOn(RxSchedulers.MainThreadScheduler);

    public ReactiveCommand<Seq<PositionedCell>, RxVoid> DrawEditionTextBox { get; }
    public Interaction<Seq<PositionedCell>, RxUnit> DrawEditionTextBoxInteraction { get; } =
        new(RxSchedulers.MainThreadScheduler);

    /// <summary>
    /// Cells with extra rendering elements
    /// </summary>
    [Reactive]
    public partial HashMap<PositionedCell, FocusCellInfo> FocusCells { get; set; }

    /// <summary>
    /// Represents a collection of coordinates and corresponding positioned definitions
    /// for the headers within the hierarchy grid.
    /// </summary>
    public ConcurrentBag<(
        ElementCoordinates Coord,
        PositionedDefinition Definition
    )> HeadersCoordinates { get; } = [];

    /// <summary>
    /// Represents a collection of tuples that associate the coordinates of grid elements with their corresponding positioned cells.
    /// </summary>
    public ConcurrentBag<(
        ElementCoordinates Coord,
        PositionedCell Cell
    )> CellsCoordinates { get; } = [];

    private HashMap<(int Row, int Column), PositionedCell> CellsCoordinatesMap { get; set; }

    public ConcurrentBag<(
        ElementCoordinates Coord,
        Guid Guid,
        Action Action
    )> GlobalHeadersCoordinates { get; } = [];

    [Reactive]
    public partial int HorizontalOffset { get; set; }

    [Reactive]
    public partial int VerticalOffset { get; set; }

    [Reactive]
    public partial double Scale { get; set; } = 1d;

    [Reactive]
    public partial double Width { get; set; } = double.NaN;

    [Reactive]
    public partial double Height { get; set; } = double.NaN;

    [Reactive]
    public partial int MaxHorizontalOffset { get; set; }

    [Reactive]
    public partial int MaxVerticalOffset { get; set; }

    [Reactive]
    public partial bool IsTransposed { get; set; }

    [Reactive]
    public partial bool EnableCrosshair { get; set; }

    [Reactive]
    public partial int HoveredColumn { get; set; }

    [Reactive]
    public partial int HoveredRow { get; set; }

    [Reactive]
    public partial SelectionMode SelectionMode { get; set; }

    [Reactive]
    public partial CellTextAlignment TextAlignment { get; set; } = CellTextAlignment.Right;

    [Reactive]
    public partial ITheme Theme { get; set; } = HierarchyGridTheme.Default;

    [Reactive]
    public partial Option<PositionedCell> HoveredCell { get; set; }

    [Reactive]
    public partial Guid HoveredElementId { get; private set; }

    [ObservableAsProperty(PropertyName = "ColumnsDefinitions")]
    private IObservable<Seq<HierarchyDefinition>> ColumnsDefinitionsObservable =>
        this.WhenAnyValue(x => x.Consumers, x => x.Producers, x => x.IsTransposed)
            .Select(t =>
            {
                var (consumers, producers, isTransposed) = t;
                return isTransposed
                    ? producers.Cast<HierarchyDefinition>()
                    : consumers.Cast<HierarchyDefinition>();
            });

    [ObservableAsProperty(PropertyName = "RowsDefinitions")]
    private IObservable<Seq<HierarchyDefinition>> RowsDefinitionsObservable =>
        this.WhenAnyValue(x => x.Consumers, x => x.Producers, x => x.IsTransposed)
            .Select(t =>
            {
                var (consumers, producers, isTransposed) = t;
                return isTransposed
                    ? consumers.Cast<HierarchyDefinition>()
                    : producers.Cast<HierarchyDefinition>();
            });

    public HierarchyGridState GetGridState() => new(this);

    public void SetGridState(HierarchyGridState state, bool useCompare = false)
    {
        if (state.Equals(default))
            return;

        try
        {
            var rowsFlat = RowsDefinitions.FlatList();
            if (rowsFlat.Length == state.RowToggles.Length)
            {
                Parallel.For(
                    0,
                    state.RowToggles.Length,
                    i => rowsFlat[i].IsExpanded = state.RowToggles[i]
                );
            }
            else
            {
                rowsFlat
                    .AsParallel()
                    .ForAll(x =>
                    {
                        x.IsExpanded = true;
                    });
            }

            var columnsFlat = ColumnsDefinitions.FlatList();
            if (columnsFlat.Length == state.ColumnToggles.Length)
            {
                Parallel.For(
                    0,
                    state.ColumnToggles.Length,
                    i => columnsFlat[i].IsExpanded = state.ColumnToggles[i]
                );
            }
            else
            {
                columnsFlat
                    .AsParallel()
                    .ForAll(x =>
                    {
                        x.IsExpanded = true;
                    });
            }

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

        Signal.Return((false, "gridstate")).InvokeCommand(DrawGridCommand);
    }

    private IEnumerable<PositionedCell> MatchPositionedCells(IEnumerable<PositionedCell> cells)
    {
        var producers = Producers.FlatList();
        var consumers = Consumers.FlatList();

        return cells
            .AsParallel()
            .Select(pc =>
            {
                var producer = producers.Find(p => p.CompareTo(pc.ProducerDefinition) == 0);
                var consumer = consumers.Find(p => p.CompareTo(pc.ConsumerDefinition) == 0);

                return from p in producer
                    from c in consumer
                    select new PositionedCell { ProducerDefinition = p, ConsumerDefinition = c };
            })
            .Somes();
    }

    public HierarchyGridState GridState
    {
        get => GetGridState();
        set => SetGridState(value);
    }

    public ReactiveCommand<(bool, string), RxVoid> DrawGridCommand { get; }
    public Interaction<RxUnit, RxUnit> DrawGridInteraction { get; } =
        new(RxSchedulers.MainThreadScheduler);

    public ReactiveCommand<
        (Option<PositionedCell>, Option<PositionedDefinition>),
        RxUnit
    > HandleTooltipCommand { get; }
    public RxCommand CloseTooltip { get; }
    public Interaction<RxUnit, RxUnit> CloseTooltipInteraction { get; } =
        new(RxSchedulers.MainThreadScheduler);
    public Interaction<PositionedCell, RxUnit> ShowTooltipInteraction { get; } =
        new(RxSchedulers.MainThreadScheduler);
    public Interaction<PositionedDefinition, RxUnit> ShowHeaderTooltipInteraction { get; } =
        new(RxSchedulers.MainThreadScheduler);

    public ReactiveCommand<CopyMode, RxUnit> CopyToClipboardCommand { get; }
    public Interaction<string, RxUnit> FillClipboardInteraction { get; } =
        new(RxSchedulers.MainThreadScheduler);

    [ObservableAsProperty(PropertyName = "IsCopyingToClipboard")]
    private IObservable<bool> IsCopyingToClipboardObservable =>
        CopyToClipboardCommand.IsExecuting.ObserveOn(RxSchedulers.MainThreadScheduler);

    public ReactiveCommand<bool, RxUnit> ToggleStatesCommand { get; }

    public RxCommand ToggleCrosshairCommand { get; }
    public RxCommand ToggleTransposeCommand { get; }
    public RxCommand ClearHighlightsCommand { get; }

    public Queue<IDisposable> ResizeObservables { get; } = new();

    public HierarchyGridViewModel()
    {
        RowsHeadersWidth = [];
        ColumnsHeadersHeight = [];
        HoveredColumn = -1;
        HoveredRow = -1;

        CellFontSize = DefaultFontSize;
        HeaderFontSize = DefaultFontSize;

        EditionContent = string.Empty;

        RegisterDefaultInteractions(this);
        DrawEditionTextBox = ReactiveCommand.CreateFromObservable(
            (Seq<PositionedCell> cells) => DrawEditionTextBoxInteraction.Handle(cells)
        );

        DrawGridCommand = CreateDrawGridCommand();
        HandleTooltipCommand = CreateHandleTooltipCommand();
        CloseTooltip = ReactiveCommand.CreateFromObservable(
            () => CloseTooltipInteraction.Handle(RxUnit.Default)
        );
        ToggleCrosshairCommand = ReactiveCommand.Create(ToggleCrossHair);
        ToggleTransposeCommand = ReactiveCommand.Create(ToggleTranspose);
        ClearHighlightsCommand = ReactiveCommand.CreateRunInBackground(ClearHighlights);
        ToggleStatesCommand = ReactiveCommand.CreateRunInBackground(
            (bool expanded) => ToggleStates(expanded)
        );
        CopyToClipboardCommand = CreateCopyToClipboardCommand();

        this.WhenActivated(disposables =>
        {
            ManageScaleConstraints(disposables);
            ManageOffsets(disposables);
            HandleTooltipDisplay(disposables);
            TriggerGridDrawing(disposables);
            ManageSelectionChange(disposables);
        });

        InitializeOAPH();
    }

    private void ManageSelectionChange(MultipleDisposable disposables)
    {
        SelectedCells
            .ObserveCollectionChanges()
            .Throttle(TimeSpan.FromMilliseconds(10))
            .Subscribe(_ =>
            {
                SelectionChangedSubject.OnNext(Selections);
                EditedCell = Option<PositionedCell>.None;
            })
            .DisposeWith(disposables);
    }

    private void ManageScaleConstraints(MultipleDisposable disposables)
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
    }

    private void ManageOffsets(MultipleDisposable disposables)
    {
        /* Don't allow horizontal offset to go above max offset */
        this.WhenAnyValue(x => x.HorizontalOffset)
            .CombineLatest(
                this.WhenAnyValue(x => x.MaxHorizontalOffset),
                (ho, m) => ho > m && m > 0
            )
            .Throttle(TimeSpan.FromMilliseconds(5))
            .Where(x => x)
            .ObserveOn(RxSchedulers.MainThreadScheduler)
            .SubscribeSafe(_ => HorizontalOffset = MaxHorizontalOffset)
            .DisposeWith(disposables);

        /* Don't allow vertical offset to go above max offset */
        this.WhenAnyValue(x => x.VerticalOffset)
            .CombineLatest(this.WhenAnyValue(x => x.MaxVerticalOffset), (vo, m) => vo > m && m > 0)
            .Throttle(TimeSpan.FromMilliseconds(5))
            .Where(x => x)
            .ObserveOn(RxSchedulers.MainThreadScheduler)
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
    }

    private void HandleTooltipDisplay(MultipleDisposable disposables)
    {
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
                this.WhenAnyValue(x => x.HoveredDefinitionHeader).DistinctUntilChanged(),
                (a, b) => (a, b)
            )
            .Throttle(TimeSpan.FromMilliseconds(1000))
            .InvokeCommand(HandleTooltipCommand)
            .DisposeWith(disposables);
    }

    private void TriggerGridDrawing(MultipleDisposable disposables)
    {
        /* Redraw grid when scrolling or changing scale */
        var gridLayoutEventsObservable = this.WhenAnyValue(
                x => x.HorizontalOffset,
                x => x.VerticalOffset,
                x => x.Scale,
                x => x.Width,
                x => x.Height
            )
            .Where(t => t is { Value1: >= 0, Value2: >= 0 })
            .Throttle(TimeSpan.FromMilliseconds(5))
            .DistinctUntilChanged()
            .Publish()
            .RefCount();

        var gridMouseEventsObservable = this.WhenAnyValue(
                x => x.HoveredColumn,
                x => x.HoveredRow,
                x => x.HoveredElementId,
                x => x.FocusCells,
                x => x.EditedCell
            )
            .Throttle(TimeSpan.FromMilliseconds(2))
            .DistinctUntilChanged()
            .Publish()
            .RefCount();

        // Events starting a grid redraw
        Signal
            .Merge(
                this.WhenAnyValue(x => x.IsTransposed)
                    .Do(isTransposed =>
                    {
                        /* Need to adapt headers size on transpose */
                        SetHeadersDimension(isTransposed);
                    })
                    .Select(_ => (false, "transpose")),
                this.WhenAnyValue(x => x.Theme)
                    .Where(x => x is not null)
                    .Select(_ => (false, "theme")),
                SelectionChanged.DistinctUntilChanged().Select(_ => (false, "selection")),
                gridLayoutEventsObservable.Select(_ => (false, "layout")),
                gridMouseEventsObservable.Select(_ => (false, "mouse")),
                ToggleCrosshairCommand.Select(_ => (false, "toggle crosshair")),
                ClearHighlightsCommand.Select(_ => (false, "highlights")),
                ToggleStatesCommand.Select(_ => (false, "toggle states"))
            )
            .Throttle(TimeSpan.FromMilliseconds(10))
            .InvokeCommand(DrawGridCommand)
            .DisposeWith(disposables);
    }

    private void SetHeadersDimension(bool isTransposed, bool preserveSizes = false)
    {
        var rowDefinitions = !isTransposed
            ? Producers.Cast<HierarchyDefinition>()
            : Consumers.Cast<HierarchyDefinition>();
        var columnDefinitions = !isTransposed
            ? Consumers.Cast<HierarchyDefinition>()
            : Producers.Cast<HierarchyDefinition>();

        RowsHeadersWidth =
        [
            .. Enumerable.Range(0, rowDefinitions.TotalDepth()).Select(_ => DefaultHeaderWidth),
        ];

        ColumnsHeadersHeight =
        [
            .. Enumerable.Range(0, columnDefinitions.TotalDepth()).Select(_ => DefaultHeaderHeight),
        ];

        var columnsCount = columnDefinitions.TotalCount(true);
        if (!preserveSizes || columnsCount != ColumnsWidths.Count)
        {
            ColumnsWidths.Clear();
            for (int x = 0; x <= columnsCount; x++)
                ColumnsWidths.Add(x, DefaultColumnWidth);
        }

        var rowsCount = rowDefinitions.TotalCount(true);
        if (!preserveSizes || rowsCount != RowsHeights.Count)
        {
            RowsHeights.Clear();
            for (int x = 0; x <= rowsCount; x++)
                RowsHeights.Add(x, DefaultRowHeight);
        }
    }

    /// <summary>
    /// Registers default interaction handlers for the specified <see cref="HierarchyGridViewModel"/> instance.
    /// Those interactions do nothing but prevent exceptions if called without real implementation.
    /// </summary>
    private static void RegisterDefaultInteractions(HierarchyGridViewModel @this)
    {
        @this.DrawGridInteraction.RegisterHandler(ctx => ctx.SetOutput(RxUnit.Default));
        @this.ShowTooltipInteraction.RegisterHandler(ctx => ctx.SetOutput(RxUnit.Default));
        @this.ShowHeaderTooltipInteraction.RegisterHandler(ctx => ctx.SetOutput(RxUnit.Default));
        @this.CloseTooltipInteraction.RegisterHandler(ctx => ctx.SetOutput(RxUnit.Default));
        @this.FillClipboardInteraction.RegisterHandler(ctx => ctx.SetOutput(RxUnit.Default));
        @this.DrawEditionTextBoxInteraction.RegisterHandler(ctx => ctx.SetOutput(RxVoid.Default));
    }

    private ReactiveCommand<(bool, string), RxUnit> CreateDrawGridCommand()
    {
        // var command = ReactiveCommand.CreateFromTask<bool, RxUnit>(async invalidate =>
        // {
        //     if (invalidate)
        //         ResultSets.Clear();
        //
        //     await DrawGridInteraction.Handle(RxUnit.Default);
        //     return RxUnit.Default;
        // });

        var command = ReactiveCommand.CreateFromObservable(
            ((bool, string) t) =>
            {
                var (invalidate, source) = t;
                Console.WriteLine($"Drawing grid from {source}");
                if (invalidate)
                    ResultSets.Clear();
                return DrawGridInteraction.Handle(RxVoid.Default);
            }
        );
        command.ThrownExceptions.SubscribeSafe(e => this.Log().Error(e));

        return command;
    }

    private ReactiveCommand<
        (Option<PositionedCell>, Option<PositionedDefinition>),
        RxUnit
    > CreateHandleTooltipCommand()
    {
        var command = ReactiveCommand.CreateFromTask(
            async ((Option<PositionedCell>, Option<PositionedDefinition>) t) =>
            {
                var (pCell, pDef) = t;
                await pCell.IfSomeAsync(async cell => await ShowTooltipInteraction.Handle(cell));
                await pDef.IfSomeAsync(async definition =>
                    await ShowHeaderTooltipInteraction.Handle(definition)
                );
            }
        );

        command.ThrownExceptions.SubscribeSafe(e => this.Log().Error(e));
        return command;
    }

    private void ToggleCrossHair()
    {
        EnableCrosshair = !EnableCrosshair;
    }

    private void ToggleTranspose()
    {
        IsTransposed = !IsTransposed;
    }

    private void ToggleStates(bool expanded)
    {
        if (expanded)
            ExpandAll();
        else
            FoldAll();
    }

    private void ExpandAll()
    {
        ColumnsDefinitions.ExpandAll();
        RowsDefinitions.ExpandAll();
    }

    private void FoldAll()
    {
        ColumnsDefinitions.FoldAll();
        RowsDefinitions.FoldAll();
    }

    private ReactiveCommand<CopyMode, RxUnit> CreateCopyToClipboardCommand()
    {
        var command = ReactiveCommand.CreateFromTask(
            async (CopyMode mode) =>
            {
                var content = await CreateClipboardContent(mode).ConfigureAwait(false);
                await FillClipboardInteraction.Handle(content);
            }
        );

        command.ThrownExceptions.SubscribeSafe(e => this.Log().Error(e));
        return command;
    }

    public void Set(HierarchyDefinitions hierarchyDefinitions, bool preserveSizes = false)
    {
        //Clear(preserveSizes);

        Producers = hierarchyDefinitions.Producers;
        Consumers = hierarchyDefinitions.Consumers;

        SetHeadersDimension(IsTransposed, preserveSizes);
        Signal.Return((true, "set definitions")).InvokeCommand(DrawGridCommand);
    }

    private void Clear(bool preserveSizes = false)
    {
        Producers = Seq<ProducerDefinition>.Empty;
        Consumers = Seq<ConsumerDefinition>.Empty;
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

    private void ClearCrosshair()
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
        CellsCoordinatesMap = CellsCoordinatesMap.Clear();
    }

    private void ClearHighlights()
    {
        foreach (
            var definition in ColumnsDefinitions
                .FlatList()
                .Concat(RowsDefinitions.FlatList())
                .Where(x => x.IsHighlighted)
        )
        {
            definition.IsHighlighted = false;
        }
    }

    /// <summary>
    /// Represents the set of cells currently rendered on the grid, determined by the viewport's dimensions
    /// and the scale or offsets applied.
    /// </summary>
    public Seq<PositionedCell> DrawnCells { get; private set; }

    /// <summary>
    /// Retrieves the collection of cells that are currently drawn within the specified dimensions and updates their state based on the provided parameters.
    /// </summary>
    /// <param name="width">The width of the area for which drawn cells are being retrieved.</param>
    /// <param name="height">The height of the area for which drawn cells are being retrieved.</param>
    /// <param name="invalidate">Specifies whether the existing drawn cells should be invalidated and recomputed.</param>
    /// <returns>A sequence of <see cref="PositionedCell"/> instances representing the cells currently drawn within the specified area.</returns>
    public Seq<PositionedCell> GetDrawnCells(
        double width,
        double height,
        bool invalidate,
        double screenScale = 1d
    )
    {
        DrawnCells = GetDrawnCells(
            HorizontalOffset,
            VerticalOffset,
            width,
            height,
            screenScale * Scale,
            invalidate
        );
        return DrawnCells;
    }

    /// <summary>
    /// Retrieves a sequence of cells based on the current viewport's width, height, horizontal offset, vertical offset, and scale.
    /// Optionally invalidates the cached results before calculation.
    /// </summary>
    /// <param name="width">The width of the viewport.</param>
    /// <param name="height">The height of the viewport.</param>
    /// <param name="invalidate">A boolean indicating whether to invalidate cached cell data before recalculating.</param>
    /// <returns>A sequence of <see cref="PositionedCell"/> instances representing the currently visible cells.</returns>
    private Seq<PositionedCell> GetDrawnCells(
        int hIndex,
        int vIndex,
        double width,
        double height,
        double scale,
        bool invalidate
    )
    {
        if (invalidate)
            ResultSets.Clear();

        var rowDefinitions = RowsDefinitions.Leaves();
        var colDefinitions = ColumnsDefinitions.Leaves();

        // Determine which cells can be drawn.
        var firstColumn = hIndex;
        var firstRow = vIndex;

        var availableWidth = width / scale;
        var availableHeight = height / scale;

        var columns = FindCells(
                firstColumn,
                RowsHeadersWidth.Sum(),
                availableWidth,
                ColumnsWidths,
                colDefinitions
            )
            .ToSeq();
        var rows = FindCells(
                firstRow,
                ColumnsHeadersHeight.Sum(),
                availableHeight,
                RowsHeights,
                rowDefinitions
            )
            .ToSeq();

        var pCells = columns
            .AsParallel()
            .SelectMany(c =>
                rows.Select(r =>
                {
                    var consumer =
                        (IsTransposed ? r.definition : c.definition) as ConsumerDefinition;
                    var producer =
                        (IsTransposed ? c.definition : r.definition) as ProducerDefinition;

                    var resultSet = ResultSets.FindOrAdd(
                        new(
                            producer?.ProducerDefinitionId ?? ProducerDefinitionId.Default,
                            consumer?.ConsumerDefinitionId ?? ConsumerDefinitionId.Default
                        ),
                        () => HierarchyDefinition.Resolve(producer, consumer)
                    );

                    var pCell = new PositionedCell
                    {
                        Left = c.coord,
                        Width = c.size,
                        Top = r.coord,
                        Height = r.size,
                        HorizontalPosition = c.index,
                        VerticalPosition = r.index,
                        ConsumerDefinition = consumer!,
                        ProducerDefinition = producer!,
                        ResultSet = resultSet,
                    };

                    return pCell;
                })
            )
            .ToSeq();

        return pCells.Strict();
    }

    /// <summary>
    /// Finds and retrieves a sequence of cells with their corresponding coordinates, sizes, indices,
    /// and definitions based on the specified start index, offset, maximum space, sizes,
    /// and hierarchy definitions.
    /// </summary>
    /// <typeparam name="T">The type of hierarchy definition, which must derive from <see cref="HierarchyDefinition"/>.</typeparam>
    /// <param name="startIndex">The starting index for searching cells.</param>
    /// <param name="offset">The initial coordinate offset for positioning cells.</param>
    /// <param name="maxSpace">The maximum available space for cells.</param>
    /// <param name="sizes">A dictionary mapping indices to cell sizes.</param>
    /// <param name="definitions">A sequence of hierarchy definitions to be processed.</param>
    /// <returns>An enumerable collection of tuples containing cell coordinates, sizes, indices, and definitions.</returns>
    private static IEnumerable<(double coord, double size, int index, T definition)> FindCells<T>(
        int startIndex,
        double offset,
        double maxSpace,
        Dictionary<int, double> sizes,
        Seq<T> definitions
    )
        where T : HierarchyDefinition
    {
        int index = 0;
        double space = offset;

        int cnt = 0;

        var frozenDefinitions = definitions.Where(x => x.Frozen);

        /* List frozen definitions first */
        foreach (var frozen in frozenDefinitions)
        {
            var size = sizes[frozen.Position];
            yield return (space, size, cnt++, frozen);
            index++;
            space += size;

            /* This would mean the grid has more frozen elements than available space */
            if (space >= maxSpace)
                break;
        }

        /* List definitions until we run out of space, or we've reached the end of available definitions */
        while (space < maxSpace && startIndex + index < definitions.Length)
        {
            var size = sizes[startIndex + index];
            yield return (space, size, startIndex + index, definitions[startIndex + index]);
            space += size;
            index++;
        }
    }

    public Option<PositionedCell> FindHoveredCell()
    {
        if (HoveredColumn == -1 || HoveredRow == -1)
            return Option<PositionedCell>.None;

        if (CellsCoordinatesMap.IsEmpty)
        {
            CellsCoordinatesMap = CellsCoordinates
                .Select(t => ((t.Cell.HorizontalPosition, t.Cell.VerticalPosition), t.Cell))
                .ToHashMap();
        }

        return CellsCoordinatesMap.Find((HoveredColumn, HoveredRow));
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
                    Signal.Return((false, "Global header")).InvokeCommand(DrawGridCommand);
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

            default:
                SelectedCells.Clear();
                break;
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Performance",
        "CA1868:Unnecessary call to 'Contains(item)'",
        Justification = "<Pending>"
    )]
    private void HandleMultiExtendedSelection(
        PositionedCell cell,
        bool isShiftPressed,
        bool isCtrlPressed,
        bool isRightClick
    )
    {
        // Right-clicking shouldn't reset current selection
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
            var lastSelection = SelectedCells[^1];
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

    private void HeaderClick(HierarchyDefinition definition)
    {
        if (definition is { HasChild: true, CanToggle: true })
            definition.IsExpanded = !definition.IsExpanded;
        else
            definition.IsHighlighted = !definition.IsHighlighted;

        Signal.Return((false, "Header")).InvokeCommand(DrawGridCommand);
    }

    internal void HandleDoubleClick(double x, double y, double screenScale)
    {
        if (ColumnsDefinitions.Length <= 0 || RowsDefinitions.Length <= 0)
            return;

        var cell = FindCoordinates(x, y, screenScale);
        EditedCell = cell.Match(pc => pc, _ => Option<PositionedCell>.None);
    }

    internal void HandleMouseLeft()
    {
        HoveredCell = Option<PositionedCell>.None;
        HoveredElementId = Guid.Empty;
        HoveredDefinitionHeader = Option<PositionedDefinition>.None;

        ClearCrosshair();
    }

    private void HoverCell(Option<PositionedCell> cell)
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
    }

    private void HoverHeader(Option<PositionedDefinition> definition, double x, double y)
    {
        HoveredCell = Option<PositionedCell>.None;
        HoveredDefinitionHeader = definition;
        definition.Match(
            s =>
            {
                HoveredElementId = s.Definition.DefinitionId;

                // Reset first
                HoveredRow = -1;
                HoveredColumn = -1;

                switch (s.Definition)
                {
                    case ConsumerDefinition consumer when consumer.Count() == 1:
                    {
                        if (IsTransposed)
                            HoveredRow = RowsDefinitions.GetPosition(consumer);
                        else
                            HoveredColumn = ColumnsDefinitions.GetPosition(consumer);
                        break;
                    }

                    case ProducerDefinition producer when producer.Count() == 1:
                    {
                        if (IsTransposed)
                            HoveredColumn = ColumnsDefinitions.GetPosition(producer);
                        else
                            HoveredRow = RowsDefinitions.GetPosition(producer);
                        break;
                    }

                    default:
                        // Already reset above; nothing to do
                        break;
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

    internal void HandleMouseOver(double x, double y, double screenScale)
    {
        if (RowsHeadersWidth.Length == 0 || ColumnsHeadersHeight.Length == 0)
        {
            HoveredCell = Option<PositionedCell>.None;
            HoveredDefinitionHeader = Option<PositionedDefinition>.None;
            HoveredElementId = Guid.Empty;
            return;
        }

        FindCoordinates(x, y, screenScale)
            .Right(HoverCell)
            .Left(definition => HoverHeader(definition, x, y));
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
        /* Search in headers coordinates if we click inside their bounds */
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

        /* Outside of headers bounds => look in cell coordinates */
        return CellsCoordinates
            .AsParallel()
            .Find(t => t.Coord.Contains(x, y))
            .Match(s => s.Cell, () => Option<PositionedCell>.None);
    }

    public bool IsEmpty() => Producers.IsEmpty || Consumers.IsEmpty;
}
