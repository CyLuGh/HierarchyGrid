using DynamicData;
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
using System.Text;
using System.Threading.Tasks;

namespace HierarchyGrid.Definitions
{
    public partial class HierarchyGridViewModel : ReactiveObject, IActivatableViewModel
    {
        public ViewModelActivator Activator { get; }

        internal SourceCache<ProducerDefinition , int> ProducersCache { get; } = new SourceCache<ProducerDefinition , int>( x => x.Position );
        internal SourceCache<ConsumerDefinition , int> ConsumersCache { get; } = new SourceCache<ConsumerDefinition , int>( x => x.Position );

        public bool HasData { [ObservableAsProperty] get; }
        [Reactive] public string StatusMessage { get; set; }

        internal ConcurrentDictionary<(Guid, Guid) , ResultSet> ResultSets { get; }
            = new ConcurrentDictionary<(Guid, Guid) , ResultSet>();

        //private ReadOnlyObservableCollection<ResultSet> _selectedResultSets;
        //public ReadOnlyObservableCollection<ResultSet> SelectedResultSets => _selectedResultSets;

        //internal SourceCache<(int row, int col, ResultSet resultSet) , (int row, int col)> SelectedPositions { get; }
        //    = new SourceCache<(int row, int col, ResultSet resultSet) , (int row, int col)>( x => (x.row, x.col) );

        //internal SourceList<(int pos, bool isRow)> Highlights { get; }
        //    = new SourceList<(int pos, bool isRow)>();

        public ConcurrentBag<(ElementCoordinates, HierarchyDefinition)> HeadersCoordinates { get; } = new();
        public ConcurrentBag<(ElementCoordinates, PositionedCell)> CellsCoordinates { get; } = new();

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

        public ReactiveCommand<Unit , Unit> DrawGridCommand { get; private set; }
        public Interaction<Unit , Unit> DrawGridInteraction { get; } = new( RxApp.MainThreadScheduler );

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

                /* Redraw grid when scrolling or changing scale */
                this.WhenAnyValue( x => x.HorizontalOffset , x => x.VerticalOffset , x => x.Scale , x => x.Width , x => x.Height )
                    .Throttle( TimeSpan.FromMilliseconds( 5 ) )
                    .DistinctUntilChanged()
                    .Select( _ => Unit.Default )
                    .InvokeCommand( DrawGridCommand )
                    .DisposeWith( disposables );
            } );
        }

        private static void RegisterDefaultInteractions( HierarchyGridViewModel @this )
        {
            @this.DrawGridInteraction.RegisterHandler( ctx => ctx.SetOutput( Unit.Default ) );
            //@this.DrawCellsInteraction.RegisterHandler( ctx => ctx.SetOutput( Unit.Default ) );
            //@this.EditInteraction.RegisterHandler( ctx => ctx.SetOutput( Unit.Default ) );
            //@this.EndEditionInteraction.RegisterHandler( ctx => ctx.SetOutput( Unit.Default ) );
        }

        private static void InitializeCommands( HierarchyGridViewModel @this )
        {
            @this.DrawGridCommand = ReactiveCommand.CreateFromObservable( () => @this.DrawGridInteraction.Handle( Unit.Default ) );
            @this.DrawGridCommand.ThrownExceptions
                .SubscribeSafe( e => @this.Log().Error( e ) );
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

            Observable.Return( Unit.Default )
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
    }
}