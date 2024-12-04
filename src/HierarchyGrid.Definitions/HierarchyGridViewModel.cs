using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using DynamicData.Binding;
using LanguageExt;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Splat;
using RxCommand = ReactiveUI.ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit>;
using RxUnit = System.Reactive.Unit;

namespace HierarchyGrid.Definitions;

public partial class HierarchyGridViewModel : ReactiveObject, IActivatableViewModel
{
    public ViewModelActivator Activator { get; } = new();
    public bool IsValid => RowsHeadersWidth.Length > 0 && ColumnsHeadersHeight.Length > 0;

    [Reactive]
    internal Seq<ProducerDefinition> Producers { get; private set; }

    [Reactive]
    internal Seq<ConsumerDefinition> Consumers { get; private set; }

    public bool HasData
    {
        [ObservableAsProperty]
        get;
    }

    [Reactive]
    public string? StatusMessage { get; set; }

    [Reactive]
    public string EditionContent { get; set; }

    private AtomHashMap<(Guid, Guid), ResultSet> ResultSets { get; } =
        Prelude.AtomHashMap<(Guid, Guid), ResultSet>();

    internal ObservableUniqueCollection<PositionedCell> SelectedCells { get; } = new();

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

    private HashMap<(int Row, int Column), PositionedCell> CellsCoordinatesMap { get; set; }

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

    public Seq<HierarchyDefinition> ColumnsDefinitions =>
        IsTransposed
            ? Producers.Cast<HierarchyDefinition>()
            : Consumers.Cast<HierarchyDefinition>();

    public Seq<HierarchyDefinition> RowsDefinitions =>
        IsTransposed
            ? Consumers.Cast<HierarchyDefinition>()
            : Producers.Cast<HierarchyDefinition>();

    public HierarchyGridState GetGridState() => new(this);

    public void SetGridState(HierarchyGridState state, bool useCompare = false)
    {
        if (state.Equals(default))
            return;

        try
        {
            var rowsFlat = RowsDefinitions.FlatList();
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

            var columnsFlat = ColumnsDefinitions.FlatList();
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

    public ReactiveCommand<bool, RxUnit> DrawGridCommand { get; }
    public Interaction<RxUnit, RxUnit> DrawGridInteraction { get; } =
        new(RxApp.MainThreadScheduler);

    public ReactiveCommand<
        (Option<PositionedCell>, Option<PositionedDefinition>),
        RxUnit
    > HandleTooltipCommand { get; }
    public RxCommand CloseTooltip { get; }
    public Interaction<RxUnit, RxUnit> CloseTooltipInteraction { get; } =
        new(RxApp.MainThreadScheduler);
    public Interaction<PositionedCell, RxUnit> ShowTooltipInteraction { get; } =
        new(RxApp.MainThreadScheduler);
    public Interaction<PositionedDefinition, RxUnit> ShowHeaderTooltipInteraction { get; } =
        new(RxApp.MainThreadScheduler);

    public ReactiveCommand<CopyMode, RxUnit> CopyToClipboardCommand { get; }
    public Interaction<string, RxUnit> FillClipboardInteraction { get; } =
        new(RxApp.MainThreadScheduler);

    public ReactiveCommand<bool, RxUnit> ToggleStatesCommand { get; }

    public RxCommand ToggleCrosshairCommand { get; }
    public RxCommand ToggleTransposeCommand { get; }
    public RxCommand ClearHighlightsCommand { get; }

    public Queue<IDisposable> ResizeObservables { get; } = new();

    public HierarchyGridViewModel()
    {
        RowsHeadersWidth = [];
        ColumnsHeadersHeight = [];

        EditionContent = string.Empty;

        RegisterDefaultInteractions(this);

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
            ManageEditionProcess(disposables);
            ManageScaleConstraints(disposables);
            UpdateDataStatus(disposables);
            ManageOffsets(disposables);
            HandleTooltipDisplay(disposables);
            TriggerGridDrawing(disposables);
            ManageSelectionChange(disposables);
        });
    }

    private void ManageEditionProcess(CompositeDisposable disposables)
    {
        EditedCellChanged
            .Do(cell =>
            {
                EditionContent = cell.Some(c => c.ResultSet.Result).None(() => string.Empty);
            })
            .Select(o => o.IsSome)
            .ToPropertyEx(
                this,
                x => x.IsEditing,
                initialValue: false,
                scheduler: RxApp.MainThreadScheduler
            )
            .DisposeWith(disposables);
    }

    private void ManageSelectionChange(CompositeDisposable disposables)
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

    private void ManageScaleConstraints(CompositeDisposable disposables)
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

    private void UpdateDataStatus(CompositeDisposable disposables)
    {
        this.WhenAnyValue(x => x.Producers)
            .Select(seq => seq.Length > 0)
            .CombineLatest(this.WhenAnyValue(x => x.Consumers).Select(seq => seq.Length > 0))
            .Select(t => t.First || t.Second)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Do(b =>
            {
                if (!b && string.IsNullOrEmpty(StatusMessage))
                    StatusMessage = "No data";
            })
            .ToPropertyEx(this, x => x.HasData, scheduler: RxApp.MainThreadScheduler)
            .DisposeWith(disposables);
    }

    private void ManageOffsets(CompositeDisposable disposables)
    {
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
            .CombineLatest(this.WhenAnyValue(x => x.MaxVerticalOffset), (vo, m) => vo > m && m > 0)
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
    }

    private void HandleTooltipDisplay(CompositeDisposable disposables)
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
            .CombineLatest(this.WhenAnyValue(x => x.HoveredDefinitionHeader).DistinctUntilChanged())
            .Throttle(TimeSpan.FromMilliseconds(1000))
            .InvokeCommand(HandleTooltipCommand)
            .DisposeWith(disposables);
    }

    private void TriggerGridDrawing(CompositeDisposable disposables)
    {
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
                ToggleCrosshairCommand.Select(_ => false),
                ClearHighlightsCommand.Select(_ => false),
                ToggleStatesCommand.Select(_ => false)
            )
            .Throttle(TimeSpan.FromMilliseconds(10))
            .InvokeCommand(DrawGridCommand)
            .DisposeWith(disposables);
    }

    private static void RegisterDefaultInteractions(HierarchyGridViewModel @this)
    {
        @this.DrawGridInteraction.RegisterHandler(ctx => ctx.SetOutput(RxUnit.Default));
        @this.ShowTooltipInteraction.RegisterHandler(ctx => ctx.SetOutput(RxUnit.Default));
        @this.ShowHeaderTooltipInteraction.RegisterHandler(ctx => ctx.SetOutput(RxUnit.Default));
        @this.CloseTooltipInteraction.RegisterHandler(ctx => ctx.SetOutput(RxUnit.Default));
        @this.FillClipboardInteraction.RegisterHandler(ctx => ctx.SetOutput(RxUnit.Default));
        @this.DrawEditionTextBoxInteraction.RegisterHandler(ctx => ctx.SetOutput(RxUnit.Default));
    }

    private ReactiveCommand<bool, RxUnit> CreateDrawGridCommand()
    {
        var command = ReactiveCommand.CreateFromTask<bool, RxUnit>(async invalidate =>
        {
            if (invalidate)
                ResultSets.Clear();

            await DrawGridInteraction.Handle(RxUnit.Default);
            return RxUnit.Default;
        });
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
                var content = CreateClipboardContent(mode);
                await FillClipboardInteraction.Handle(content);
            }
        );
        command.ThrownExceptions.SubscribeSafe(e => this.Log().Error(e));
        return command;
    }

    public void Set(HierarchyDefinitions hierarchyDefinitions, bool preserveSizes = false)
    {
        Clear(preserveSizes);

        Producers = hierarchyDefinitions.Producers;
        Consumers = hierarchyDefinitions.Consumers;

        RowsHeadersWidth = Enumerable
            .Range(0, RowsDefinitions.TotalDepth())
            .Select(_ => DefaultHeaderWidth)
            .ToArray();

        ColumnsHeadersHeight = Enumerable
            .Range(0, ColumnsDefinitions.TotalDepth())
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
            Seq<T> definitions
        )
            where T : HierarchyDefinition
        {
            int index = 0;
            double space = offset;

            var frozenDefinitions = definitions.Where(x => x.Frozen);

            int cnt = 0;
            foreach (var frozen in frozenDefinitions)
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
                        (producer?.Guid ?? Guid.Empty, consumer?.Guid ?? Guid.Empty),
                        () => HierarchyDefinition.Resolve(producer!, consumer!)
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
                        ResultSet = resultSet
                    };

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

        Observable.Return(false).InvokeCommand(DrawGridCommand);
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
                HoveredElementId = s.Definition.Guid;
                if (s.Definition is ConsumerDefinition consumer && consumer.Count() == 1)
                {
                    HoveredColumn = ColumnsDefinitions.GetPosition(consumer);
                    HoveredRow = -1;
                }
                else if (s.Definition is ProducerDefinition producer && producer.Count() == 1)
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
