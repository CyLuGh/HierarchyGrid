using HierarchyGrid.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace HierarchyGrid
{
    partial class VirtualHierarchyGrid
    {
        private double ScaleFactor => ViewModel?.ScaleFactor ?? 1d;

        private double AvailableRowSpace
        {
            get
            {
                var columnsElements = ViewModel?.ColumnsElements;

                // TODO include find mode
                double height = (ScrollGrid.ActualHeight - ScrollGrid.RowDefinitions[1].ActualHeight) * 1 / ScaleFactor - (columnsElements != null ? columnsElements.TotalDepth(false) * 30 : VGrid.RowDefinitions[0].ActualHeight) /*- ( FindMode ? 80 : 0 )*/;
                if (columnsElements != null && columnsElements.TotalDepth(false) != columnsElements.TotalDepth())
                    height -= 30;
                return height;
            }
        }

        private double AvailableColumnSpace => ScrollGrid.ActualWidth * 1 / ScaleFactor - VGrid.ColumnDefinitions[0].Width.Value;

        public int FixedColumns => ViewModel?.ColumnsElements.Where(x => x.Parent == null && x.Frozen).Sum(x => x.Count()) ?? 0;

        public int FixedRows => ViewModel?.RowsElements.Where(x => x.Parent == null && x.Frozen).Sum(x => x.Count()) ?? 0;

        /// <summary>
        /// Creates a column definition and configures its size properties.
        /// </summary>
        /// <param name="idx"></param>
        /// <param name="leaves"></param>
        /// <returns></returns>
        private ColumnDefinition CreateColumnDefinition(int idx, List<HierarchyDefinition> leaves, Grid parent)
        {
            ColumnDefinition cDef = new ColumnDefinition();

            if (idx < leaves.Count - 1)
            { /* Every column except the last one */
                cDef.Width = new GridLength(leaves[idx].Size);
                cDef.SharedSizeGroup = "C" + idx;
            }
            else /* Last column fills the gap */
                cDef.Width = new GridLength(1, GridUnitType.Star);

            parent.ColumnDefinitions.Add(cDef);

            return cDef;
        }

        private GridSplitter CreateColumnSplitter(ToggleButton header, HierarchyDefinition def, int pos, int depth, Grid parent)
        {
            var splitter = new GridSplitter()
            {
                Width = 3,
                //Style = (Style)FindResource("VirtualGridSplitterStyle"),
                ResizeDirection = GridResizeDirection.Columns,
                ResizeBehavior = GridResizeBehavior.BasedOnAlignment,
                Tag = header
            };

            //splitter.DragCompleted += new System.Windows.Controls.Primitives.DragCompletedEventHandler(vsplitter_DragCompleted);
            Grid.SetColumn(splitter, pos);
            Grid.SetRow(splitter, def.Level);
            if (depth - def.Level > 0)
                Grid.SetRowSpan(splitter, depth - def.Level + 1);

            parent.Children.Add(splitter);

            return splitter;
        }

        private RowDefinition CreateRowDefinition(int idx, List<HierarchyDefinition> leaves, Grid parent)
        {
            /* Create row definition */
            var rDef = new RowDefinition();
            rDef.Height = new GridLength(leaves[idx].Size);
            rDef.SharedSizeGroup = "R" + idx;
            parent.RowDefinitions.Add(rDef);

            return rDef;
        }

        private void UpdateSize(HierarchyDefinition[] rowsElements, HierarchyDefinition[] colsElements, bool draw = true)
        {
            UpdateSize(rowsElements.Leaves().ToList(), AvailableRowSpace, VScrollVGrid, true, ScrollGrid.ColumnDefinitions[1].Width, 25);
            UpdateSize(colsElements.Leaves().ToList(), AvailableColumnSpace, HScrollVGrid, false, ScrollGrid.RowDefinitions[1].Height, 0);

            //if (draw)
            //    DrawGridVirtual();
        }

        /// <summary>
        /// Determines the elements to be displayed on a given axis according to available space.
        /// </summary>
        /// <param name="leaves">Elements that have a leaf status in the hierarchy</param>
        /// <param name="availSpace">Available space for drawing</param>
        /// <param name="scrollBar">ScrollBar linked to the axis</param>
        /// <param name="gLength">Dimension to be occupied by the scrollbar if needed</param>
        /// <param name="viewPortSize">ViewPortSize property of the scrollbar</param>
        private void UpdateSize<X>(List<X> leaves, double availSpace, ScrollBar scrollBar, bool isVertical, GridLength gLength, int viewPortSize) where X : HierarchyDefinition
        {
            int elements = leaves.Count;
            int els = elements;
            int min = 0;

            while (availSpace > 0 && els > 0)
            {
                els--;
                availSpace -= leaves[els].Size;
                min++;
            }

            scrollBar.Minimum = 0;
            if (elements - min == 0 && availSpace > 0)
            {
                gLength = new GridLength(0, GridUnitType.Pixel);
                scrollBar.Maximum = 0;
            }
            else
            {
                gLength = new GridLength(0, GridUnitType.Auto);
                scrollBar.Maximum = elements - min + 1;
                scrollBar.ViewportSize = viewPortSize;
            }

            if (isVertical)
            {
                if (ViewModel.VScrollPos > scrollBar.Maximum)
                    ViewModel.VScrollPos = (int)scrollBar.Maximum;
            }
            else
            {
                if (ViewModel.HScrollPos > scrollBar.Maximum)
                    ViewModel.HScrollPos = (int)scrollBar.Maximum;
            }
        }
    }
}