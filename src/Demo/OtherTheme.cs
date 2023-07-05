using HierarchyGrid.Definitions;
using System.Windows.Media;

namespace Demo
{
    internal class OtherTheme : ITheme
    {
        public ThemeColor BackgroundColor => new( 255 , 10 , 10 , 10 );

        public ThemeColor ForegroundColor => new( 255 , 240 , 240 , 240 );

        public ThemeColor BorderColor => ThemeColors.SlateGray;

        public ThemeColor SelectionBorderColor => ThemeColors.White;

        public float SelectionBorderThickness => 2f;

        public ThemeColor HeaderBackgroundColor => ThemeColors.LightGray;

        public ThemeColor HeaderForegroundColor => ThemeColors.Black;

        public ThemeColor HoverBackgroundColor => new( Colors.PaleGoldenrod.ToString() );

        public ThemeColor HoverForegroundColor => ThemeColors.Black;

        public ThemeColor HoverHeaderBackgroundColor => new( Colors.Goldenrod.ToString() );

        public ThemeColor HoverHeaderForegroundColor => ThemeColors.White;

        public ThemeColor HighlightBackgroundColor => ThemeColors.LightBlue;

        public ThemeColor HighlightForegroundColor => ThemeColors.Black;

        public ThemeColor HighlightHeaderBackgroundColor => ThemeColors.LightBlue;

        public ThemeColor HighlightHeaderForegroundColor => ThemeColors.Black;

        public ThemeColor ReadOnlyBackgroundColor => ThemeColors.LightGray;

        public ThemeColor ReadOnlyForegroundColor => ThemeColors.DarkSlateGray;

        public ThemeColor ComputedBackgroundColor => ThemeColors.LightGray;

        public ThemeColor ComputedForegroundColor => ThemeColors.Blue;

        public ThemeColor RemarkBackgroundColor => ThemeColors.GreenYellow;

        public ThemeColor RemarkForegroundColor => ThemeColors.Black;

        public ThemeColor WarningBackgroundColor => ThemeColors.YellowGreen;

        public ThemeColor WarningForegroundColor => ThemeColors.Black;

        public ThemeColor ErrorBackgroundColor => ThemeColors.IndianRed;

        public ThemeColor ErrorForegroundColor => ThemeColors.White;

        public ThemeColor EmptyBackgroundColor => ThemeColors.LightGray;
    }
}