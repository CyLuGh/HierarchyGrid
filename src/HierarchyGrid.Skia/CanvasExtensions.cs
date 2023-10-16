using HierarchyGrid.Definitions;
using LanguageExt;
using SkiaSharp;
using Topten.RichTextKit;

namespace HierarchyGrid.Skia
{
    internal static class CanvasExtensions
    {
        private enum GlobalHeader
        { CollapseAll, ExpandAll, Local }

        internal static void DrawGlobalHeaders( this SKCanvas canvas ,
            HierarchyGridViewModel viewModel ,
            SkiaTheme theme ,
            IList<(ElementCoordinates, Guid)> previousGlobalCoordinates ,
            double screenScale = 1d )
        {
            var rowDepth = viewModel.RowsDefinitions.TotalDepth();
            var colDepth = viewModel.ColumnsDefinitions.TotalDepth();

            var columnsVerticalSpan = viewModel.ColumnsHeadersHeight.Take( viewModel.ColumnsHeadersHeight.Length - 1 ).Sum();
            var rowsHorizontalSpan = viewModel.RowsHeadersWidth.Take( viewModel.RowsHeadersWidth.Length - 1 ).Sum();

            canvas.DrawGlobalHeader( viewModel ,
                previousGlobalCoordinates ,
                theme ,
                vm => vm.ColumnsDefinitions.FlatList().Concat( vm.RowsDefinitions.FlatList() ) ,
                ( hd , b ) => hd.IsExpanded = b ,
                false ,
                0 , 0 , rowsHorizontalSpan , columnsVerticalSpan , screenScale , GlobalHeader.CollapseAll );

            /* Draw rows global headers */
            var currentY = columnsVerticalSpan;
            var currentX = 0d;

            for ( int i = 0 ; i < rowDepth - 1 ; i++ )
            {
                var lvl = i;
                var width = viewModel.RowsHeadersWidth[i];

                canvas.DrawGlobalHeader( viewModel ,
                    previousGlobalCoordinates ,
                    theme ,
                    vm => vm.RowsDefinitions.FlatList().Where( x => x.Level == lvl ) ,
                    ( hd , exp ) => hd.IsExpanded = exp ,
                    viewModel.RowsDefinitions.FlatList().Where( x => x.Level == lvl ).All( x => !x.IsExpanded ) ,
                    currentX , currentY , width , viewModel.ColumnsHeadersHeight[viewModel.ColumnsHeadersHeight.Length - 1] , screenScale );
                currentX += width;
            }

            /* Draw columns global headers */
            currentX = rowsHorizontalSpan;
            currentY = 0d;

            for ( int i = 0 ; i < colDepth - 1 ; i++ )
            {
                var lvl = i;
                var height = viewModel.ColumnsHeadersHeight[i];

                canvas.DrawGlobalHeader( viewModel ,
                    previousGlobalCoordinates ,
                    theme ,
                    vm => vm.ColumnsDefinitions.FlatList().Where( x => x.Level == lvl ) ,
                    ( hd , exp ) => hd.IsExpanded = exp ,
                    viewModel.ColumnsDefinitions.FlatList().Where( x => x.Level == lvl ).All( x => !x.IsExpanded ) ,
                    currentX , currentY , viewModel.RowsHeadersWidth[viewModel.RowsHeadersWidth.Length - 1] , height , screenScale );
                currentY += height;
            }

            canvas.DrawGlobalHeader( viewModel ,
                previousGlobalCoordinates ,
                theme ,
                vm => vm.ColumnsDefinitions.FlatList().Concat( vm.RowsDefinitions.FlatList() ) ,
                ( hd , b ) => hd.IsExpanded = b ,
                true ,
                currentX , currentY , viewModel.RowsHeadersWidth[viewModel.RowsHeadersWidth.Length - 1] , viewModel.ColumnsHeadersHeight[viewModel.ColumnsHeadersHeight.Length - 1] , screenScale ,
                GlobalHeader.ExpandAll );
        }

        private static void DrawGlobalHeader( this SKCanvas canvas ,
            HierarchyGridViewModel viewModel ,
            IList<(ElementCoordinates, Guid)> previousGlobalCoordinates ,
            SkiaTheme theme ,
            Func<HierarchyGridViewModel , IEnumerable<HierarchyDefinition>> selector ,
            Action<HierarchyDefinition , bool> action ,
            bool expanded ,
            double left ,
            double top ,
            double width ,
            double height ,
            double screenScale ,
            GlobalHeader globalHeader = GlobalHeader.Local )
        {
            var rect = SKRect.Create( (float) ( left * screenScale ) , (float) ( top * screenScale ) , (float) ( width * screenScale ) , (float) ( height * screenScale ) );
            var coordinates = new ElementCoordinates( left * screenScale , top * screenScale , ( left + width ) * screenScale , ( top + height ) * screenScale );

            var isHovered = previousGlobalCoordinates.Find( t => rect.IntersectsWith( t.Item1.ToRectangle() ) )
                .Some( t => t.Item2.Equals( viewModel.HoveredElementId ) )
                .None( () => false );

            using var paint = new SKPaint();
            paint.Style = SKPaintStyle.Fill;
            paint.Color = isHovered ? theme.HoverHeaderBackgroundColor : theme.HeaderBackgroundColor;
            canvas.DrawRect( rect , paint );

            paint.Style = SKPaintStyle.Stroke;
            paint.Color = theme.BorderColor;
            canvas.DrawRect( rect , paint );

            var decorator = GetGlobalHeaderDecorator( !expanded , left * screenScale , top * screenScale , globalHeader );
            paint.Color = isHovered ? theme.HoverHeaderForegroundColor : theme.HeaderForegroundColor;
            paint.Style = SKPaintStyle.StrokeAndFill;
            canvas.DrawPath( decorator , paint );

            var act = () =>
            {
                foreach ( var hd in selector( viewModel ) )
                    action( hd , expanded );
            };

            viewModel.GlobalHeadersCoordinates.Add( new( coordinates , Guid.NewGuid() , act ) );
        }

        internal static void DrawColumnHeaders( this SKCanvas canvas ,
            HierarchyGridViewModel viewModel ,
            SkiaTheme theme ,
            Func<HierarchyGridViewModel , HierarchyDefinition[]> selector ,
            float availableWidth ,
            ref int headerCount ,
            double screenScale = 1d )
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
                canvas.DrawColumnHeader( viewModel , theme , ref headerCount , ref currentPosition , column , hdef , width , screenScale );
                column++;
            }

            while ( column < hdefs.Length && currentPosition < availableWidth )
            {
                var hdef = hdefs[column];
                var width = viewModel.ColumnsWidths[column];

                canvas.DrawColumnHeader( viewModel , theme , ref headerCount , ref currentPosition , column , hdef , width , screenScale );
                column++;
            }
        }

        private static void DrawColumnHeader( this SKCanvas canvas , HierarchyGridViewModel viewModel , SkiaTheme theme , ref int headerCount , ref double currentPosition , int column , HierarchyDefinition hdef , double width , double screenScale )
        {
            var height = hdef.IsExpanded && hdef.HasChild ?
                                viewModel.ColumnsHeadersHeight[hdef.Level] :
                                Enumerable.Range( hdef.Level , viewModel.ColumnsHeadersHeight.Length - hdef.Level )
                                    .Select( x => viewModel.ColumnsHeadersHeight[x] ).Sum();

            var top = Enumerable.Range( 0 , hdef.Level )
                .Select( x => viewModel.ColumnsHeadersHeight[x] )
                .Sum();

            canvas.DrawHeader( viewModel , theme , ref headerCount , hdef , currentPosition , top , width , height , screenScale );

            canvas.DrawParentColumnHeader( viewModel , theme , hdef , hdef , column , currentPosition , ref headerCount , screenScale );
            currentPosition += width;
        }

        private static void DrawParentColumnHeader( this SKCanvas canvas , HierarchyGridViewModel viewModel , SkiaTheme theme , HierarchyDefinition src , HierarchyDefinition origin , int column , double currentPosition , ref int headerCount , double screenScale )
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

            canvas.DrawHeader( viewModel , theme , ref headerCount , hdef , currentPosition , top , width , height , screenScale );

            viewModel.ColumnsParents.Add( hdef );

            canvas.DrawParentColumnHeader( viewModel , theme , hdef , origin , column , currentPosition , ref headerCount , screenScale );
        }

        internal static void DrawRowHeaders( this SKCanvas canvas ,
            HierarchyGridViewModel viewModel ,
            SkiaTheme theme ,
            Func<HierarchyGridViewModel , HierarchyDefinition[]> selector ,
            float availableHeight ,
            ref int headerCount ,
            double screenScale = 1d )
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
                canvas.DrawRowHeader( viewModel , theme , ref headerCount , ref currentPosition , row , hdef , height , screenScale );
                row++;
            }

            while ( row < hdefs.Length && currentPosition < availableHeight )
            {
                var hdef = hdefs[row];
                var height = viewModel.RowsHeights[row];

                canvas.DrawRowHeader( viewModel , theme , ref headerCount , ref currentPosition , row , hdef , height , screenScale );
                row++;
            }
        }

        private static void DrawRowHeader( this SKCanvas canvas , HierarchyGridViewModel viewModel , SkiaTheme theme , ref int headerCount , ref double currentPosition , int row , HierarchyDefinition hdef , double height , double screenScale )
        {
            var width = hdef.IsExpanded && hdef.HasChild ?
                                viewModel.RowsHeadersWidth[hdef.Level] :
                                Enumerable.Range( hdef.Level , viewModel.RowsHeadersWidth.Length - hdef.Level )
                                    .Where( x => x < viewModel.RowsHeadersWidth.Length )
                                    .Select( x => viewModel.RowsHeadersWidth[x] ).Sum();

            var left = Enumerable.Range( 0 , hdef.Level )
                .Where( x => x < viewModel.RowsHeadersWidth.Length )
                .Select( x => viewModel.RowsHeadersWidth[x] ).Sum();

            canvas.DrawHeader( viewModel , theme , ref headerCount , hdef , left , currentPosition , width , height , screenScale );

            canvas.DrawParentRowHeader( viewModel , theme , hdef , hdef , row , currentPosition , ref headerCount , screenScale );

            currentPosition += height;
        }

        private static void DrawParentRowHeader( this SKCanvas canvas , HierarchyGridViewModel viewModel , SkiaTheme theme , HierarchyDefinition src , HierarchyDefinition origin , int row , double currentPosition , ref int headerCount , double screenScale )
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

            canvas.DrawHeader( viewModel , theme , ref headerCount , hdef , left , currentPosition , width , height , screenScale );

            viewModel.RowsParents.Add( hdef );

            canvas.DrawParentRowHeader( viewModel , theme , hdef , origin , row , currentPosition , ref headerCount , screenScale );
        }

        private static void DrawHeader( this SKCanvas canvas ,
            HierarchyGridViewModel viewModel ,
            SkiaTheme theme ,
            ref int headerCount ,
            HierarchyDefinition hdef ,
            double left ,
            double top ,
            double width ,
            double height ,
            double screenScale )
        {
            var rect = SKRect.Create( (float) ( left * screenScale ) , (float) ( top * screenScale ) , (float) ( width * screenScale ) , (float) ( height * screenScale ) );

            var renderInfo = RenderInfo.FindRender( viewModel , theme , hdef );

            using var paint = new SKPaint();
            paint.Style = SKPaintStyle.Fill;
            paint.Color = renderInfo.BackgroundColor;
            canvas.DrawRect( rect , paint );

            paint.Style = SKPaintStyle.Stroke;
            paint.Color = theme.BorderColor;
            canvas.DrawRect( rect , paint );

            GetHeaderDecorator( hdef , left * screenScale , top * screenScale )
                .IfSome( decorator =>
                {
                    paint.Color = renderInfo.ForegroundColor;
                    paint.Style = SKPaintStyle.StrokeAndFill;
                    canvas.DrawPath( decorator , paint );
                } );

            TextDrawer.Clear();
            TextDrawer.Alignment = TextAlignment.Left;
            TextDrawer.AddText( hdef.Content.ToString() , new Style { FontSize = TextSize , TextColor = renderInfo.ForegroundColor } );
            TextDrawer.MaxHeight = (float) ( ( height - 10 ) * screenScale );
            TextDrawer.MaxWidth = (float) ( ( width - 22 ) * screenScale );

            TextDrawer.Paint( canvas , new SKPoint( (float) ( ( left + 20 ) * screenScale ) , (float) ( ( top + 6 ) * screenScale ) ) , TextPaintOptions );
            viewModel.HeadersCoordinates.Add( new( new( left , top , left + width , top + height ) , hdef ) );

            headerCount++;
        }

        private static Option<SKPath> GetHeaderDecorator( HierarchyDefinition hdef , double left , double top )
        {
            if ( !hdef.HasChild )
                return Option<SKPath>.None;

            return hdef.IsExpanded ? BuildExpandedPath( left , top ) : BuildFoldedPath( left , top );
        }

        private static SKPath GetGlobalHeaderDecorator( bool isExpanded , double left , double top , GlobalHeader globalHeader )
            => globalHeader switch
            {
                GlobalHeader.ExpandAll => BuildExpandAllPath( left , top ),
                GlobalHeader.CollapseAll => BuildFoldAllPath( left , top ),
                _ => isExpanded ? BuildExpandedPath( left , top ) : BuildFoldedPath( left , top )
            };

        private static SKPath BuildFoldAllPath( double left , double top )
        {
            var path = new SKPath { FillType = SKPathFillType.EvenOdd };

            var startPoint = new SKPoint( 11f + (float) left , 5f + (float) top );
            path.MoveTo( startPoint );
            path.LineTo( startPoint.X , startPoint.Y + 7f );
            path.LineTo( startPoint.X - 8f , startPoint.Y + 7f );
            path.LineTo( startPoint );

            startPoint = new SKPoint( 3f + (float) left , 14f + (float) top );
            path.MoveTo( startPoint );
            path.LineTo( startPoint.X + 8f , startPoint.Y );
            path.LineTo( startPoint.X + 8f , startPoint.Y + 7f );
            path.LineTo( startPoint );

            startPoint = new SKPoint( 13f + (float) left , 5f + (float) top );
            path.MoveTo( startPoint );
            path.LineTo( startPoint.X , startPoint.Y + 7f );
            path.LineTo( startPoint.X + 7f , startPoint.Y + 7f );
            path.LineTo( startPoint );

            startPoint = new SKPoint( 13f + (float) left , 14f + (float) top );
            path.MoveTo( startPoint );
            path.LineTo( startPoint.X + 7f , startPoint.Y );
            path.LineTo( startPoint.X , startPoint.Y + 7f );
            path.LineTo( startPoint );

            return path;
        }

        private static SKPath BuildExpandAllPath( double left , double top )
        {
            var path = new SKPath { FillType = SKPathFillType.EvenOdd };

            var startPoint = new SKPoint( 3f + (float) left , 5f + (float) top );
            path.MoveTo( startPoint );
            path.LineTo( startPoint.X + 7f , startPoint.Y );
            path.LineTo( startPoint.X , startPoint.Y + 7f );
            path.LineTo( startPoint );

            startPoint = new SKPoint( 3f + (float) left , 14f + (float) top );
            path.MoveTo( startPoint );
            path.LineTo( startPoint.X , startPoint.Y + 7f );
            path.LineTo( startPoint.X + 7f , startPoint.Y + 7f );
            path.LineTo( startPoint );

            startPoint = new SKPoint( 12f + (float) left , 5f + (float) top );
            path.MoveTo( startPoint );
            path.LineTo( startPoint.X + 8f , startPoint.Y );
            path.LineTo( startPoint.X + 8f , startPoint.Y + 7f );
            path.LineTo( startPoint );

            startPoint = new SKPoint( 20f + (float) left , 14f + (float) top );
            path.MoveTo( startPoint );
            path.LineTo( startPoint.X , startPoint.Y + 7f );
            path.LineTo( startPoint.X - 8f , startPoint.Y + 7f );
            path.LineTo( startPoint );

            return path;
        }

        private static SKPath BuildFoldedPath( double left , double top )
        {
            var path = new SKPath { FillType = SKPathFillType.EvenOdd };
            var startPoint = new SKPoint( 3f + (float) left , 5f + (float) top );
            path.MoveTo( startPoint );
            path.LineTo( startPoint.X + 8f , startPoint.Y + 8f );
            path.LineTo( startPoint.X , startPoint.Y + 16f );
            path.Close();
            return path;
        }

        private static SKPath BuildExpandedPath( double left , double top )
        {
            var path = new SKPath { FillType = SKPathFillType.EvenOdd };
            var startPoint = new SKPoint( 3f + (float) left , 9f + (float) top );
            path.MoveTo( startPoint );
            path.LineTo( startPoint.X + 16f , startPoint.Y );
            path.LineTo( startPoint.X + 8f , startPoint.Y + 8f );
            path.Close();
            return path;
        }

        internal static void DrawCells( this SKCanvas canvas ,
            HierarchyGridViewModel viewModel ,
            SkiaTheme theme ,
            IEnumerable<PositionedCell> cells ,
            double screenScale = 1d )
        {
            foreach ( var cell in cells )
                canvas.DrawCell( viewModel , theme , cell , screenScale );
        }

        private static void DrawCell( this SKCanvas canvas ,
            HierarchyGridViewModel viewModel ,
            SkiaTheme theme ,
            PositionedCell cell ,
            double screenScale = 1d )
        {
            var rect = SKRect.Create( (float) ( cell.Left * screenScale ) ,
                (float) ( cell.Top * screenScale ) ,
                (float) ( cell.Width * screenScale ) ,
                (float) ( cell.Height * screenScale ) );

            var renderInfo = RenderInfo.FindRender( viewModel , theme , cell );

            using var paint = new SKPaint();
            paint.Style = SKPaintStyle.Fill;
            paint.Color = renderInfo.BackgroundColor;

            if ( cell.ResultSet.Qualifier == Qualification.Empty && !cell.HasSpecialRenderStatus( viewModel ) )
            {
                paint.Shader = SKShader.CreateLinearGradient(
                    new SKPoint( rect.Left , rect.Top ) ,
                    new SKPoint( rect.Right , rect.Bottom ) ,
                    new SKColor[] { renderInfo.BackgroundColor , renderInfo.ForegroundColor } ,
                    SKShaderTileMode.Repeat );
            }

            canvas.DrawRect( rect , paint );

            paint.Shader = null;
            paint.Style = SKPaintStyle.Stroke;
            paint.StrokeWidth = 1f;
            paint.Color = theme.BorderColor;
            canvas.DrawRect( rect , paint );

            /* Add extra render on focus cells */
            viewModel.FocusCells.Find( cell )
                .IfSome( fci =>
                {
                    var borderThickness = fci.BorderThickness;

                    /* Paint background */
                    paint.Style = SKPaintStyle.Fill;
                    paint.Color = fci.BackgroundColor.ToSKColor();

                    rect = SKRect.Create( (float) ( cell.Left * screenScale ) + borderThickness ,
                    (float) ( cell.Top * screenScale ) + borderThickness ,
                    (float) ( cell.Width * screenScale ) - ( borderThickness + 1f ) ,
                    (float) ( cell.Height * screenScale ) - ( borderThickness + 1f ) );
                    canvas.DrawRect( rect , paint );

                    /* Paint border */
                    paint.Style = SKPaintStyle.Stroke;
                    paint.Color = fci.BorderColor.ToSKColor();
                    paint.StrokeWidth = fci.BorderThickness;
                    canvas.DrawRect( rect , paint );
                } );

            if ( viewModel.Selections.Contains( cell ) )
            {
                paint.Color = theme.SelectionBorderColor;
                paint.StrokeWidth = theme.SelectionBorderThickness;

                rect = SKRect.Create( (float) ( cell.Left * screenScale ) + theme.SelectionBorderThickness ,
                    (float) ( cell.Top * screenScale ) + theme.SelectionBorderThickness ,
                    (float) ( cell.Width * screenScale ) - ( theme.SelectionBorderThickness + 1f ) ,
                    (float) ( cell.Height * screenScale ) - ( theme.SelectionBorderThickness + 1f ) );
                canvas.DrawRect( rect , paint );
            }

            float textHPadding = (float) ( ( 6f + theme.SelectionBorderThickness ) * screenScale );
            var textVPadding = (float) ( cell.Height - ( TextSize * screenScale ) );

            TextDrawer.Clear();
            TextDrawer.Alignment = viewModel.TextAlignment.ToRichTextKitTextAlignment();
            TextDrawer.AddText( cell.ResultSet.Result , new Style { FontSize = TextSize , TextColor = renderInfo.ForegroundColor } );
            TextDrawer.MaxHeight = (float) ( cell.Height * screenScale );
            TextDrawer.MaxWidth = (float) ( cell.Width * screenScale ) - textHPadding;

            TextDrawer.Paint( canvas , new SKPoint( (float) ( cell.Left * screenScale ) + ( textHPadding / 2 ) , (float) ( cell.Top * screenScale ) + ( textVPadding / 2 ) ) , TextPaintOptions );

            viewModel.CellsCoordinates.Add( (new( cell ), cell) );
        }

        private static TextBlock TextDrawer { get; } = new TextBlock();
        private static TextPaintOptions TextPaintOptions { get; } = new TextPaintOptions { Edging = SKFontEdging.Antialias };
        private const float TextSize = 14f;
    }
}