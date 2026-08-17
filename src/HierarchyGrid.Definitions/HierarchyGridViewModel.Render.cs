using System.Collections.Generic;
using ReactiveUI;
using ReactiveUI.Primitives.Signals;

namespace HierarchyGrid.Definitions
{
    public partial class HierarchyGridViewModel
    {
        private static readonly double DEFAULT_HEADER_HEIGHT = 30;
        private static readonly double DEFAULT_COLUMN_WIDTH = 120;
        private static readonly double DEFAULT_ROW_HEIGHT = 30;
        private static readonly double DEFAULT_HEADER_WIDTH = 80;
        private static readonly float DEFAULT_FONT_SIZE = 15;

        public double DefaultHeaderWidth { get; set; } = DEFAULT_HEADER_WIDTH;
        public double DefaultHeaderHeight { get; set; } = DEFAULT_HEADER_HEIGHT;
        public double DefaultColumnWidth { get; set; } = DEFAULT_COLUMN_WIDTH;
        public double DefaultRowHeight { get; set; } = DEFAULT_ROW_HEIGHT;
        public float DefaultFontSize { get; set; } = DEFAULT_FONT_SIZE;

        public float CellFontSize { get; set; }
        public float HeaderFontSize { get; set; }
        public string? CellFontFamily { get; set; }
        public string? HeaderFontFamily { get; set; }

        public double[] RowsHeadersWidth { get; private set; }
        public double[] ColumnsHeadersHeight { get; private set; }

        public Dictionary<int, double> ColumnsWidths { get; } = [];
        public Dictionary<int, double> RowsHeights { get; } = [];

        public List<HierarchyDefinition> ColumnsParents { get; } = [];
        public List<HierarchyDefinition> RowsParents { get; } = [];

        public void SetColumnsWidths(double width)
        {
            foreach (var kvp in ColumnsWidths)
                ColumnsWidths[kvp.Key] = width;

            Signal.Return(false).InvokeCommand(DrawGridCommand);
        }

        public void SetRowsHeights(double height)
        {
            foreach (var kvp in RowsHeights)
                RowsHeights[kvp.Key] = height;

            Signal.Return(false).InvokeCommand(DrawGridCommand);
        }

        public void SetFontSize(float fontSize)
        {
            CellFontSize = fontSize;

            Signal.Return(false).InvokeCommand(DrawGridCommand);
        }

        public void SetHeaderFontSize(float fontSize)
        {
            HeaderFontSize = fontSize;

            Signal.Return(false).InvokeCommand(DrawGridCommand);
        }
    }
}
