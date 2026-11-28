using Avalonia;
using HierarchyGrid.Definitions;

namespace HierarchyGrid.Avalonia;

public partial class Grid
{
    public static readonly StyledProperty<string?> StatusMessageProperty =
        AvaloniaProperty.Register<Grid, string?>(nameof(StatusMessage), string.Empty);

    public string? StatusMessage
    {
        get => GetValue(StatusMessageProperty);
        set => SetValue(StatusMessageProperty, value);
    }

    public static readonly StyledProperty<double> DefaultColumnWidthProperty =
        AvaloniaProperty.Register<Grid, double>(
            nameof(DefaultColumnWidth),
            HierarchyGridViewModel.DEFAULT_COLUMN_WIDTH
        );

    public double DefaultColumnWidth
    {
        get => GetValue(DefaultColumnWidthProperty);
        set => SetValue(DefaultColumnWidthProperty, value);
    }

    public static readonly StyledProperty<double> DefaultRowHeightProperty =
        AvaloniaProperty.Register<Grid, double>(
            nameof(DefaultRowHeight),
            HierarchyGridViewModel.DEFAULT_ROW_HEIGHT
        );

    public double DefaultRowHeight
    {
        get => GetValue(DefaultRowHeightProperty);
        set => SetValue(DefaultRowHeightProperty, value);
    }

    public static readonly StyledProperty<double> DefaultHeaderWidthProperty =
        AvaloniaProperty.Register<Grid, double>(
            nameof(DefaultHeaderWidth),
            HierarchyGridViewModel.DEFAULT_HEADER_WIDTH
        );

    public double DefaultHeaderWidth
    {
        get => GetValue(DefaultHeaderWidthProperty);
        set => SetValue(DefaultHeaderWidthProperty, value);
    }

    public static readonly StyledProperty<double> DefaultHeaderHeightProperty =
        AvaloniaProperty.Register<Grid, double>(
            nameof(DefaultHeaderHeight),
            HierarchyGridViewModel.DEFAULT_HEADER_HEIGHT
        );

    public double DefaultHeaderHeight
    {
        get => GetValue(DefaultHeaderHeightProperty);
        set => SetValue(DefaultHeaderHeightProperty, value);
    }

    public static readonly StyledProperty<bool> EnableCrosshairProperty = AvaloniaProperty.Register<
        Grid,
        bool
    >(nameof(EnableCrosshair), false);

    public bool EnableCrosshair
    {
        get => GetValue(EnableCrosshairProperty);
        set => SetValue(EnableCrosshairProperty, value);
    }

    public static readonly StyledProperty<float> CellFontSizeProperty = AvaloniaProperty.Register<
        Grid,
        float
    >(nameof(CellFontSize), HierarchyGridViewModel.DEFAULT_FONT_SIZE);

    public float CellFontSize
    {
        get => GetValue(CellFontSizeProperty);
        set => SetValue(CellFontSizeProperty, value);
    }

    public static readonly StyledProperty<float> HeaderFontSizeProperty = AvaloniaProperty.Register<
        Grid,
        float
    >(nameof(HeaderFontSize), HierarchyGridViewModel.DEFAULT_FONT_SIZE);

    public float HeaderFontSize
    {
        get => GetValue(HeaderFontSizeProperty);
        set => SetValue(HeaderFontSizeProperty, value);
    }

    public static readonly StyledProperty<string> CellFontFamilyProperty =
        AvaloniaProperty.Register<Grid, string>(nameof(CellFontFamily));

    public string CellFontFamily
    {
        get => GetValue(CellFontFamilyProperty);
        set => SetValue(CellFontFamilyProperty, value);
    }

    public static readonly StyledProperty<string> HeaderFontFamilyProperty =
        AvaloniaProperty.Register<Grid, string>(nameof(HeaderFontFamily));

    public string HeaderFontFamily
    {
        get => GetValue(HeaderFontFamilyProperty);
        set => SetValue(HeaderFontFamilyProperty, value);
    }
}
