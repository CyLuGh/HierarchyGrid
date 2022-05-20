using HierarchyGrid.Definitions;
using ReactiveUI;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using MoreLinq;
using DynamicData;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Input;
using Splat;

using HGVM = HierarchyGrid.Definitions.HierarchyGridViewModel;

using ReactiveMarbles.ObservableEvents;
using HierarchyGrid.Skia;

namespace VirtualHierarchyGrid
{
    public partial class HierarchyGrid
    {
        private void DrawGrid( Size size )
        {
            //HierarchyGridDrawer.Draw( hierarchyGridViewModel , this.)
        }

        private void DrawCells( PositionedCell[] pCells , bool invalidate )
        { }

        //// Keep a cache of cells to be reused when redrawing -- it costs less to reuse than create
        //private readonly List<HierarchyGridCell> _cells = new List<HierarchyGridCell>();

        //private readonly List<(Guid, Guid, HierarchyGridCell)> _drawnCells =
        //    new List<(Guid, Guid, HierarchyGridCell)>();

        //private readonly List<HierarchyGridHeader> _headers = new List<HierarchyGridHeader>();
        //private readonly List<GridSplitter> _splitters = new List<GridSplitter>();

        //private readonly HashSet<HierarchyDefinition> _columnsParents = new HashSet<HierarchyDefinition>();
        //private readonly HashSet<HierarchyDefinition> _rowsParents = new HashSet<HierarchyDefinition>();

        //private void DrawCells( PositionedCell[] pCells , bool invalidate )
        //{
        //    // Find cells that were previously drawn
        //    var commonCells = _drawnCells.Join( pCells ,
        //        o => new { pId = o.Item1 , cId = o.Item2 } ,
        //        p => new { pId = p.ProducerDefinition.Guid , cId = p.ConsumerDefinition.Guid } ,
        //        ( o , p ) => (drawn: o, pos: p) )
        //        .ToArray();

        //    commonCells.ForEach( t =>
        //        {
        //            var cell = t.drawn.Item3;

        //            cell.Width = t.pos.Width;
        //            cell.Height = t.pos.Height;
        //            Canvas.SetLeft( cell , t.pos.Left );
        //            Canvas.SetTop( cell , t.pos.Top );

        //            if ( invalidate )
        //            {
        //                InvalidateCell( t.pos , cell );
        //            }
        //        } );

        //    _drawnCells.Where( x => !commonCells.Select( c => c.drawn.Item3 ).Contains( x.Item3 ) )
        //        .ForEach( c => HierarchyGridCanvas.Children.Remove( c.Item3 ) );

        //    var cells = pCells.Where( x => !commonCells.Select( t => t.pos ).Contains( x ) )
        //        .Select( pCell => (pCell.ProducerDefinition.Guid, pCell.ConsumerDefinition.Guid, CreateCell( pCell )) )
        //        .ToArray();

        //    foreach ( var cell in cells )
        //        HierarchyGridCanvas.Children.Add( cell.Item3 );

        //    _drawnCells.Clear();
        //    _drawnCells.AddRange( cells.Concat( commonCells.Select( t => t.drawn ) ) );
        //}

        //private void DrawGrid( Size size )
        //{
        //    ViewModel.HoveredRow = -1;
        //    ViewModel.HoveredColumn = -1;

        //    if ( !ViewModel.IsValid )
        //    {
        //        HierarchyGridCanvas.Children.Clear();
        //        return;
        //    }

        //    var rowDefinitions = ViewModel.RowsDefinitions.Leaves().ToArray();
        //    var colDefinitions = ViewModel.ColumnsDefinitions.Leaves().ToArray();

        //    foreach ( var hdr in HierarchyGridCanvas.Children.OfType<HierarchyGridHeader>().ToArray() )
        //        HierarchyGridCanvas.Children.Remove( hdr );

        //    foreach ( var gs in HierarchyGridCanvas.Children.OfType<GridSplitter>().ToArray() )
        //        HierarchyGridCanvas.Children.Remove( gs );

        //    _columnsParents.Clear();
        //    _rowsParents.Clear();

        //    int headerCount = 0;
        //    int splitterCount = 0;

        //    DrawColumnsHeaders( colDefinitions , size.Width / ViewModel.Scale , ref headerCount , ref splitterCount );
        //    DrawRowsHeaders( rowDefinitions , size.Height / ViewModel.Scale , ref headerCount , ref splitterCount );

        //    // Draw global headers afterwards or last splitter will be drawn under column headers
        //    DrawGlobalHeaders( ref headerCount , ref splitterCount );

        //    //DrawCells( size , rowDefinitions , colDefinitions );

        //    RestoreHighlightedCell();
        //    RestoreHoveredCell();
        //}

        //private void RestoreHighlightedCell()
        //{
        //    var rows = ViewModel.Highlights.Items.Where( o => o.isRow ).Select( o => o.pos ).ToArray();
        //    var columns = ViewModel.Highlights.Items.Where( o => !o.isRow ).Select( o => o.pos ).ToArray();

        //    _cells.Where( c => columns.Contains( c.ViewModel.ColumnIndex ) || rows.Contains( c.ViewModel.RowIndex ) )
        //        .ForAll( c => c.ViewModel.IsHighlighted = true );
        //}

        //private void RestoreHoveredCell()
        //{
        //    var hoveredCell = ( Mouse.DirectlyOver as DependencyObject )?.GetVisualParent<HierarchyGridCell>();
        //    if ( hoveredCell != null )
        //    {
        //        this.Log().Debug( hoveredCell );
        //        hoveredCell.ViewModel.IsHovered = true;

        //        if ( ViewModel.EnableCrosshair )
        //        {
        //            ViewModel.HoveredColumn = hoveredCell.ViewModel.ColumnIndex;
        //            ViewModel.HoveredRow = hoveredCell.ViewModel.RowIndex;
        //        }
        //        else
        //        {
        //            ViewModel.ClearCrosshair();
        //        }
        //    }
        //}

        //private void DrawGlobalHeaders( ref int headerCount , ref int splitterCount )
        //{
        //    var rowDepth = ViewModel.RowsDefinitions.TotalDepth();
        //    var colDepth = ViewModel.ColumnsDefinitions.TotalDepth();

        //    var splitters = new List<GridSplitter>();
        //    Action<DragCompletedEventArgs> action = e =>
        //    {
        //        (int position, IDisposable drag) tag = ((int, IDisposable)) ( (GridSplitter) e.Source ).Tag;
        //        var idx = tag.position;
        //        ViewModel.RowsHeadersWidth[idx] = (double) Math.Max( ViewModel.RowsHeadersWidth[idx] + e.HorizontalChange , 10d );

        //        Observable.Return( (ViewModel.HorizontalOffset, ViewModel.VerticalOffset, ViewModel.Width,
        //                ViewModel.Height, ViewModel.Scale, false) )
        //            .InvokeCommand( ViewModel , x => x.FindCellsToDrawCommand );
        //    };

        //    double currentX = 0, currentY = 0;

        //    var columnsVerticalSpan = ViewModel.ColumnsHeadersHeight.Take( ViewModel.ColumnsHeadersHeight.Length - 1 ).Sum();
        //    var rowsHorizontalSpan = ViewModel.RowsHeadersWidth.Take( ViewModel.RowsHeadersWidth.Length - 1 ).Sum();

        //    var foldAllButton = BuildHeader( ref headerCount , null , rowsHorizontalSpan , columnsVerticalSpan );
        //    foldAllButton.ViewModel.Content = TryFindResource( "CollapseAllIcon" );
        //    foldAllButton.ToolTip = "Collapse all";
        //    var evts = new Queue<IDisposable>();
        //    evts.Enqueue( foldAllButton.Events().MouseLeftButtonDown
        //        .Do( _ =>
        //         {
        //             ViewModel.SelectedPositions.Clear();
        //             ViewModel.RowsDefinitions.FlatList( true ).ForEach( x => x.IsExpanded = false );
        //             ViewModel.ColumnsDefinitions.FlatList( true ).ForEach( x => x.IsExpanded = false );

        //             Observable.Return( (ViewModel.HorizontalOffset, ViewModel.VerticalOffset, ViewModel.Width,
        //                     ViewModel.Height, ViewModel.Scale, false) )
        //                 .InvokeCommand( ViewModel , x => x.FindCellsToDrawCommand );
        //         } )
        //        .Select( _ => Unit.Default )
        //        .InvokeCommand( ViewModel , x => x.DrawGridCommand ) );
        //    foldAllButton.Tag = evts;

        //    Canvas.SetLeft( foldAllButton , currentX );
        //    Canvas.SetTop( foldAllButton , currentY );
        //    HierarchyGridCanvas.Children.Add( foldAllButton );

        //    // Draw row headers
        //    currentY = columnsVerticalSpan;
        //    for ( int i = 0 ; i < rowDepth - 1 ; i++ )
        //    {
        //        var width = ViewModel.RowsHeadersWidth[i];
        //        var height = ViewModel.ColumnsHeadersHeight.Last();
        //        var tb = BuildHeader( ref headerCount , null , width , height );
        //        var queue = new Queue<IDisposable>();
        //        var idx = i; // Copy to local variable or else event will always use last value of i
        //        evts.Enqueue( tb.Events().MouseLeftButtonDown
        //        .Do( _ =>
        //         {
        //             ViewModel.SelectedPositions.Clear();

        //             var defs = ViewModel.RowsDefinitions.FlatList( true )
        //                                      .Where( x => x.Level == idx )
        //                                      .ToArray();
        //             var desiredState = defs.AsParallel().Any( x => x.IsExpanded );

        //             defs.ForEach( x => x.IsExpanded = !desiredState );

        //             Observable.Return( (ViewModel.HorizontalOffset, ViewModel.VerticalOffset, ViewModel.Width,
        //                     ViewModel.Height, ViewModel.Scale, false) )
        //                 .InvokeCommand( ViewModel , x => x.FindCellsToDrawCommand );
        //         } )
        //        .Select( _ => Unit.Default )
        //        .InvokeCommand( ViewModel , x => x.DrawGridCommand ) );
        //        tb.ViewModel.Content = ViewModel.RowsDefinitions
        //                                    .FlatList( true )
        //                                    .AsParallel()
        //                                    .Where( x => x.Level == idx )
        //                                    .Any( x => x.IsExpanded ) ? TryFindResource( "CollapseIcon" ) : TryFindResource( "ExpandIcon" );
        //        tb.Tag = evts;

        //        Canvas.SetLeft( tb , currentX );
        //        Canvas.SetTop( tb , currentY );
        //        HierarchyGridCanvas.Children.Add( tb );
        //        currentX += width;

        //        var gSplitter = BuildSplitter( ref splitterCount , 2 , height , GridResizeDirection.Columns , i , action );
        //        Canvas.SetLeft( gSplitter , currentX );
        //        Canvas.SetTop( gSplitter , currentY );
        //        splitters.Add( gSplitter );
        //    }

        //    // Draw column headers
        //    currentY = 0;
        //    for ( int i = 0 ; i < colDepth - 1 ; i++ )
        //    {
        //        var width = ViewModel.RowsHeadersWidth.Last();
        //        var height = ViewModel.ColumnsHeadersHeight[i];
        //        var tb = BuildHeader( ref headerCount , null , width , height );

        //        var idx = i; // Copy to local variable or else event will always use last value of i
        //        evts.Enqueue( tb.Events().MouseLeftButtonDown
        //        .Do( _ =>
        //         {
        //             ViewModel.SelectedPositions.Clear();
        //             var defs = ViewModel.ColumnsDefinitions.FlatList( true )
        //                                      .Where( x => x.Level == idx )
        //                                      .ToArray();
        //             var desiredState = defs.AsParallel().Any( x => x.IsExpanded );
        //             defs.ForEach( x => x.IsExpanded = !desiredState );

        //             Observable.Return( (ViewModel.HorizontalOffset, ViewModel.VerticalOffset, ViewModel.Width,
        //                     ViewModel.Height, ViewModel.Scale, false) )
        //                 .InvokeCommand( ViewModel , x => x.FindCellsToDrawCommand );
        //         } )
        //        .Select( _ => Unit.Default )
        //        .InvokeCommand( ViewModel , x => x.DrawGridCommand ) );
        //        tb.ViewModel.Content = ViewModel.ColumnsDefinitions
        //                                    .FlatList( true )
        //                                    .AsParallel()
        //                                    .Where( x => x.Level == idx )
        //                                    .Any( x => x.IsExpanded ) ? TryFindResource( "CollapseIcon" ) : TryFindResource( "ExpandIcon" );
        //        tb.Tag = evts;

        //        Canvas.SetLeft( tb , currentX );
        //        Canvas.SetTop( tb , currentY );
        //        HierarchyGridCanvas.Children.Add( tb );
        //        currentY += height;
        //    }

        //    var expandAllButton = BuildHeader( ref headerCount , null , ViewModel.RowsHeadersWidth.Last() , ViewModel.ColumnsHeadersHeight.Last() );
        //    expandAllButton.ViewModel.Content = TryFindResource( "ExpandAllIcon" );

        //    expandAllButton.ToolTip = "Expand all";
        //    evts = new Queue<IDisposable>();
        //    evts.Enqueue( expandAllButton.Events().MouseLeftButtonDown
        //        .Do( _ =>
        //         {
        //             ViewModel.SelectedPositions.Clear();
        //             ViewModel.RowsDefinitions.FlatList( true ).ForEach( x => x.IsExpanded = true );
        //             ViewModel.ColumnsDefinitions.FlatList( true ).ForEach( x => x.IsExpanded = true );

        //             Observable.Return( (ViewModel.HorizontalOffset, ViewModel.VerticalOffset, ViewModel.Width,
        //                     ViewModel.Height, ViewModel.Scale, false) )
        //                 .InvokeCommand( ViewModel , x => x.FindCellsToDrawCommand );
        //         } )
        //        .Select( _ => Unit.Default )
        //        .InvokeCommand( ViewModel , x => x.DrawGridCommand ) );
        //    expandAllButton.Tag = evts;

        //    Canvas.SetLeft( expandAllButton , currentX );
        //    Canvas.SetTop( expandAllButton , currentY );
        //    HierarchyGridCanvas.Children.Add( expandAllButton );

        //    var splitter = BuildSplitter( ref splitterCount ,
        //        2 ,
        //        ViewModel.ColumnsHeadersHeight.Last() ,
        //        GridResizeDirection.Columns ,
        //        ViewModel.RowsHeadersWidth.Length - 1 ,
        //        action );
        //    Canvas.SetLeft( splitter , ViewModel.RowsHeadersWidth.Sum() );
        //    Canvas.SetTop( splitter , currentY );
        //    splitters.Add( splitter );

        //    foreach ( var gridSplitter in splitters )
        //        HierarchyGridCanvas.Children.Add( gridSplitter );
        //}

        //private void InvalidateCell( PositionedCell pCell , HierarchyGridCell cell )
        //{
        //    cell.ViewModel.ResultSet = pCell.ResultSet;
        //}

        //private HierarchyGridCell CreateCell( PositionedCell pCell )
        //{
        //    var vm = new HierarchyGridCellViewModel( ViewModel );
        //    var cell = new HierarchyGridCell { ViewModel = vm };

        //    vm.ResultSet = ResultSet.Default;

        //    cell.ViewModel.IsSelected = ViewModel.SelectedPositions.Lookup( (pCell.VerticalPosition, pCell.HorizontalPosition) ).HasValue;

        //    cell.ViewModel.ColumnIndex = pCell.HorizontalPosition;
        //    cell.ViewModel.RowIndex = pCell.VerticalPosition;

        //    cell.Width = pCell.Width;
        //    cell.Height = pCell.Height;

        //    cell.ViewModel.ResultSet =
        //        HierarchyDefinition.Resolve( pCell.ProducerDefinition , pCell.ConsumerDefinition );

        //    Canvas.SetLeft( cell , pCell.Left );
        //    Canvas.SetTop( cell , pCell.Top );

        //    return cell;
        //}

        //private void DrawColumnsHeaders( HierarchyDefinition[] hdefs , double availableWidth , ref int headerCount , ref int splitterCount )
        //{
        //    double currentPosition = ViewModel.RowsHeadersWidth.Sum();
        //    int column = ViewModel.HorizontalOffset;

        //    var frozen = hdefs.Where( x => x.Frozen ).ToArray();

        //    ViewModel.MaxHorizontalOffset = hdefs.Length - ( 1 + frozen.Length );
        //    var splitters = new List<GridSplitter>();

        //    foreach ( var hdef in frozen )
        //    {
        //        var width = ViewModel.ColumnsWidths[hdefs.IndexOf( hdef )];
        //        DrawColumnHeader( ref headerCount , ref splitterCount , ref currentPosition , column , splitters , hdef , width );
        //        column++;
        //    }

        //    while ( column < hdefs.Length && currentPosition < availableWidth )
        //    {
        //        var hdef = hdefs[column];
        //        var width = ViewModel.ColumnsWidths[column];

        //        DrawColumnHeader( ref headerCount , ref splitterCount , ref currentPosition , column , splitters , hdef , width );
        //        column++;
        //    }

        //    foreach ( var gridSplitter in splitters )
        //        HierarchyGridCanvas.Children.Add( gridSplitter );
        //}

        //private void DrawColumnHeader( ref int headerCount , ref int splitterCount , ref double currentPosition , int column , List<GridSplitter> splitters , HierarchyDefinition hdef , double width )
        //{
        //    var height = hdef.IsExpanded && hdef.HasChild ?
        //                        ViewModel.ColumnsHeadersHeight[hdef.Level] :
        //                        Enumerable.Range( hdef.Level , ViewModel.ColumnsHeadersHeight.Length - hdef.Level )
        //                            .Select( x => ViewModel.ColumnsHeadersHeight[x] ).Sum();

        //    var tb = BuildHeader( ref headerCount , hdef , width , height );
        //    tb.ViewModel.ColumnIndex = column;

        //    var top = Enumerable.Range( 0 , hdef.Level ).Select( x => ViewModel.ColumnsHeadersHeight[x] ).Sum();
        //    Canvas.SetLeft( tb , currentPosition );
        //    Canvas.SetTop( tb , top );
        //    HierarchyGridCanvas.Children.Add( tb );

        //    Action<DragCompletedEventArgs> action = e =>
        //    {
        //        (int position, IDisposable drag) tag = ((int, IDisposable)) ( (GridSplitter) e.Source ).Tag;
        //        var currentColumn = tag.position;
        //        ViewModel.ColumnsWidths[currentColumn] = (double) Math.Max( ViewModel.ColumnsWidths[currentColumn] + e.HorizontalChange , 10d );

        //        Observable.Return( (ViewModel.HorizontalOffset, ViewModel.VerticalOffset, ViewModel.Width,
        //                ViewModel.Height, ViewModel.Scale, false) )
        //            .InvokeCommand( ViewModel , x => x.FindCellsToDrawCommand );
        //    };
        //    var gridSplitter = BuildSplitter( ref splitterCount , 2 , height , GridResizeDirection.Columns , column , action );

        //    Canvas.SetLeft( gridSplitter , currentPosition + width - 1 );
        //    Canvas.SetTop( gridSplitter , top );
        //    splitters.Add( gridSplitter );

        //    DrawParentColumnHeader( hdef , hdef , column , currentPosition , ref headerCount );
        //    currentPosition += width;
        //}

        //private void DrawParentColumnHeader( HierarchyDefinition src , HierarchyDefinition origin , int column , double currentPosition , ref int headerCount )
        //{
        //    if ( src.Parent == null )
        //        return;

        //    var hdef = src.Parent;

        //    if ( _columnsParents.Contains( hdef ) )
        //        return;

        //    var width = Enumerable.Range( column , hdef.Count() - origin.RelativePositionFrom( hdef ) )
        //        .Select( x => ViewModel.ColumnsWidths.TryGetValue( x , out var size ) ? size : 0 ).Sum();

        //    var height = ViewModel.ColumnsHeadersHeight[hdef.Level];

        //    var tb = BuildHeader( ref headerCount , hdef , width , height );
        //    tb.ViewModel.CanToggle = true;

        //    var top = Enumerable.Range( 0 , hdef.Level ).Select( x => ViewModel.ColumnsHeadersHeight[x] ).Sum();
        //    Canvas.SetLeft( tb , currentPosition );
        //    Canvas.SetTop( tb , top );
        //    HierarchyGridCanvas.Children.Add( tb );

        //    _columnsParents.Add( hdef );

        //    DrawParentColumnHeader( hdef , origin , column , currentPosition , ref headerCount );
        //}

        //private void DrawRowsHeaders( HierarchyDefinition[] hdefs , double availableHeight , ref int headerCount , ref int splitterCount )
        //{
        //    double currentPosition = ViewModel.ColumnsHeadersHeight.Sum();
        //    int row = ViewModel.VerticalOffset;

        //    var frozen = hdefs.Where( x => x.Frozen ).ToArray();

        //    ViewModel.MaxVerticalOffset = hdefs.Length - ( 1 + frozen.Length );
        //    var splitters = new List<GridSplitter>();

        //    foreach ( var hdef in frozen )
        //    {
        //        var height = ViewModel.RowsHeights[hdefs.IndexOf( hdef )];
        //        DrawRowHeader( ref headerCount , ref splitterCount , ref currentPosition , row , splitters , hdef , height );
        //        row++;
        //    }

        //    while ( row < hdefs.Length && currentPosition < availableHeight )
        //    {
        //        var hdef = hdefs[row];
        //        var height = ViewModel.RowsHeights[row];
        //        DrawRowHeader( ref headerCount , ref splitterCount , ref currentPosition , row , splitters , hdef , height );

        //        row++;
        //    }

        //    foreach ( var gridSplitter in splitters )
        //        HierarchyGridCanvas.Children.Add( gridSplitter );
        //}

        //private void DrawRowHeader( ref int headerCount , ref int splitterCount , ref double currentPosition , int row , List<GridSplitter> splitters , HierarchyDefinition hdef , double height )
        //{
        //    var width = hdef.IsExpanded && hdef.HasChild ?
        //                        ViewModel.RowsHeadersWidth[hdef.Level] :
        //                        Enumerable.Range( hdef.Level , ViewModel.RowsHeadersWidth.Length - hdef.Level )
        //                            .Where( x => x < ViewModel.RowsHeadersWidth.Length )
        //                            .Select( x => ViewModel.RowsHeadersWidth[x] ).Sum();

        //    var tb = BuildHeader( ref headerCount , hdef , width , height );
        //    tb.ViewModel.RowIndex = row;

        //    var left = Enumerable.Range( 0 , hdef.Level ).Where( x => x < ViewModel.RowsHeadersWidth.Length ).Select( x => ViewModel.RowsHeadersWidth[x] ).Sum();
        //    Canvas.SetLeft( tb , left );
        //    Canvas.SetTop( tb , currentPosition );
        //    HierarchyGridCanvas.Children.Add( tb );

        //    Action<DragCompletedEventArgs> action = e =>
        //    {
        //        (int position, IDisposable drag) tag = ((int, IDisposable)) ( (GridSplitter) e.Source ).Tag;
        //        var currentRow = tag.position;
        //        ViewModel.RowsHeights[currentRow] = (double) Math.Max( ViewModel.RowsHeights[currentRow] + e.VerticalChange , 10d );

        //        Observable.Return( (ViewModel.HorizontalOffset, ViewModel.VerticalOffset, ViewModel.Width,
        //                ViewModel.Height, ViewModel.Scale, false) )
        //            .InvokeCommand( ViewModel , x => x.FindCellsToDrawCommand );
        //    };
        //    var gridSplitter = BuildSplitter( ref splitterCount , width , 2 , GridResizeDirection.Rows , row , action );

        //    Canvas.SetLeft( gridSplitter , left );
        //    Canvas.SetTop( gridSplitter , currentPosition + height - 1 );
        //    splitters.Add( gridSplitter );

        //    DrawParentRowHeader( hdef , hdef , row , currentPosition , ref headerCount );

        //    currentPosition += height;
        //}

        //private void DrawParentRowHeader( HierarchyDefinition src , HierarchyDefinition origin , int row , double currentPosition , ref int headerCount )
        //{
        //    if ( src.Parent == null )
        //        return;

        //    var hdef = src.Parent;

        //    if ( _rowsParents.Contains( hdef ) )
        //        return;

        //    var height = Enumerable.Range( row , hdef.Count() - origin.RelativePositionFrom( hdef ) )
        //        .Select( x => ViewModel.RowsHeights.TryGetValue( x , out var size ) ? size : 0 ).Sum();

        //    var width = ViewModel.RowsHeadersWidth[hdef.Level];

        //    var tb = BuildHeader( ref headerCount , hdef , width , height );
        //    tb.ViewModel.CanToggle = true;

        //    var left = Enumerable.Range( 0 , hdef.Level ).Where( x => x < ViewModel.RowsHeadersWidth.Length ).Select( x => ViewModel.RowsHeadersWidth[x] ).Sum();
        //    Canvas.SetLeft( tb , left );
        //    Canvas.SetTop( tb , currentPosition );
        //    HierarchyGridCanvas.Children.Add( tb );

        //    _rowsParents.Add( hdef );

        //    DrawParentRowHeader( hdef , origin , row , currentPosition , ref headerCount );
        //}

        //private HierarchyGridHeader BuildHeader( ref int headerCount , [AllowNull] HierarchyDefinition hdef , double width , double height )
        //{
        //    HierarchyGridHeader tb = null;
        //    if ( headerCount < _headers.Count )
        //    {
        //        tb = _headers[headerCount];
        //        if ( tb.Tag is Queue<IDisposable> previousEvents )
        //            previousEvents.ForEach( e => e.Dispose() );
        //        tb.Tag = null;
        //        tb.ViewModel.RowIndex = null;
        //        tb.ViewModel.ColumnIndex = null;
        //        tb.ViewModel.CanToggle = false;
        //    }
        //    else
        //    {
        //        tb = new HierarchyGridHeader { ViewModel = new HierarchyGridHeaderViewModel( ViewModel ) };
        //        _headers.Add( tb );
        //    }

        //    tb.ViewModel.Content = hdef?.Content ?? string.Empty;
        //    tb.Height = height;
        //    tb.Width = width;
        //    tb.ViewModel.IsChecked = hdef?.HasChild == true && hdef?.IsExpanded == true;
        //    tb.ViewModel.IsHighlighted = hdef?.IsHighlighted ?? false;

        //    var evts = new Queue<IDisposable>();
        //    if ( hdef?.HasChild == true )
        //    {
        //        tb.ViewModel.CanToggle = true;

        //        evts.Enqueue( tb.Events().MouseLeftButtonDown
        //            .Do( _ =>
        //             {
        //                 ViewModel.SelectedPositions.Clear();
        //                 hdef.IsExpanded = !hdef.IsExpanded;

        //                 Observable.Return( (ViewModel.HorizontalOffset, ViewModel.VerticalOffset, ViewModel.Width,
        //                         ViewModel.Height, ViewModel.Scale, false) )
        //                     .InvokeCommand( ViewModel , x => x.FindCellsToDrawCommand );
        //             } )
        //            .Select( _ => Unit.Default )
        //            .InvokeCommand( ViewModel , x => x.DrawGridCommand ) );
        //    }
        //    else if ( hdef != null )
        //        // Clicking on header should add/remove from highlights
        //        evts.Enqueue( tb.Events().MouseLeftButtonDown
        //            .Throttle( TimeSpan.FromMilliseconds( 200 ) )
        //            .ObserveOn( RxApp.MainThreadScheduler )
        //            .Select( _ =>
        //             {
        //                 hdef.IsHighlighted = !hdef.IsHighlighted;
        //                 tb.ViewModel.IsHighlighted = hdef.IsHighlighted;

        //                 return tb.ViewModel;
        //             } )
        //            .InvokeCommand( ViewModel , x => x.UpdateHighlightsCommand ) );

        //    tb.Tag = evts;
        //    headerCount++;

        //    return tb;
        //}

        //private GridSplitter BuildSplitter( ref int splitterCount , double width , double height , GridResizeDirection direction , int position , Action<DragCompletedEventArgs> action )
        //{
        //    GridSplitter gridSplitter = null;

        //    if ( splitterCount < _splitters.Count )
        //    {
        //        gridSplitter = _splitters[splitterCount];

        //        (int position, IDisposable drag) tag = ((int, IDisposable)) gridSplitter.Tag;
        //        tag.drag.Dispose();
        //    }
        //    else
        //    {
        //        gridSplitter = new GridSplitter();
        //        _splitters.Add( gridSplitter );
        //    }

        //    gridSplitter.Width = width;
        //    gridSplitter.Height = height;
        //    gridSplitter.ResizeDirection = direction;
        //    gridSplitter.Background = Brushes.Transparent;

        //    var drag = gridSplitter.Events().DragCompleted
        //            .Do( e => action( e ) )
        //            .Select( _ => Unit.Default )
        //            .ObserveOn( RxApp.MainThreadScheduler )
        //            .InvokeCommand( ViewModel , x => x.DrawGridCommand );

        //    gridSplitter.Tag = (position, drag);

        //    splitterCount++;
        //    return gridSplitter;
        //}
    }
}