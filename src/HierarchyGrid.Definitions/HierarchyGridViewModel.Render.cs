using ReactiveUI;
using System.Collections.Generic;
using System.Reactive.Linq;

namespace HierarchyGrid.Definitions
{
    public partial class HierarchyGridViewModel
    {
        private static double DEFAULT_HEADER_WIDTH => 80;
        private static double DEFAULT_HEADER_HEIGHT => 30;
        private static double DEFAULT_COLUMN_WIDTH => 120;
        private static double DEFAULT_ROW_HEIGHT => 30;

        public double DefaultHeaderWidth { get; set; } = DEFAULT_HEADER_WIDTH;
        public double DefaultHeaderHeight { get; set; } = DEFAULT_HEADER_HEIGHT;
        public double DefaultColumnWidth { get; set; } = DEFAULT_COLUMN_WIDTH;
        public double DefaultRowHeight { get; set; } = DEFAULT_ROW_HEIGHT;

        public double[] RowsHeadersWidth { get; internal set; }
        public double[] ColumnsHeadersHeight { get; internal set; }

        public Dictionary<int , double> ColumnsWidths { get; } = new Dictionary<int , double>();
        public Dictionary<int , double> RowsHeights { get; } = new Dictionary<int , double>();

        public List<HierarchyDefinition> ColumnsParents { get; } = new();
        public List<HierarchyDefinition> RowsParents { get; } = new();

        //[Reactive] public System.Windows.TextAlignment TextAlignment { get; set; }

        public void SetColumnsWidths( double width )
        {
            foreach ( var kvp in ColumnsWidths )
                ColumnsWidths[kvp.Key] = width;

            Observable.Return( false )
                .InvokeCommand( DrawGridCommand );

            //Observable.Return( (HorizontalOffset, VerticalOffset, Width, Height, Scale, true) )
            //    .InvokeCommand( FindCellsToDrawCommand );
        }

        public void SetRowsHeights( double height )
        {
            foreach ( var kvp in RowsHeights )
                RowsHeights[kvp.Key] = height;

            Observable.Return( false )
                .InvokeCommand( DrawGridCommand );

            //Observable.Return( (HorizontalOffset, VerticalOffset, Width, Height, Scale, true) )
            //    .InvokeCommand( FindCellsToDrawCommand );
        }
    }
}