using LanguageExt;
using System;
using System.Linq;

namespace HierarchyGrid.Definitions
{
    public struct HierarchyGridState : IEquatable<HierarchyGridState>
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

        public HierarchyGridState()
        {
            VerticalOffset = 0;
            HorizontalOffset = 0;
            RowToggles = Arr<bool>.Empty;
            ColumnToggles = Arr<bool>.Empty;
        }

        public HierarchyGridState( HierarchyGridViewModel viewModel )
        {
            VerticalOffset = viewModel.VerticalOffset;
            HorizontalOffset = viewModel.HorizontalOffset;
            RowToggles = viewModel.RowsDefinitions.FlatList().Select( x => x.IsExpanded ).ToArr();
            ColumnToggles = viewModel.ColumnsDefinitions.FlatList().Select( x => x.IsExpanded ).ToArr();
        }

        public bool IsDefault()
            => VerticalOffset == 0
                && HorizontalOffset == 0
                && RowToggles.Length == 0
                && ColumnToggles.Length == 0;

        public bool Equals( HierarchyGridState other )
        {
            return VerticalOffset == other.VerticalOffset
                && HorizontalOffset == other.HorizontalOffset
                && RowToggles.SequenceEqual( other.RowToggles )
                && ColumnToggles.SequenceEqual( other.ColumnToggles );
        }

        public override bool Equals( object obj )
            => obj is HierarchyGridState hierarchyGridState && Equals( hierarchyGridState );

        public override int GetHashCode()
            => HashCode.Combine( VerticalOffset , HorizontalOffset , RowToggles , ColumnToggles );

        public static bool operator ==( HierarchyGridState a , HierarchyGridState b )
            => a.Equals( b );

        public static bool operator !=( HierarchyGridState a , HierarchyGridState b )
            => !a.Equals( b );
    }
}