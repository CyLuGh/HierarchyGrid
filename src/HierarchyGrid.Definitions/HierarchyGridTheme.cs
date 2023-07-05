namespace HierarchyGrid.Definitions;

public sealed class HierarchyGridTheme : ITheme
{
    public static HierarchyGridTheme Default { get; } = new HierarchyGridTheme();

    private HierarchyGridTheme()
    {
    }

    public ThemeColor BackgroundColor => ThemeColors.White;

    public ThemeColor ForegroundColor => ThemeColors.Black;

    public ThemeColor BorderColor => ThemeColors.SlateGray;

    public ThemeColor SelectionBorderColor => ThemeColors.Black;
    
    public float SelectionBorderThickness => 1f;

    public ThemeColor HeaderBackgroundColor => ThemeColors.LightGray;

    public ThemeColor HeaderForegroundColor => ThemeColors.Black;

    public ThemeColor HoverBackgroundColor => ThemeColors.LightSeaGreen;

    public ThemeColor HoverForegroundColor => ThemeColors.Black;

    public ThemeColor HoverHeaderBackgroundColor => ThemeColors.SeaGreen;

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

    public ThemeColor EmptyBackgroundColor => ThemeColors.Gainsboro;
}