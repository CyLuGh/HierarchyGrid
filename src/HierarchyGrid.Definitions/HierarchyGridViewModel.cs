using DynamicData;
using LanguageExt;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Splat;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using Unit = System.Reactive.Unit;

namespace HierarchyGrid.Definitions
{
    public partial class HierarchyGridViewModel : ReactiveObject, IActivatableViewModel
    {
        public ViewModelActivator Activator { get; }
        public bool IsValid => RowsHeadersWidth?.Any() == true && ColumnsHeadersHeight?.Any() == true;

        internal SourceCache<ProducerDefinition , int> ProducersCache { get; } = new SourceCache<ProducerDefinition , int>( x => x.Position );
        internal SourceCache<ConsumerDefinition , int> ConsumersCache { get; } = new SourceCache<ConsumerDefinition , int>( x => x.Position );

        public bool HasData { [ObservableAsProperty] get; }
        [Reactive] public string StatusMessage { get; set; }

        internal ConcurrentDictionary<(Guid, Guid) , ResultSet> ResultSets { get; }
            = new ConcurrentDictionary<(Guid, Guid) , ResultSet>();

        internal ObservableCollection<PositionedCell> SelectedCells { get; } = new();

        public ReadOnlyObservableCollection<PositionedCell> Selections => new( SelectedCells );

        public ConcurrentBag<(ElementCoordinates Coord, HierarchyDefinition Definition)> HeadersCoordinates { get; } = new();
        public ConcurrentBag<(ElementCoordinates Coord, PositionedCell Cell)> CellsCoordinates { get; } = new();
        public ConcurrentBag<(ElementCoordinates Coord, Guid Guid, Action Action)> GlobalHeadersCoordinates { get; } = new();

        [Reactive] public int HorizontalOffset { get; set; }
        [Reactive] public int VerticalOffset { get; set; }

        [Reactive] public double Scale { get; set; } = 1d;

        [Reactive] public double Width { get; set; } = double.NaN;
        [Reactive] public double Height { get; set; } = double.NaN;

        [Reactive] public int MaxHorizontalOffset { get; set; }
        [Reactive] public int MaxVerticalOffset { get; set; }

        [Reactive] public bool IsTransposed { get; set; }

        [Reactive] public bool EnableCrosshair { get; set; }
        [Reactive] public int HoveredColumn { get; set; } = -1;
        [Reactive] public int HoveredRow { get; set; } = -1;

        [Reactive] public bool EnableMultiSelection { get; set; }
        [Reactive] public bool IsEditing { get; set; }

        [Reactive] public ITheme Theme { get; set; } = HierarchyGridTheme.Default;

        private readonly Subject<Option<PositionedCell>> _hoveredCell = new();

        [Reactive] public Guid HoveredElementId { get; private set; }

        public HierarchyDefinition[] ColumnsDefinitions => IsTransposed ?
            ProducersCache.Items.Cast<HierarchyDefinition>().ToArray() : ConsumersCache.Items.Cast<HierarchyDefinition>().ToArray();

        public HierarchyDefinition[] RowsDefinitions => IsTransposed ?
            ConsumersCache.Items.Cast<HierarchyDefinition>().ToArray() : ProducersCache.Items.Cast<HierarchyDefinition>().ToArray();

        public HierarchyGridState GridState
        {
            get { return new HierarchyGridState( this ); }
            set
            {
                if ( value.Equals( default ) )
                    return;

                try
                {
                    var rowsFlat = RowsDefinitions.FlatList().ToArray();
                    if ( rowsFlat.Length == value.RowToggles.Length )
                        Parallel.For( 0 , value.RowToggles.Length , i => rowsFlat[i].IsExpanded = value.RowToggles[i] );
                    else
                        rowsFlat.AsParallel().ForAll( x => { x.IsExpanded = true; } );

                    var columnsFlat = ColumnsDefinitions.FlatList().ToArray();
                    if ( columnsFlat.Length == value.ColumnToggles.Length )
                        Parallel.For( 0 , value.ColumnToggles.Length , i => columnsFlat[i].IsExpanded = value.ColumnToggles[i] );
                    else
                        columnsFlat.AsParallel().ForAll( x => { x.IsExpanded = true; } );

                    VerticalOffset = value.VerticalOffset;
                    HorizontalOffset = value.HorizontalOffset;
                }
                catch ( Exception )
                {
                    VerticalOffset = 0;
                    HorizontalOffset = 0;
                }

                Observable.Return( false )
                    .InvokeCommand( DrawGridCommand );
            }
        }

        public ReactiveCommand<bool , Unit> DrawGridCommand { get; private set; }
        public Interaction<Unit , Unit> DrawGridInteraction { get; } = new( RxApp.MainThreadScheduler );
        public ReactiveCommand<bool , Unit> EndEditionCommand { get; private set; }
        public Interaction<Unit , Unit> EndEditionInteraction { get; } = new( RxApp.MainThreadScheduler );
        public Interaction<PositionedCell , Unit> StartEditionInteraction { get; } = new( RxApp.MainThreadScheduler );
        public CombinedReactiveCommand<bool , Unit> EndAndDrawCommand { get; private set; }
        public ReactiveCommand<Option<PositionedCell> , Unit> HandleTooltipCommand { get; private set; }
        public Interaction<Unit , Unit> CloseTooltipInteraction { get; } = new( RxApp.MainThreadScheduler );
        public Interaction<PositionedCell , Unit> ShowTooltipInteraction { get; } = new( RxApp.MainThreadScheduler );

        public ReactiveCommand<CopyMode , Unit> CopyToClipboardCommand { get; private set; }
        public Interaction<string , Unit> FillClipboardInteraction { get; } = new( RxApp.MainThreadScheduler );

        public ReactiveCommand<bool , Unit> ToggleStatesCommand { get; private set; }

        public ReactiveCommand<Unit , Unit> ToggleCrosshairCommand { get; private set; }
        public ReactiveCommand<Unit , Unit> ClearHighlightsCommand { get; private set; }

        public Queue<IDisposable> ResizeObservables { get; } = new();

        public HierarchyGridViewModel()
        {
            Activator = new ViewModelActivator();

            RegisterDefaultInteractions( this );
            InitializeCommands( this );

            this.WhenActivated( disposables =>
            {
                ProducersCache.Connect().DisposeMany().Select( _ => ProducersCache.Items.Any() )
                        .CombineLatest( ConsumersCache.Connect().DisposeMany().Select( _ => ConsumersCache.Items.Any() ) )
                        .Select( t => t.First || t.Second )
                        .ObserveOn( RxApp.MainThreadScheduler )
                        .Do( b =>
                        {
                            if ( !b )
                                StatusMessage = "No data";
                        } )
                        .ToPropertyEx( this , x => x.HasData , scheduler: RxApp.MainThreadScheduler )
                        .DisposeWith( disposables );

                /* Don't allow scale < 0.75 */
                this.WhenAnyValue( x => x.Scale )
                    .Where( x => x < 0.75 )
                    .SubscribeSafe( _ => Scale = 0.75 )
                    .DisposeWith( disposables );

                /* Don't allow scale > 1 */
                this.WhenAnyValue( x => x.Scale )
                    .Where( x => x > 1 )
                    .SubscribeSafe( _ => Scale = 1 )
                    .DisposeWith( disposables );

                /* Don't allow horizontal offset to go above max offset */
                this.WhenAnyValue( x => x.HorizontalOffset )
                    .CombineLatest( this.WhenAnyValue( x => x.MaxHorizontalOffset ) ,
                    ( ho , m ) => ho > m && m > 0 )
                    .Throttle( TimeSpan.FromMilliseconds( 5 ) )
                    .Where( x => x )
                    .ObserveOn( RxApp.MainThreadScheduler )
                    .SubscribeSafe( _ => HorizontalOffset = MaxHorizontalOffset )
                    .DisposeWith( disposables );

                /* Don't allow vertical offset to go above max offset */
                this.WhenAnyValue( x => x.VerticalOffset )
                    .CombineLatest( this.WhenAnyValue( x => x.MaxVerticalOffset ) ,
                    ( vo , m ) => vo > m && m > 0 )
                    .Throttle( TimeSpan.FromMilliseconds( 5 ) )
                    .Where( x => x )
                    .ObserveOn( RxApp.MainThreadScheduler )
                    .SubscribeSafe( _ => VerticalOffset = MaxVerticalOffset )
                    .DisposeWith( disposables );

                /* Don't allow negative horizontal offset */
                this.WhenAnyValue( x => x.HorizontalOffset )
                    .Where( x => x < 0 )
                    .SubscribeSafe( _ => HorizontalOffset = 0 )
                    .DisposeWith( disposables );

                /* Don't allow negative vertical offset */
                this.WhenAnyValue( x => x.VerticalOffset )
                    .Where( x => x < 0 )
                    .SubscribeSafe( _ => VerticalOffset = 0 )
                    .DisposeWith( disposables );

                /* Redraw grid when scrolling or changing scale */
                this.WhenAnyValue( x => x.HorizontalOffset , x => x.VerticalOffset , x => x.Scale , x => x.Width , x => x.Height )
                    .Throttle( TimeSpan.FromMilliseconds( 5 ) )
                    .DistinctUntilChanged()
                    .Select( _ => false )
                    .InvokeCommand( EndAndDrawCommand )
                    .DisposeWith( disposables );

                this.WhenAnyValue( x => x.HoveredColumn , x => x.HoveredRow , x => x.HoveredElementId )
                    .DistinctUntilChanged()
                    .Select( _ => false )
                    .InvokeCommand( DrawGridCommand )
                    .DisposeWith( disposables );

                this.WhenAnyValue( x => x.Theme )
                    .Select( _ => false )
                    .InvokeCommand( DrawGridCommand )
                    .DisposeWith( disposables );

                _hoveredCell.Throttle( TimeSpan.FromMilliseconds( 600 ) )
                    .DistinctUntilChanged()
                    .InvokeCommand( HandleTooltipCommand )
                    .DisposeWith( disposables );

                ToggleCrosshairCommand
                    .Select( _ => false )
                    .InvokeCommand( DrawGridCommand )
                    .DisposeWith( disposables );

                ClearHighlightsCommand
                    .Select( _ => false )
                    .InvokeCommand( DrawGridCommand )
                    .DisposeWith( disposables );

                ToggleStatesCommand
                    .Select( _ => false )
                    .InvokeCommand( DrawGridCommand )
                    .DisposeWith( disposables );
            } );
        }

        private static void RegisterDefaultInteractions( HierarchyGridViewModel @this )
        {
            @this.DrawGridInteraction.RegisterHandler( ctx => ctx.SetOutput( Unit.Default ) );
            @this.StartEditionInteraction.RegisterHandler( ctx => ctx.SetOutput( Unit.Default ) );
            @this.EndEditionInteraction.RegisterHandler( ctx => ctx.SetOutput( Unit.Default ) );
            @this.ShowTooltipInteraction.RegisterHandler( ctx => ctx.SetOutput( Unit.Default ) );
            @this.CloseTooltipInteraction.RegisterHandler( ctx => ctx.SetOutput( Unit.Default ) );
            @this.FillClipboardInteraction.RegisterHandler( ctx => ctx.SetOutput( Unit.Default ) );
        }

        private static void InitializeCommands( HierarchyGridViewModel @this )
        {
            @this.DrawGridCommand = ReactiveCommand
                .CreateFromTask<bool , Unit>( async invalidate =>
                {
                    if ( invalidate )
                        @this.ResultSets.Clear();

                    await @this.DrawGridInteraction.Handle( Unit.Default );
                    return Unit.Default;
                } );

            @this.DrawGridCommand.ThrownExceptions
                .SubscribeSafe( e => @this.Log().Error( e ) );

            @this.EndEditionCommand = ReactiveCommand
                .CreateFromObservable( ( bool _ ) => @this.EndEditionInteraction.Handle( Unit.Default ) );
            @this.EndEditionCommand.ThrownExceptions
                .SubscribeSafe( e => @this.Log().Error( e ) );

            @this.EndAndDrawCommand = ReactiveCommand.CreateCombined( new[] { @this.EndEditionCommand , @this.DrawGridCommand } );

            @this.HandleTooltipCommand = ReactiveCommand
                .CreateFromTask( ( Option<PositionedCell> o ) =>
                    o.MatchAsync( async cell => await @this.ShowTooltipInteraction.Handle( cell ) ,
                        async () => await @this.CloseTooltipInteraction.Handle( Unit.Default ) ) );
            @this.DrawGridCommand.ThrownExceptions
                .SubscribeSafe( e => @this.Log().Error( e ) );

            @this.ToggleCrosshairCommand = ReactiveCommand.Create( () =>
            {
                @this.EnableCrosshair = !@this.EnableCrosshair;
                return Unit.Default;
            } );
            @this.ToggleCrosshairCommand.ThrownExceptions
                .SubscribeSafe( e => @this.Log().Error( e ) );

            @this.ClearHighlightsCommand = ReactiveCommand.CreateFromObservable( () =>
                Observable.Start( () => @this.ClearHighlights() ) );
            @this.ClearHighlightsCommand.ThrownExceptions
                .SubscribeSafe( e => @this.Log().Error( e ) );

            @this.CopyToClipboardCommand = ReactiveCommand.CreateFromTask( async ( CopyMode mode ) =>
                {
                    var content = @this.CreateClipboardContent( mode );
                    await @this.FillClipboardInteraction.Handle( content );
                }
            );
            @this.CopyToClipboardCommand.ThrownExceptions
                .SubscribeSafe( e => @this.Log().Error( e ) );

            @this.ToggleStatesCommand = ReactiveCommand.CreateFromObservable( ( bool expanded ) =>
                Observable.Start( () =>
                {
                    if ( expanded )
                    {
                        @this.ColumnsDefinitions.ExpandAll();
                        @this.RowsDefinitions.ExpandAll();
                    }
                    else
                    {
                        @this.ColumnsDefinitions.FoldAll();
                        @this.RowsDefinitions.FoldAll();
                    }
                } ) );
        }

        public void Set( HierarchyDefinitions hierarchyDefinitions , bool preserveSizes = false )
        {
            Clear( preserveSizes );

            ProducersCache.AddOrUpdate( hierarchyDefinitions.Producers );
            ConsumersCache.AddOrUpdate( hierarchyDefinitions.Consumers );

            RowsHeadersWidth = Enumerable.Range( 0 , RowsDefinitions.TotalDepth( true ) )
                .Select( _ => DEFAULT_HEADER_WIDTH )
                .ToArray();

            ColumnsHeadersHeight = Enumerable.Range( 0 , ColumnsDefinitions.TotalDepth( true ) )
                .Select( _ => DEFAULT_HEADER_HEIGHT )
                .ToArray();

            var columnsCount = ColumnsDefinitions.TotalCount( true );
            if ( !preserveSizes || columnsCount != ColumnsWidths.Count )
            {
                ColumnsWidths.Clear();
                for ( int x = 0 ; x <= columnsCount ; x++ )
                    ColumnsWidths.Add( x , DEFAULT_COLUMN_WIDTH );
            }

            var rowsCount = RowsDefinitions.TotalCount( true );
            if ( !preserveSizes || rowsCount != RowsHeights.Count )
            {
                RowsHeights.Clear();
                for ( int x = 0 ; x <= rowsCount ; x++ )
                    RowsHeights.Add( x , DEFAULT_ROW_HEIGHT );
            }

            Observable.Return( true )
                .InvokeCommand( DrawGridCommand );
        }

        public void Clear( bool preserveSizes = false )
        {
            ProducersCache.Clear();
            ConsumersCache.Clear();

            //SelectedPositions.Clear();

            if ( !preserveSizes )
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
        }

        public void ClearCoordinates()
        {
            HeadersCoordinates.Clear();
            CellsCoordinates.Clear();
            GlobalHeadersCoordinates.Clear();
        }

        public void ClearHighlights()
        {
            foreach ( var hdef in ColumnsDefinitions.FlatList().Concat( RowsDefinitions.FlatList() )
                                    .Where( x => x.IsHighlighted ) )
            {
                hdef.IsHighlighted = false;
            }
        }

        public PositionedCell[] DrawnCells( double width , double height , bool invalidate )
            => DrawnCells( HorizontalOffset , VerticalOffset , width , height , Scale , invalidate );

        private PositionedCell[] DrawnCells( int hIndex , int vIndex , double width , double height , double scale , bool invalidate )
        {
            static IEnumerable<(double coord, double size, int index, T definition)> FindCells<T>( int startIndex , double offset , double maxSpace ,
                Dictionary<int , double> sizes , T[] definitions ) where T : HierarchyDefinition
            {
                int index = 0;
                double space = offset;

                var frozens = definitions.Where( x => x.Frozen ).ToArray();

                int cnt = 0;
                foreach ( var frozen in frozens )
                {
                    var size = sizes[frozen.Position];
                    yield return (space, size, cnt++, frozen);
                    index++;
                    space += size;
                }

                while ( space < maxSpace && startIndex + index < definitions.Length )
                {
                    var size = sizes[startIndex + index];
                    yield return (space, size, startIndex + index, definitions[startIndex + index]);
                    space += size;
                    index++;
                }
            }

            if ( invalidate )
                ResultSets.Clear();

            var rowDefinitions = RowsDefinitions.Leaves().ToArray();
            var colDefinitions = ColumnsDefinitions.Leaves().ToArray();
            // Determine which cells can be drawn.
            var firstColumn = hIndex;
            var firstRow = vIndex;

            var availableWidth = width / scale;
            var availableHeight = height / scale;

            var columns = FindCells( firstColumn , RowsHeadersWidth?.Sum() ?? 0d , availableWidth , ColumnsWidths , colDefinitions ).ToArray();
            var rows = FindCells( firstRow , ColumnsHeadersHeight?.Sum() ?? 0d , availableHeight , RowsHeights , rowDefinitions ).ToArray();

            var pCells = columns.AsParallel().SelectMany( c => rows.Select( r =>
            {
                var pCell = new PositionedCell
                {
                    Left = c.coord ,
                    Width = c.size ,
                    Top = r.coord ,
                    Height = r.size ,
                    HorizontalPosition = c.index ,
                    VerticalPosition = r.index ,
                    ConsumerDefinition = ( IsTransposed ? r.definition : c.definition ) as ConsumerDefinition ,
                    ProducerDefinition = ( IsTransposed ? c.definition : r.definition ) as ProducerDefinition
                };

                if ( !ResultSets.TryGetValue( (pCell.ProducerDefinition.Guid, pCell.ConsumerDefinition.Guid) , out var rSet ) )
                {
                    rSet = HierarchyDefinition.Resolve( pCell.ProducerDefinition , pCell.ConsumerDefinition );
                }
                pCell.ResultSet = rSet;

                return pCell;
            } ) ).ToArray();

            ResultSets.Clear();
            pCells
                .AsParallel()
                .ForAll( pCell => { var _ = ResultSets.TryAdd( (pCell.ProducerDefinition.Guid, pCell.ConsumerDefinition.Guid) , pCell.ResultSet ); } );

            return pCells;
        }

        public Option<PositionedCell> FindHoveredCell()
        {
            if ( HoveredColumn == -1 || HoveredRow == -1 )
                return Option<PositionedCell>.None;

            return CellsCoordinates
                .Select( t => Option<PositionedCell>.Some( t.Cell ) )
                .FirstOrDefault( o => o.Match( c => c.VerticalPosition == HoveredRow && c.HorizontalPosition == HoveredColumn ,
                    () => false ) , Option<PositionedCell>.None );
        }

        internal async void HandleMouseDown( double x , double y , bool isRightClick = false )
        {
            if ( !IsValid )
                return;

            await EndEditionInteraction.Handle( Unit.Default );

            // Find corresponding element
            if ( !isRightClick && x <= RowsHeadersWidth.Sum() && y <= ColumnsHeadersHeight.Sum() )
            {
                /* Global header */
                FindGlobalAction( x , y )
                    .IfSome( a =>
                    {
                        a();
                        Observable.Return( false )
                            .InvokeCommand( DrawGridCommand );
                    } );
            }
            else
            {
                var element = FindCoordinates( x , y );
                element.Match( c =>
                {
                    c.Match( cell => CellClick( cell ) , () => { } );
                } ,
                d =>
                {
                    if ( !isRightClick )
                        d.Match( hdef => HeaderClick( hdef ) , () => { } );
                } );
            }
        }

        private void CellClick( PositionedCell cell )
        {
            if ( !EnableMultiSelection )
                SelectedCells.Clear();

            SelectedCells.Add( cell );

            Observable.Return( false )
                .InvokeCommand( DrawGridCommand );
        }

        private void HeaderClick( HierarchyDefinition hdef )
        {
            if ( hdef.HasChild && hdef.CanToggle )
                hdef.IsExpanded = !hdef.IsExpanded;
            else
                hdef.IsHighlighted = !hdef.IsHighlighted;

            Observable.Return( false )
                .InvokeCommand( DrawGridCommand );
        }

        internal void HandleDoubleClick( double x , double y )
        {
            FindCoordinates( x , y )
                .IfRight( o
                    => o.IfSome( async cell
                        => await StartEditionInteraction.Handle( cell ) ) );
        }

        internal void HandleMouseLeft()
        {
            _hoveredCell.OnNext( Option<PositionedCell>.None );
            HoveredElementId = Guid.Empty;
            ClearCrosshair();
        }

        internal void HandleMouseOver( double x , double y )
        {
            if ( RowsHeadersWidth?.Any() != true || ColumnsHeadersHeight?.Any() != true )
            {
                _hoveredCell.OnNext( Option<PositionedCell>.None );
                HoveredElementId = Guid.Empty;
                return;
            }

            var element = FindCoordinates( x , y );
            element.Match( cell =>
            {
                _hoveredCell.OnNext( cell );
                HoveredElementId = Guid.Empty;

                cell.Match( s =>
                {
                    HoveredColumn = s.HorizontalPosition;
                    HoveredRow = s.VerticalPosition;
                } , () =>
                {
                    HoveredColumn = -1;
                    HoveredRow = -1;
                } );
            } ,
            hdef =>
            {
                _hoveredCell.OnNext( Option<PositionedCell>.None );
                hdef.Match( s =>
                {
                    HoveredElementId = s.Guid;
                    if ( s is ConsumerDefinition consumer && consumer.Count() == 1 )
                    {
                        HoveredColumn = ColumnsDefinitions.GetPosition( consumer );
                        HoveredRow = -1;
                    }
                    else if ( s is ProducerDefinition producer && producer.Count() == 1 )
                    {
                        HoveredRow = RowsDefinitions.GetPosition( producer );
                        HoveredColumn = -1;
                    }
                    else
                    {
                        HoveredColumn = -1;
                        HoveredRow = -1;
                    }
                } , () =>
                {
                    HoveredElementId = GlobalHeadersCoordinates
                        .Find( t => t.Coord.Contains( x , y ) )
                        .Some( t => t.Guid )
                        .None( () => Guid.Empty );

                    HoveredColumn = -1;
                    HoveredRow = -1;
                } );
            } );
        }

        public Option<Action> FindGlobalAction( double x , double y )
            => GlobalHeadersCoordinates
                .Find( t => t.Coord.Contains( x , y ) )
                .Match( s => s.Action , () => Option<Action>.None );

        public Either<Option<HierarchyDefinition> , Option<PositionedCell>> FindCoordinates( double x , double y )
        {
            if ( x <= RowsHeadersWidth.Sum() || y <= ColumnsHeadersHeight.Sum() )
            {
                return HeadersCoordinates
                    .AsParallel()
                    .Find( t => t.Coord.Contains( x , y ) )
                    .Match( s => s.Definition , () => Option<HierarchyDefinition>.None );
            }
            else
            {
                return CellsCoordinates
                    .AsParallel()
                    .Find( t => t.Coord.Contains( x , y ) )
                    .Match( s => s.Cell , () => Option<PositionedCell>.None );
            }
        }
    }
}