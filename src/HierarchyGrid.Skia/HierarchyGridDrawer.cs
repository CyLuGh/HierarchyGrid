using HierarchyGrid.Definitions;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HierarchyGrid.Skia
{
    public static class HierarchyGridDrawer
    {
        public static void Draw( HierarchyGridViewModel viewModel , SKCanvas canvas , float width , float height )
        {
            canvas.Clear();

            if ( viewModel.HasData )
            {
                int headerCount = 0;

                viewModel.ClearCoordinates();

                canvas.DrawCells( viewModel , viewModel.DrawnCells( width , height , false ) );
                canvas.DrawColumnHeaders( viewModel , v => v.ColumnsDefinitions.Leaves().ToArray() , width , ref headerCount );
                canvas.DrawRowHeaders( viewModel , v => v.RowsDefinitions.Leaves().ToArray() , height , ref headerCount );
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