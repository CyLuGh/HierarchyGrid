namespace HierarchyGrid.Definitions;

public readonly struct FocusCellInfo
{
    public ThemeColor BackgroundColor { get; init; }
    public ThemeColor BorderColor { get; init; }
    public float BorderThickness { get; init; }
    public string TooltipInfo { get; init; }
}