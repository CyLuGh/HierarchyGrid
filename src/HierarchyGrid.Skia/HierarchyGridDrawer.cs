using HierarchyGrid.Definitions;
using ReactiveUI;
using ReactiveUI.Primitives.Signals;
using SkiaSharp;
using Topten.RichTextKit;

namespace HierarchyGrid.Skia
{
    public static class HierarchyGridDrawer
    {
        //TODO Check invalidate

        public static void Draw(
            HierarchyGridViewModel viewModel,
            SKCanvas canvas,
            float width,
            float height,
            double screenScale,
            bool invalidate = false
        )
        {
            canvas.Clear();
            var theme = new SkiaTheme(viewModel.Theme);

            using var paintBackground = new SKPaint();
            paintBackground.Color = theme.BackgroundColor;
            paintBackground.Style = SKPaintStyle.StrokeAndFill;
            var rectBackground = SKRect.Create(width, height); // TODO: check scale
            canvas.DrawRect(rectBackground, paintBackground);

            if (!viewModel.IsEmpty())
            {
                int headerCount = 0;
                var previousGlobalCoordinates = viewModel
                    .GlobalHeadersCoordinates.Select(t => (t.Coord, t.Guid))
                    .ToList();

                viewModel.ClearCoordinates();

                canvas.DrawGlobalHeaders(viewModel, theme, previousGlobalCoordinates, screenScale);

                canvas.DrawCells(
                    viewModel,
                    theme,
                    viewModel.GetDrawnCells(width, height, invalidate, screenScale)
                );

                canvas.DrawColumnHeaders(
                    viewModel,
                    theme,
                    v => [.. v.ColumnsDefinitions.Leaves()],
                    width,
                    ref headerCount,
                    screenScale
                );

                canvas.DrawRowHeaders(
                    viewModel,
                    theme,
                    v => [.. v.RowsDefinitions.Leaves()],
                    height,
                    ref headerCount,
                    screenScale
                );
            }
            else
            {
                var resultingScale = screenScale * viewModel.Scale;

                TextBlock textDrawer = new();
                textDrawer.AddText(
                    viewModel.StatusMessage ?? "NO MESSAGE",
                    new Style()
                    {
                        FontSize = (float)(64f * screenScale),
                        TextColor = theme.ForegroundColor,
                        FontFamily = !string.IsNullOrEmpty(viewModel.CellFontFamily)
                            ? viewModel.CellFontFamily
                            : "Monospace",
                    }
                );

                textDrawer.Paint(
                    canvas,
                    new SKPoint(
                        (float)(resultingScale * width - textDrawer.MeasuredWidth) / 2,
                        (float)(resultingScale * height - textDrawer.MeasuredHeight) / 2
                    ),
                    new TextPaintOptions { Edging = SKFontEdging.SubpixelAntialias }
                );
            }

            canvas.Flush();

            // Draw textbox
            Signal
                .Return(viewModel.DrawnCells)
                .InvokeCommand(viewModel, x => x.DrawEditionTextBox);
        }
    }
}
