using System;
using System.Linq;

namespace HierarchyGrid.Definitions
{
    public struct HierarchyGridState
    {
        /// <summary>
        /// Vertical scrollbar position
        /// </summary>
        public int VerticalOffset { get; init; }

        /// <summary>
        /// Horizontal scrollbar position
        /// </summary>
        public int HorizontalOffset { get; init; }


        /// <summary>
        /// Expand state of each row
        /// </summary>
        public bool[] RowToggles { get; init; }

        /// <summary>
        /// Expand state of each column
        /// </summary>
        public bool[] ColumnToggles { get; init; }

        public HierarchyGridState()
        {
            VerticalOffset = 0;
            HorizontalOffset = 0;
            RowToggles = Array.Empty<bool>();
            ColumnToggles = Array.Empty<bool>();
        }

        public HierarchyGridState( HierarchyGridViewModel viewModel)
        {
            VerticalOffset = viewModel.VerticalOffset;
            HorizontalOffset = viewModel.HorizontalOffset;
            RowToggles = viewModel.RowsDefinitions.FlatList().Select( x => x.IsExpanded ).ToArray();
            ColumnToggles = viewModel.ColumnsDefinitions.FlatList().Select( x => x.IsExpanded ).ToArray();
        }
    }
}
