using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.Text;

namespace VirtualHierarchyGrid
{
    partial class HierarchyGridViewModel
    {
        public static double DEFAULT_HEADER_WIDTH => 80;
        public static double DEFAULT_HEADER_HEIGHT => 30;
        public static double DEFAULT_COLUMN_WIDTH => 120;
        public static double DEFAULT_ROW_HEIGHT => 30;

        public double[] RowsHeadersWidth { get; internal set; }
        public double[] ColumnsHeadersHeight { get; internal set; }

        public Dictionary<int, double> ColumnsWidths { get; } = new Dictionary<int, double>();
        public Dictionary<int, double> RowsHeights { get; } = new Dictionary<int, double>();

        [Reactive] public System.Windows.TextAlignment TextAlignment { get; set; }
    }
}