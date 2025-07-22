using Avalonia;

namespace HierarchyGrid.Avalonia;

public partial class Grid
{
    public static readonly StyledProperty<string?> StatusMessageProperty =
        AvaloniaProperty.Register<Grid, string?>(nameof(StatusMessage), "No message");

    public string? StatusMessage
    {
        get => GetValue(StatusMessageProperty);
        set => SetValue(StatusMessageProperty, value);
    }

    public static readonly StyledProperty<int> DefaultColumnWidthProperty =
        AvaloniaProperty.Register<Grid, int>(nameof(DefaultColumnWidth), 120);

    public int DefaultColumnWidth
    {
        get => GetValue(DefaultColumnWidthProperty);
        set => SetValue(DefaultColumnWidthProperty, value);
    }

    public static readonly StyledProperty<int> DefaultRowHeightProperty = AvaloniaProperty.Register<
        Grid,
        int
    >(nameof(DefaultRowHeight), 30);

    public int DefaultRowHeight
    {
        get => GetValue(DefaultRowHeightProperty);
        set => SetValue(DefaultRowHeightProperty, value);
    }

    public static readonly StyledProperty<int> DefaultHeaderWidthProperty =
        AvaloniaProperty.Register<Grid, int>(nameof(DefaultHeaderWidth), 80);

    public int DefaultHeaderWidth
    {
        get => GetValue(DefaultHeaderWidthProperty);
        set => SetValue(DefaultHeaderWidthProperty, value);
    }

    public static readonly StyledProperty<int> DefaultHeaderHeightProperty =
        AvaloniaProperty.Register<Grid, int>(nameof(DefaultHeaderHeight), 30);

    public int DefaultHeaderHeight
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
    >(nameof(CellFontSize), 15);

    public float CellFontSize
    {
        get => GetValue(CellFontSizeProperty);
        set => SetValue(CellFontSizeProperty, value);
    }

    public static readonly StyledProperty<float> HeaderFontSizeProperty = AvaloniaProperty.Register<
        Grid,
        float
    >(nameof(HeaderFontSize), 15);

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
