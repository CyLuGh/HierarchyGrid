namespace HierarchyGrid.Definitions
{
    public class HierarchyGridState
    {
        /// <summary>
        /// Vertical scrollbar position
        /// </summary>
        public int VerticalOffset { get; set; }

        /// <summary>
        /// Horizontal scrollbar position
        /// </summary>
        public int HorizontalOffset { get; set; }


        /// <summary>
        /// Expand state of each row
        /// </summary>
        public bool[] RowToggles { get; set; }

        /// <summary>
        /// Expand state of each column
        /// </summary>
        public bool[] ColumnToggles { get; set; }
    }
}
