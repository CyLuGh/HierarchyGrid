using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using MoreLinq.Extensions;
using ReactiveUI;

namespace VirtualHierarchyGrid
{
    public partial class PloufHierarchyGridViewModel
    {
        public static double DEFAULT_HEADER_WIDTH => 80;
        public static double DEFAULT_HEADER_HEIGHT => 30;
        public static double DEFAULT_COLUMN_WIDTH => 120;
        public static double DEFAULT_ROW_HEIGHT => 30;

        public double[] RowsHeadersWidth { get; internal set; }
        public double[] ColumnsHeadersHeight { get; internal set; }

        internal Dictionary<int , double> ColumnsWidths { get; } = new Dictionary<int , double>();
        internal Dictionary<int , double> RowsHeights { get; } = new Dictionary<int , double>();

        [Reactive] public System.Windows.TextAlignment TextAlignment { get; set; }

        public void SetColumnsWidths( double width )
        {
            ColumnsWidths.ToArray()
                .ForEach( kvp => ColumnsWidths[kvp.Key] = width );

            Observable.Return( Unit.Default )
                .InvokeCommand( DrawGridCommand );

            Observable.Return( (HorizontalOffset, VerticalOffset, Width, Height, Scale, true) )
                .InvokeCommand( FindCellsToDrawCommand );
        }

        public void SetRowsHeights( double height )
        {
            RowsHeights.ToArray()
                .ForEach( kvp => RowsHeights[kvp.Key] = height );

            Observable.Return( Unit.Default )
                .InvokeCommand( DrawGridCommand );

            Observable.Return( (HorizontalOffset, VerticalOffset, Width, Height, Scale, true) )
                .InvokeCommand( FindCellsToDrawCommand );
        }
    }
}