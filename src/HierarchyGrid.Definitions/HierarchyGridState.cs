using LanguageExt;
using System;
using System.Linq;

namespace HierarchyGrid.Definitions;

public readonly record struct HierarchyGridState : IEquatable<HierarchyGridState>
{
    public static HierarchyGridState Default { get; } = new();

    /// <summary>
    /// Vertical scrollbar position
    /// </summary>
    public int VerticalOffset { get; init; }

    /// <summary>
    /// Horizontal scrollbar position
    /// </summary>
    public int HorizontalOffset { get; init; }

    /// <summary>
    /// Expanded state of each row
    /// </summary>
    public Arr<bool> RowToggles { get; init; }

    /// <summary>
    /// Expanded state of each column
    /// </summary>
    public Arr<bool> ColumnToggles { get; init; }

    /// <summary>
    /// Currently selected cells
    /// </summary>
    public Arr<PositionedCell> Selections { get; init; }

    public HierarchyGridState()
    {
        VerticalOffset = 0;
        HorizontalOffset = 0;
        RowToggles = Arr<bool>.Empty;
        ColumnToggles = Arr<bool>.Empty;
        Selections = Arr<PositionedCell>.Empty;
    }

    public HierarchyGridState( HierarchyGridViewModel viewModel )
    {
        VerticalOffset = viewModel.VerticalOffset;
        HorizontalOffset = viewModel.HorizontalOffset;
        RowToggles = viewModel.RowsDefinitions.FlatList().Select( x => x.IsExpanded ).ToArr();
        ColumnToggles = viewModel.ColumnsDefinitions.FlatList().Select( x => x.IsExpanded ).ToArr();
        Selections = viewModel.Selections.ToArr();
    }

    public HierarchyGridState( HierarchyGridState state )
    {
        VerticalOffset = state.VerticalOffset;
        HorizontalOffset = state.HorizontalOffset;
        RowToggles = state.RowToggles;
        ColumnToggles = state.ColumnToggles;
        Selections = state.Selections;
    }

    public bool IsDefault()
        => VerticalOffset == 0
           && HorizontalOffset == 0
           && RowToggles.IsEmpty
           && ColumnToggles.IsEmpty
           && Selections.IsEmpty;

    public bool Equals( HierarchyGridState other )
        => VerticalOffset == other.VerticalOffset
           && HorizontalOffset == other.HorizontalOffset
           && RowToggles.SequenceEqual( other.RowToggles )
           && ColumnToggles.SequenceEqual( other.ColumnToggles );

    public override int GetHashCode()
        => HashCode.Combine( VerticalOffset , HorizontalOffset , RowToggles , ColumnToggles );
}