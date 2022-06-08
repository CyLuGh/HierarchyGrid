using HierarchyGrid.Definitions;
using SkiaSharp;

namespace HierarchyGrid.Skia
{
    public static class HierarchyGridDrawer
    {
        public static void Draw( HierarchyGridViewModel viewModel , SKCanvas canvas , float width , float height , bool invalidate )
        {
            canvas.Clear();

            if ( viewModel.HasData )
            {
                int headerCount = 0;

                viewModel.ClearCoordinates();

                var theme = new SkiaTheme( viewModel.Theme );

                canvas.DrawGlobalHeaders( viewModel , theme );
                canvas.DrawCells( viewModel , theme , viewModel.DrawnCells( width , height , invalidate ) );
                canvas.DrawColumnHeaders( viewModel , theme , v => v.ColumnsDefinitions.Leaves().ToArray() , width , ref headerCount );
                canvas.DrawRowHeaders( viewModel , theme , v => v.RowsDefinitions.Leaves().ToArray() , height , ref headerCount );
            }
            else
            {
                using var paint = new SKPaint();
                paint.TextSize = 64f;
                paint.IsAntialias = true;
                paint.Color = SKColors.DarkSlateGray;
                paint.TextAlign = SKTextAlign.Center;

                canvas.DrawText( viewModel.StatusMessage ?? "NO MESSAGE" , width / 2 , height / 2 , paint );
            }
        }
    }
}