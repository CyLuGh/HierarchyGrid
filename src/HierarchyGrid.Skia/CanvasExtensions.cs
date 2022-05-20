using HierarchyGrid.Definitions;
using SkiaSharp;
using SkiaSharp.HarfBuzz;
using System.Data.Common;
using Topten.RichTextKit;

namespace HierarchyGrid.Skia
{
    internal static class CanvasExtensions
    {
        internal static void DrawColumnHeaders( this SKCanvas canvas , HierarchyGridViewModel viewModel , Func<HierarchyGridViewModel , HierarchyDefinition[]> selector , float availableWidth , ref int headerCount )
        {
            viewModel.ColumnsParents.Clear();
            var hdefs = selector( viewModel ).ToArr();

            /* First column header X position begins at row headers width */
            double currentPosition = viewModel.RowsHeadersWidth.Sum();

            /* Index of the first column to be drawn */
            int column = viewModel.HorizontalOffset;

            var frozen = hdefs.Where( x => x.Frozen ).ToArr();
            viewModel.MaxHorizontalOffset = hdefs.Length - ( 1 + frozen.Length );

            foreach ( var hdef in frozen )
            {
                var width = viewModel.ColumnsWidths[hdefs.IndexOf( hdef )];
                canvas.DrawColumnHeader( viewModel , ref headerCount , ref currentPosition , column , hdef , width );
                column++;
            }

            while ( column < hdefs.Length && currentPosition < availableWidth )
            {
                var hdef = hdefs[column];
                var width = viewModel.ColumnsWidths[column];

                canvas.DrawColumnHeader( viewModel , ref headerCount , ref currentPosition , column , hdef , width );
                column++;
            }
        }

        internal static void DrawColumnHeader( this SKCanvas canvas , HierarchyGridViewModel viewModel , ref int headerCount , ref double currentPosition , int column , HierarchyDefinition hdef , double width )
        {
            var height = hdef.IsExpanded && hdef.HasChild ?
                                viewModel.ColumnsHeadersHeight[hdef.Level] :
                                Enumerable.Range( hdef.Level , viewModel.ColumnsHeadersHeight.Length - hdef.Level )
                                    .Select( x => viewModel.ColumnsHeadersHeight[x] ).Sum();

            var top = Enumerable.Range( 0 , hdef.Level )
                .Select( x => viewModel.ColumnsHeadersHeight[x] )
                .Sum();

            canvas.DrawHeader( ref headerCount , hdef , currentPosition , top , width , height );

            canvas.DrawParentColumnHeader( viewModel , hdef , hdef , column , currentPosition , ref headerCount );
            currentPosition += width;
        }

        internal static void DrawParentColumnHeader( this SKCanvas canvas , HierarchyGridViewModel viewModel , HierarchyDefinition src , HierarchyDefinition origin , int column , double currentPosition , ref int headerCount )
        {
            if ( src.Parent == null )
                return;

            var hdef = src.Parent;

            if ( viewModel.ColumnsParents.Contains( hdef ) )
                return;

            var width = Enumerable.Range( column , hdef.Count() - origin.RelativePositionFrom( hdef ) )
                .Select( x => viewModel.ColumnsWidths.TryGetValue( x , out var size ) ? size : 0 ).Sum();

            var top = Enumerable.Range( 0 , hdef.Level )
                .Select( x => viewModel.ColumnsHeadersHeight[x] )
                .Sum();
            var height = viewModel.ColumnsHeadersHeight[hdef.Level];
            canvas.DrawHeader( ref headerCount , hdef , currentPosition , top , width , height );

            viewModel.ColumnsParents.Add( hdef );

            canvas.DrawParentColumnHeader( viewModel , hdef , origin , column , currentPosition , ref headerCount );
        }

        internal static void DrawRowHeaders( this SKCanvas canvas , HierarchyGridViewModel viewModel , Func<HierarchyGridViewModel , HierarchyDefinition[]> selector , float availableHeight , ref int headerCount )
        {
            viewModel.RowsParents.Clear();
            var hdefs = selector( viewModel ).ToArr();

            /* First row header Y position begins at column headers height */
            double currentPosition = viewModel.ColumnsHeadersHeight.Sum();

            /* Index of the first row to be drawn */
            int row = viewModel.VerticalOffset;

            var frozen = hdefs.Where( x => x.Frozen ).ToArr();
            viewModel.MaxVerticalOffset = hdefs.Length - ( 1 + frozen.Length );

            foreach ( var hdef in frozen )
            {
                var height = viewModel.RowsHeights[hdefs.IndexOf( hdef )];
                canvas.DrawRowHeader( viewModel , ref headerCount , ref currentPosition , row , hdef , height );
                row++;
            }

            while ( row < hdefs.Length && currentPosition < availableHeight )
            {
                var hdef = hdefs[row];
                var height = viewModel.RowsHeights[row];

                canvas.DrawRowHeader( viewModel , ref headerCount , ref currentPosition , row , hdef , height );
                row++;
            }
        }

        internal static void DrawRowHeader( this SKCanvas canvas , HierarchyGridViewModel viewModel , ref int headerCount , ref double currentPosition , int row , HierarchyDefinition hdef , double height )
        {
            var width = hdef.IsExpanded && hdef.HasChild ?
                                viewModel.RowsHeadersWidth[hdef.Level] :
                                Enumerable.Range( hdef.Level , viewModel.RowsHeadersWidth.Length - hdef.Level )
                                    .Where( x => x < viewModel.RowsHeadersWidth.Length )
                                    .Select( x => viewModel.RowsHeadersWidth[x] ).Sum();

            var left = Enumerable.Range( 0 , hdef.Level )
                .Where( x => x < viewModel.RowsHeadersWidth.Length )
                .Select( x => viewModel.RowsHeadersWidth[x] ).Sum();

            canvas.DrawHeader( ref headerCount , hdef , left , currentPosition , width , height );

            canvas.DrawParentRowHeader( viewModel , hdef , hdef , row , currentPosition , ref headerCount );

            currentPosition += height;
        }

        internal static void DrawParentRowHeader( this SKCanvas canvas , HierarchyGridViewModel viewModel , HierarchyDefinition src , HierarchyDefinition origin , int row , double currentPosition , ref int headerCount )
        {
            if ( src.Parent == null )
                return;

            var hdef = src.Parent;

            if ( viewModel.RowsParents.Contains( hdef ) )
                return;

            var height = Enumerable.Range( row , hdef.Count() - origin.RelativePositionFrom( hdef ) )
                .Select( x => viewModel.RowsHeights.TryGetValue( x , out var size ) ? size : 0 ).Sum();

            var left = Enumerable.Range( 0 , hdef.Level )
                .Where( x => x < viewModel.RowsHeadersWidth.Length )
                .Select( x => viewModel.RowsHeadersWidth[x] ).Sum();
            var width = viewModel.RowsHeadersWidth[hdef.Level];
            canvas.DrawHeader( ref headerCount , hdef , left , currentPosition , width , height );

            viewModel.RowsParents.Add( hdef );

            canvas.DrawParentRowHeader( viewModel , hdef , origin , row , currentPosition , ref headerCount );
        }

        internal static void DrawHeader( this SKCanvas canvas , ref int headerCount , HierarchyDefinition hdef , double left , double top , double width , double height )
        {
            var rect = SKRect.Create( (float) left , (float) top , (float) width , (float) height );

            using var paint = new SKPaint();
            paint.Style = SKPaintStyle.Fill;
            paint.Color = SKColors.LightBlue;
            canvas.DrawRect( rect , paint );

            paint.Style = SKPaintStyle.Stroke;
            paint.Color = SKColors.SlateGray;
            canvas.DrawRect( rect , paint );

            TextDrawer.Clear();
            TextDrawer.Alignment = TextAlignment.Left;
            TextDrawer.AddText( hdef.Content.ToString() , new Style { FontSize = TextSize , TextColor = SKColors.DarkSlateGray } );
            TextDrawer.MaxHeight = (float) height;
            TextDrawer.MaxWidth = (float) width;

            TextDrawer.Paint( canvas , new SKPoint( (float) left + 2 , (float) top + 2 ) , TextPaintOptions );

            headerCount++;
        }

        internal static void DrawCells( this SKCanvas canvas , HierarchyGridViewModel viewModel , PositionedCell[] cells )
        {
            foreach ( var cell in cells )
                canvas.DrawCell( viewModel , cell );
        }

        internal static void DrawCell( this SKCanvas canvas , HierarchyGridViewModel viewModel , PositionedCell cell )
        {
            var rect = SKRect.Create( (float) cell.Left , (float) cell.Top , (float) cell.Width , (float) cell.Height );

            using var paint = new SKPaint();
            paint.Style = SKPaintStyle.Fill;
            paint.Color = SKColors.LightYellow;
            canvas.DrawRect( rect , paint );

            TextDrawer.Clear();
            TextDrawer.Alignment = TextAlignment.Right;
            TextDrawer.AddText( cell.ResultSet.Result , new Style { FontSize = TextSize , TextColor = SKColors.DarkSlateGray } );
            TextDrawer.MaxHeight = (float) cell.Height;
            TextDrawer.MaxWidth = (float) cell.Width;

            TextDrawer.Paint( canvas , new SKPoint( (float) cell.Left , (float) cell.Top + 2 ) , TextPaintOptions );
            viewModel.CellsCoordinates.Add( (new ElementCoordinates( cell ), cell) );
        }

        private static TextBlock TextDrawer { get; } = new TextBlock();
        private static TextPaintOptions TextPaintOptions { get; } = new TextPaintOptions { Edging = SKFontEdging.Antialias };
        private static readonly float TextSize = 14f;
    }
}