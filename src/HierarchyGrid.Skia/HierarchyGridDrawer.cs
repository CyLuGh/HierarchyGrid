using HierarchyGrid.Definitions;
using SkiaSharp;
using System.Reactive.Linq;

namespace HierarchyGrid.Skia
{
    public static class HierarchyGridDrawer
    {
        //TODO Check invalidate

        public static async Task Draw( HierarchyGridViewModel viewModel ,
            SKCanvas canvas ,
            float width ,
            float height ,
            double screenScale = 1d ,
            bool invalidate = false )
        {
            canvas.Clear();
            var theme = new SkiaTheme( viewModel.Theme );

            using var paintBackground = new SKPaint();
            paintBackground.Color = theme.BackgroundColor;
            paintBackground.Style = SKPaintStyle.StrokeAndFill;
            var rectBackground = SKRect.Create( width , height );
            canvas.DrawRect( rectBackground , paintBackground );
        
            if ( viewModel.HasData )
            {
                int headerCount = 0;
                var previousGlobalCoordinates = viewModel.GlobalHeadersCoordinates
                    .Select( t => (t.Coord, t.Guid) )
                    .ToList();
                viewModel.ClearCoordinates();

                canvas.DrawGlobalHeaders( viewModel , theme , previousGlobalCoordinates , screenScale );
                canvas.DrawCells( viewModel , theme , viewModel.GetDrawnCells( width , height , invalidate ) , screenScale );
                canvas.DrawColumnHeaders( viewModel , theme , v => v.ColumnsDefinitions.Leaves().ToArray() , width , ref headerCount , screenScale );
                canvas.DrawRowHeaders( viewModel , theme , v => v.RowsDefinitions.Leaves().ToArray() , height , ref headerCount , screenScale );
            }
            else
            {
                using var paint = new SKPaint();
                paint.TextSize = 64f;
                paint.IsAntialias = true;
                paint.Color = theme.ForegroundColor;
                paint.TextAlign = SKTextAlign.Center;

                canvas.DrawText( viewModel.StatusMessage ?? "NO MESSAGE" , width / 2 , height / 2 , paint );
            }

            canvas.Flush();

            // Draw textbox
            await viewModel.DrawEditionTextBoxInteraction.Handle( viewModel.DrawnCells );
        }
    }
}