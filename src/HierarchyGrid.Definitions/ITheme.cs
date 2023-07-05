namespace HierarchyGrid.Definitions;

public interface ITheme
{
    public ThemeColor BackgroundColor { get; }
    public ThemeColor ForegroundColor { get; }
    public ThemeColor BorderColor { get; }
    
    public ThemeColor SelectionBorderColor { get; }
    public float SelectionBorderThickness { get; }

    public ThemeColor HeaderBackgroundColor { get; }
    public ThemeColor HeaderForegroundColor { get; }

    public ThemeColor HoverBackgroundColor { get; }
    public ThemeColor HoverForegroundColor { get; }
    public ThemeColor HoverHeaderBackgroundColor { get; }
    public ThemeColor HoverHeaderForegroundColor { get; }

    public ThemeColor HighlightBackgroundColor { get; }
    public ThemeColor HighlightForegroundColor { get; }
    public ThemeColor HighlightHeaderBackgroundColor { get; }
    public ThemeColor HighlightHeaderForegroundColor { get; }

    public ThemeColor ReadOnlyBackgroundColor { get; }
    public ThemeColor ReadOnlyForegroundColor { get; }

    public ThemeColor ComputedBackgroundColor { get; }
    public ThemeColor ComputedForegroundColor { get; }

    public ThemeColor RemarkBackgroundColor { get; }
    public ThemeColor RemarkForegroundColor { get; }

    public ThemeColor WarningBackgroundColor { get; }
    public ThemeColor WarningForegroundColor { get; }

    public ThemeColor ErrorBackgroundColor { get; }
    public ThemeColor ErrorForegroundColor { get; }

    public ThemeColor EmptyBackgroundColor { get; }
}