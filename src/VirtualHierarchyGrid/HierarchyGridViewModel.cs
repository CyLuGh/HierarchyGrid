using System.Security.Cryptography.X509Certificates;
using System.Runtime.CompilerServices;
using System.Globalization;
using DynamicData;
using HierarchyGrid.Definitions;
using MoreLinq;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace VirtualHierarchyGrid
{
    public partial class HierarchyGridViewModel : ReactiveObject, IActivatableViewModel
    {
        public ViewModelActivator Activator { get; }

        internal SourceCache<ProducerDefinition , int> ProducersCache { get; } = new SourceCache<ProducerDefinition , int>( x => x.Position );
        internal SourceCache<ConsumerDefinition , int> ConsumersCache { get; } = new SourceCache<ConsumerDefinition , int>( x => x.Position );

        internal SourceCache<ResultSet , (Guid, Guid)> ResultSets { get; }
            = new SourceCache<ResultSet , (Guid, Guid)>( x => (x.ProducerId, x.ConsumerId) );

        public bool HasData { [ObservableAsProperty] get; }
        [Reactive] public string StatusMessage { get; set; }

        private ReadOnlyObservableCollection<ResultSet> _selectedResultSets;
        public ReadOnlyObservableCollection<ResultSet> SelectedResultSets => _selectedResultSets;

        internal SourceCache<(int row, int col, ResultSet resultSet) , (int row, int col)> SelectedPositions { get; }
            = new SourceCache<(int row, int col, ResultSet resultSet) , (int row, int col)>( x => (x.row, x.col) );

        internal SourceList<(int pos, bool isRow)> Highlights { get; }
            = new SourceList<(int pos, bool isRow)>();

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

        [Reactive] public (ProducerDefinition, ConsumerDefinition, ResultSet)[] Selections { get; set; }

        public HierarchyDefinition[] ColumnsDefinitions => IsTransposed ?
            ProducersCache.Items.Cast<HierarchyDefinition>().ToArray() : ConsumersCache.Items.Cast<HierarchyDefinition>().ToArray();

        public HierarchyDefinition[] RowsDefinitions => IsTransposed ?
            ConsumersCache.Items.Cast<HierarchyDefinition>().ToArray() : ProducersCache.Items.Cast<HierarchyDefinition>().ToArray();

        public ReactiveCommand<(int, int, double, double, double) , PositionedCell[]> FindCellsToDrawCommand { get; private set; }
        public ReactiveCommand<(PositionedCell[], bool) , Unit> DrawCellsCommand { get; private set; }
        public ReactiveCommand<Unit , Unit> DrawGridCommand { get; private set; }
        public ReactiveCommand<Unit , Unit> BuildResultSetsCommand { get; private set; }

        public Interaction<Unit , Unit> DrawGridInteraction { get; }
            = new Interaction<Unit , Unit>( RxApp.MainThreadScheduler );

        public Interaction<(PositionedCell[], bool) , Unit> DrawCellsInteraction { get; }
            = new Interaction<(PositionedCell[], bool) , Unit>( RxApp.MainThreadScheduler );

        public bool IsValid => RowsHeadersWidth?.Any() == true && ColumnsHeadersHeight?.Any() == true;

        public ReactiveCommand<(int row, int column, ResultSet rs) , Unit> EditCommand { get; private set; }

        public Interaction<(int row, int column, ResultSet rs) , Unit> EditInteraction { get; }
            = new Interaction<(int row, int column, ResultSet rs) , Unit>( RxApp.MainThreadScheduler );

        public ReactiveCommand<Unit , Unit> EndEditionCommand { get; private set; }

        public Interaction<Unit , Unit> EndEditionInteraction { get; }
            = new Interaction<Unit , Unit>( RxApp.MainThreadScheduler );

        public ReactiveCommand<HierarchyGridHeaderViewModel , Unit> UpdateHighlightsCommand { get; private set; }

        public ReactiveCommand<bool , DataObject> CopyGridCommand { get; private set; }
        public ReactiveCommand<object , Unit> CopyToClipboardCommand { get; private set; }
        public ReactiveCommand<Unit , Unit> ExportCsvFileCommand { get; private set; }

        public HierarchyGridViewModel()
        {
            Activator = new ViewModelActivator();

            RegisterDefaultInteractions( this );
            InitializeCommands( this );

            TextAlignment = TextAlignment.Right;

            this.WhenActivated( disposables =>
            {
                StatusMessage = ResultSets.Items.Any() ? string.Empty : "No data";

                ResultSets.Connect()
                     .DisposeMany()
                     .Select( _ => ResultSets.Items.Any() )
                     .ObserveOn( RxApp.MainThreadScheduler )
                     .Do( b =>
                     {
                         if ( !b )
                             StatusMessage = "No data";
                     } )
                     .CombineLatest( BuildResultSetsCommand.IsExecuting.Select( x => !x ) )
                     .Select( bs => new[] { bs.First , bs.Second }.All( x => x ) )
                     .ToPropertyEx( this , x => x.HasData , scheduler: RxApp.MainThreadScheduler )
                     .DisposeWith( disposables );

                BuildResultSetsCommand.IsExecuting
                    .Subscribe( b =>
                    {
                        if ( b )
                            StatusMessage = "Building grid";
                    } )
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

                /* Redraw grid when scrolling or changing scale */
                this.WhenAnyValue( x => x.HorizontalOffset , x => x.VerticalOffset , x => x.Scale )
                    .DistinctUntilChanged()
                    .Select( _ => Unit.Default )
                    .Throttle( TimeSpan.FromMilliseconds( 15 ) )
                    .InvokeCommand( DrawGridCommand )
                    .DisposeWith( disposables );

                this.WhenAnyValue( x => x.HorizontalOffset , x => x.VerticalOffset , x => x.Width , x => x.Height ,
                        x => x.Scale )
                    .Throttle( TimeSpan.FromMilliseconds( 15 ) )
                    .DistinctUntilChanged()
                    .InvokeCommand( FindCellsToDrawCommand )
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

                /* Clear selection when changing selection mode */
                this.WhenAnyValue( x => x.EnableMultiSelection )
                    .SubscribeSafe( _ => SelectedPositions.Clear() )
                    .DisposeWith( disposables );

                this.WhenAnyValue( x => x.EnableCrosshair )
                    .Where( ec => ec == false )
                    .Select( _ => Unit.Default )
                    .InvokeCommand( DrawGridCommand )
                    .DisposeWith( disposables );

                /* Toggle edit mode on */
                EditCommand.SubscribeSafe( _ => IsEditing = true )
                    .DisposeWith( disposables );

                /* Toggle edit mode off */
                DrawGridCommand.SubscribeSafe( _ => IsEditing = false )
                    .DisposeWith( disposables );

                /* Clear textbox when exiting edition mode */
                this.WhenAnyValue( x => x.IsEditing )
                    .DistinctUntilChanged()
                    .Where( x => !x )
                    .Select( _ => Unit.Default )
                    .InvokeCommand( EndEditionCommand )
                    .DisposeWith( disposables );

                /* When clearing highlights, definitions should be resetted too */
                Highlights.Connect()
                    .SubscribeSafe( c =>
                    {
                        if ( c.Any( x => x.Reason == ListChangeReason.Clear ) )
                        {
                            ProducersCache.Items.FlatList().ForEach( x => x.IsHighlighted = false );
                            ConsumersCache.Items.FlatList().ForEach( x => x.IsHighlighted = false );

                            Observable.Return( Unit.Default )
                                .InvokeCommand( DrawGridCommand );
                        }
                    } )
                    .DisposeWith( disposables );

                CopyGridCommand
                    .ObserveOn( RxApp.MainThreadScheduler )
                    .InvokeCommand<object , Unit>( CopyToClipboardCommand )
                    .DisposeWith( disposables );

                FindCellsToDrawCommand
                    .Select( pCells => (pCells, false) )
                    .InvokeCommand( DrawCellsCommand )
                    .DisposeWith( disposables );

                /* Redraw grid when cache has been updated */
                this.BuildResultSetsCommand
                    .InvokeCommand( DrawGridCommand )
                    .DisposeWith( disposables );

                SelectedPositions.Connect()
                    .Transform( x => x.resultSet )
                    .ObserveOn( RxApp.MainThreadScheduler )
                    .Bind( out _selectedResultSets )
                    .DisposeMany()
                    .SubscribeSafe()
                    .DisposeWith( disposables );

                SelectedPositions.Connect()
                    .SubscribeSafe( x =>
                    {
                        var producers = ProducersCache.Items.FlatList().ToDictionary( x => x.Guid );
                        var consumers = ConsumersCache.Items.FlatList().ToDictionary( x => x.Guid );
                        Selections = x.Where( o => o.Reason == ChangeReason.Add )
                            .Select( o =>
                            {
                                var resultSet = o.Current.resultSet;
                                return (producers[resultSet.ProducerId], consumers[resultSet.ConsumerId], resultSet);
                            }
                        ).ToArray();
                    } )
                    .DisposeWith( disposables );
            } );
        }

        private static void RegisterDefaultInteractions( HierarchyGridViewModel @this )
        {
            @this.DrawGridInteraction.RegisterHandler( ctx => ctx.SetOutput( Unit.Default ) );
            @this.DrawCellsInteraction.RegisterHandler( ctx => ctx.SetOutput( Unit.Default ) );
            @this.EditInteraction.RegisterHandler( ctx => ctx.SetOutput( Unit.Default ) );
            @this.EndEditionInteraction.RegisterHandler( ctx => ctx.SetOutput( Unit.Default ) );
        }

        private static void InitializeCommands( HierarchyGridViewModel @this )
        {
            @this.DrawGridCommand = ReactiveCommand.CreateFromObservable( () => @this.DrawGridInteraction.Handle( Unit.Default ) );
            @this.DrawCellsCommand = ReactiveCommand.CreateFromObservable( ( (PositionedCell[], bool) t ) => @this.DrawCellsInteraction.Handle( t ) );
            @this.EditCommand = ReactiveCommand.CreateFromObservable<(int, int, ResultSet) , Unit>( t => @this.EditInteraction.Handle( t ) );
            @this.EndEditionCommand = ReactiveCommand.CreateFromObservable( () => @this.EndEditionInteraction.Handle( Unit.Default ) );

            @this.BuildResultSetsCommand = ReactiveCommand.CreateFromObservable( () => Observable.Start( () =>
              {
                  @this.ResultSets.Clear();

                  var consumers = @this.ConsumersCache.Items.FlatList().ToArray();
                  @this.ProducersCache.Items.FlatList().AsParallel().ForAll( producer =>
                     consumers.ForEach( consumer => @this.ResultSets.AddOrUpdate( HierarchyDefinition.Resolve( producer , consumer ) ) )
                  );
              } ) );

            @this.UpdateHighlightsCommand = ReactiveCommand.CreateFromObservable<HierarchyGridHeaderViewModel , Unit>( vModel =>
                    Observable.Start( () =>
                     {
                         var pos = vModel.RowIndex ?? vModel.ColumnIndex ?? -1;
                         var isRow = vModel.RowIndex != null;

                         if ( vModel.IsHighlighted )
                             @this.Highlights.Add( (pos, isRow) );
                         else
                             @this.Highlights.Remove( (pos, isRow) );
                     } ) );

            @this.CopyGridCommand =
                ReactiveCommand.CreateFromObservable<bool , DataObject>( b =>
                      Observable.Start( () =>
                       {
                           return @this.CopyToClipboard( string.Empty , b , @this.RowsDefinitions.Leaves() , @this.ColumnsDefinitions.Leaves() );
                       } ) );

            @this.CopyToClipboardCommand =
                ReactiveCommand.Create<object , Unit>( data =>
                  {
                      Clipboard.SetDataObject( data );
                      return Unit.Default;
                  } );

            @this.ExportCsvFileCommand =
                ReactiveCommand.CreateFromObservable( () =>
                     Observable.Start( () =>
                      {
                          var file = Path.Combine( Path.GetTempPath() , $"{Guid.NewGuid()}.csv" );
                          var content = @this.ExportCsv( ";" );
                          if ( !string.IsNullOrWhiteSpace( content ) )
                          {
                              using ( var fs = new FileStream( file , FileMode.Create ) )
                              using ( var sw = new StreamWriter( fs ) )
                              {
                                  sw.Write( content );
                                  sw.Flush();
                              }

                              Process.Start( "notepad" , file );
                          }
                      } ) );

            @this.FindCellsToDrawCommand =
                ReactiveCommand.CreateFromObservable( ( (int, int, double, double, double) t ) =>
                    Observable.Start( () =>
                    {
                        var (hIndex, vIndex, width, height, scale) = t;
                        var cells = @this.ChooseDrawnCells( hIndex , vIndex , width , height , scale );
                        return cells;
                    } ) );
        }

        public void Set( HierarchyDefinitions hierarchyDefinitions )
        {
            Clear();

            ProducersCache.AddOrUpdate( hierarchyDefinitions.Producers );
            ConsumersCache.AddOrUpdate( hierarchyDefinitions.Consumers );

            RowsHeadersWidth = Enumerable.Range( 0 , RowsDefinitions.TotalDepth( true ) )
                .Select( _ => DEFAULT_HEADER_WIDTH )
                .ToArray();

            ColumnsHeadersHeight = Enumerable.Range( 0 , ColumnsDefinitions.TotalDepth( true ) )
                .Select( _ => DEFAULT_HEADER_HEIGHT )
                .ToArray();

            Enumerable.Range( 0 , ColumnsDefinitions.TotalCount( true ) )
                .ForEach( x => ColumnsWidths.Add( x , DEFAULT_COLUMN_WIDTH ) );

            Enumerable.Range( 0 , RowsDefinitions.TotalCount( true ) )
                .ForEach( x => RowsHeights.Add( x , DEFAULT_ROW_HEIGHT ) );

            Observable.Return( Unit.Default )
                .InvokeCommand( BuildResultSetsCommand );
        }

        public void Clear()
        {
            ProducersCache.Clear();
            ConsumersCache.Clear();

            SelectedPositions.Clear();

            ResultSets.Clear();

            ColumnsWidths.Clear();
            RowsHeights.Clear();

            HorizontalOffset = 0;
            VerticalOffset = 0;

            HoveredRow = -1;
            HoveredColumn = -1;
        }

        private PositionedCell[] ChooseDrawnCells( int hIndex , int vIndex , double width , double height , double scale )
        {
            IEnumerable<(double coord, double size, int index, T definition)> FindCells<T>( int startIndex , double offset , double maxSpace ,
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

            var rowDefinitions = RowsDefinitions.Leaves().ToArray();
            var colDefinitions = ColumnsDefinitions.Leaves().ToArray();
            // Determine which cells can be drawn.
            var firstColumn = hIndex;
            var firstRow = vIndex;

            var availableWidth = width / scale;
            var availableHeight = height / scale;

            var columns = FindCells( firstColumn , RowsHeadersWidth?.Sum() ?? 0d , availableWidth , ColumnsWidths , colDefinitions ).ToArray();
            var rows = FindCells( firstRow , ColumnsHeadersHeight?.Sum() ?? 0d , availableHeight , RowsHeights , rowDefinitions ).ToArray();

            return columns.SelectMany( c => rows.Select( r => new PositionedCell
            {
                Left = c.coord ,
                Width = c.size ,
                Top = r.coord ,
                Height = r.size ,
                HorizontalPosition = c.index ,
                VerticalPosition = r.index ,
                ConsumerDefinition = ( IsTransposed ? r.definition : c.definition ) as ConsumerDefinition ,
                ProducerDefinition = ( IsTransposed ? c.definition : r.definition ) as ProducerDefinition
            } ) ).ToArray();
        }

        public string ExportCsv( string separator )
        {
            var sb = new StringBuilder();

            var rowsFlat = RowsDefinitions.FlatList().ToArray();
            var colsFlat = ColumnsDefinitions.FlatList().ToArray();

            foreach ( var level in colsFlat.Select( o => o.Level ).Distinct().OrderBy( o => o ) )
            {
                foreach ( var _ in rowsFlat.Select( o => o.Level ).Distinct().OrderBy( o => o ) )
                    sb.Append( separator );

                foreach ( var colDef in colsFlat.Where( x => x.Level == level ) )
                    if ( (string) colDef.Content != "Dummy" )
                    {
                        sb.Append( colDef.Content );
                        sb.Append( separator );
                        for ( int i = 1 ; i < colDef.Span ; ++i )
                            sb.Append( separator );
                    }
                    else
                        sb.Append( separator );
                sb.Append( Environment.NewLine );
            }

            int currentLevel = -1;
            int maxLevel = rowsFlat.Max( o => o.Depth() );
            var colLeaves = ColumnsDefinitions.Leaves().ToList();
            foreach ( var rowDef in RowsDefinitions.FlatList( false ) )
            {
                if ( rowDef.Level <= currentLevel )
                {
                    sb.Append( Environment.NewLine );
                    // Empty cells for hierarchy alignment
                    for ( int i = 0 ; i < rowDef.Level ; i++ )
                        sb.Append( separator );
                }

                currentLevel = rowDef.Level;

                sb.Append( rowDef.Content );
                sb.Append( separator );

                if ( !rowDef.HasChild || !rowDef.IsExpanded )
                {
                    for ( int i = 1 ; i < maxLevel - currentLevel ; ++i )
                        sb.Append( separator );
                }

                if ( !rowDef.HasChild || !rowDef.IsExpanded )
                {
                    // Add data
                    foreach ( var colDef in colLeaves )
                    {
                        var idd = Identify( rowDef , colDef );

                        var str = idd.Some( key =>
                             {
                                 var lkp = ResultSets.Lookup( key );
                                 return lkp.HasValue ? lkp.Value.Result : string.Empty;
                             } )
                           .None( () => string.Empty );
                        sb.Append( str );
                        sb.Append( separator );
                    }
                }
            }

            return sb.ToString();
        }

        public HierarchyGridState GridState
        {
            get
            {
                try
                {
                    return new HierarchyGridState
                    {
                        VerticalOffset = VerticalOffset ,
                        HorizontalOffset = HorizontalOffset ,
                        RowToggles = RowsDefinitions.FlatList().Select( o => o.Path.All( x => x.IsExpanded ) ).ToArray() ,
                        ColumnToggles = ColumnsDefinitions.FlatList().Select( o => o.Path.All( x => x.IsExpanded ) ).ToArray()
                    };
                }
                catch ( Exception )
                {
                    return default;
                }
            }

            set
            {
                if ( value.Equals( default( HierarchyGridState ) ) )
                    return;

                try
                {
                    var rowsFlat = RowsDefinitions.FlatList().ToArray();
                    if ( rowsFlat.Length == value.RowToggles.Length )
                        Parallel.For( 0 , value.RowToggles.Length , i => rowsFlat[i].IsExpanded = value.RowToggles[i] );
                    else
                        rowsFlat.AsParallel().ForAll( x => x.IsExpanded = true );

                    var columnsFlat = ColumnsDefinitions.FlatList().ToArray();
                    if ( columnsFlat.Length == value.ColumnToggles.Length )
                        Parallel.For( 0 , value.ColumnToggles.Length , i => columnsFlat[i].IsExpanded = value.ColumnToggles[i] );
                    else
                        columnsFlat.AsParallel().ForAll( x => x.IsExpanded = true );

                    VerticalOffset = value.VerticalOffset;
                    HorizontalOffset = value.HorizontalOffset;
                }
                catch ( Exception )
                {
                    VerticalOffset = 0;
                    HorizontalOffset = 0;
                }

                Observable.Return( Unit.Default )
                    .InvokeCommand( DrawGridCommand );
            }
        }
    }
}