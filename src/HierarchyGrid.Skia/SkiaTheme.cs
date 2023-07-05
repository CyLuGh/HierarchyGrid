using HierarchyGrid.Definitions;
using SkiaSharp;

namespace HierarchyGrid.Skia
{
    internal record SkiaTheme
    {
        public SkiaTheme( ITheme theme )
        {
            BackgroundColor = theme.BackgroundColor.ToSKColor();
            ForegroundColor = theme.ForegroundColor.ToSKColor();
            BorderColor = theme.BorderColor.ToSKColor();
            
            SelectionBorderColor = theme.SelectionBorderColor.ToSKColor();
            SelectionBorderThickness = theme.SelectionBorderThickness;

            HeaderBackgroundColor = theme.HeaderBackgroundColor.ToSKColor();
            HeaderForegroundColor = theme.HeaderForegroundColor.ToSKColor();

            HoverBackgroundColor = theme.HoverBackgroundColor.ToSKColor();
            HoverForegroundColor = theme.HoverForegroundColor.ToSKColor();
            HoverHeaderBackgroundColor = theme.HoverHeaderBackgroundColor.ToSKColor();
            HoverHeaderForegroundColor = theme.HoverHeaderForegroundColor.ToSKColor();

            HighlightBackgroundColor = theme.HighlightBackgroundColor.ToSKColor();
            HighlightForegroundColor = theme.HighlightForegroundColor.ToSKColor();
            HighlightHeaderBackgroundColor = theme.HighlightHeaderBackgroundColor.ToSKColor();
            HighlightHeaderForegroundColor = theme.HighlightHeaderForegroundColor.ToSKColor();

            ReadOnlyBackgroundColor = theme.ReadOnlyBackgroundColor.ToSKColor();
            ReadOnlyForegroundColor = theme.ReadOnlyForegroundColor.ToSKColor();

            ComputedBackgroundColor = theme.ComputedBackgroundColor.ToSKColor();
            ComputedForegroundColor = theme.ComputedForegroundColor.ToSKColor();

            RemarkBackgroundColor = theme.RemarkBackgroundColor.ToSKColor();
            RemarkForegroundColor = theme.RemarkForegroundColor.ToSKColor();

            WarningBackgroundColor = theme.WarningBackgroundColor.ToSKColor();
            WarningForegroundColor = theme.WarningForegroundColor.ToSKColor();

            ErrorBackgroundColor = theme.ErrorBackgroundColor.ToSKColor();
            ErrorForegroundColor = theme.ErrorForegroundColor.ToSKColor();

            EmptyBackgroundColor = theme.EmptyBackgroundColor.ToSKColor();
            EmptyForegroundColor = theme.BorderColor.With( a: 100 ).ToSKColor();
        }

        public SKColor BackgroundColor { get; init; }
        public SKColor ForegroundColor { get; init; }
        public SKColor BorderColor { get; init; }

        public SKColor SelectionBorderColor { get; init; }
        public float SelectionBorderThickness { get; init; }

        public SKColor HeaderBackgroundColor { get; init; }
        public SKColor HeaderForegroundColor { get; init; }

        public SKColor HoverBackgroundColor { get; init; }
        public SKColor HoverForegroundColor { get; init; }
        public SKColor HoverHeaderBackgroundColor { get; init; }
        public SKColor HoverHeaderForegroundColor { get; init; }

        public SKColor HighlightBackgroundColor { get; init; }
        public SKColor HighlightForegroundColor { get; init; }
        public SKColor HighlightHeaderBackgroundColor { get; init; }
        public SKColor HighlightHeaderForegroundColor { get; init; }

        public SKColor ReadOnlyBackgroundColor { get; init; }
        public SKColor ReadOnlyForegroundColor { get; init; }

        public SKColor ComputedBackgroundColor { get; init; }
        public SKColor ComputedForegroundColor { get; init; }

        public SKColor RemarkBackgroundColor { get; init; }
        public SKColor RemarkForegroundColor { get; init; }

        public SKColor WarningBackgroundColor { get; init; }
        public SKColor WarningForegroundColor { get; init; }

        public SKColor ErrorBackgroundColor { get; init; }
        public SKColor ErrorForegroundColor { get; init; }

        public SKColor EmptyBackgroundColor { get; init; }
        public SKColor EmptyForegroundColor { get; init; }
    }

    internal static class ThemeColorExtension
    {
        public static SKColor ToSKColor( this ThemeColor tColor )
            => new( tColor.R , tColor.G , tColor.B , tColor.A );
    }
}