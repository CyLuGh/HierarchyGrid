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

        private void UpdateSize(HierarchyGridViewModel hgvm, bool draw = true)
        {
            UpdateSize(hgvm.RowsElements.Leaves().ToList(), AvailableRowSpace, VScrollVGrid, true, ScrollGrid.ColumnDefinitions[1].Width, 25);
            UpdateSize(hgvm.ColumnsElements.Leaves().ToList(), AvailableColumnSpace, HScrollVGrid, false, ScrollGrid.RowDefinitions[1].Height, 0);

            if (draw)
                DrawGridVirtual(hgvm);
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

        /// <summary>
        /// Removes children elements and row/column definitions for each grid components.
        /// </summary>
        private void ClearGrids()
        {
            //foreach (var col in ConsumersFlat)
            //    col.IsHovered = col.IsContentHovered = false;

            //foreach (var row in ProducersFlat)
            //    row.IsHovered = row.IsContentHovered = false;

            VColumnHeadersGrid.ColumnDefinitions.Clear();
            VColumnHeadersGrid.RowDefinitions.Clear();
            VColumnHeadersGrid.Children.Clear();

            VRowHeadersGrid.ColumnDefinitions.Clear();
            VRowHeadersGrid.RowDefinitions.Clear();
            VRowHeadersGrid.Children.Clear();

            ClearCells();
        }

        private void ClearCells()
        {
            VCellsGrid.ColumnDefinitions.Clear();
            VCellsGrid.RowDefinitions.Clear();
        }

        /// <summary>
        /// Removes column elements only (horizontal scroll)
        /// </summary>
        private void ClearH()
        {
            //foreach (var col in ConsumersFlat)
            //    col.IsHovered = col.IsContentHovered = false;

            VColumnHeadersGrid.ColumnDefinitions.Clear();
            VColumnHeadersGrid.RowDefinitions.Clear();
            VColumnHeadersGrid.Children.Clear();

            VCellsGrid.ColumnDefinitions.Clear();
        }

        /// <summary>
        /// Removes row elements only (vertical scroll)
        /// </summary>
        private void ClearV()
        {
            //foreach (var row in ProducersFlat)
            //    row.IsHovered = row.IsContentHovered = false;

            VRowHeadersGrid.ColumnDefinitions.Clear();
            VRowHeadersGrid.RowDefinitions.Clear();
            VRowHeadersGrid.Children.Clear();

            VCellsGrid.RowDefinitions.Clear();
        }

        /// <summary>
        /// Redraws the visible parts of the grid.
        /// </summary>
        private void DrawGridVirtual(HierarchyGridViewModel hgvm)
        {
            ClearGrids();

            DrawHeaders(hgvm.RowsElements, hgvm.RowLevels, ScrollGrid.RowDefinitions[0].ActualHeight * 1 / ScaleFactor, hgvm.VScrollPos, false);
            DrawHeaders(hgvm.ColumnsElements, hgvm.ColumnLevels, ScrollGrid.ActualWidth * 1 / ScaleFactor - VGrid.ColumnDefinitions[0].Width.Value, hgvm.HScrollPos, true);

            DrawCellsContent(hgvm);

            //if (CheckDefinitions(ProducersDefinitions, ConsumersDefinitions))
            //{
            //    DrawHeaders(RowsElements, ScrollGrid.RowDefinitions[0].ActualHeight * 1 / ScaleFactor, VScrollPos, false);
            //    DrawHeaders(ColumnsElements, ScrollGrid.ActualWidth * 1 / ScaleFactor - VGrid.ColumnDefinitions[0].Width.Value, HScrollPos, true);

            //    DrawGlobalHeaders();
            //    Timer.Interval = DetermineDelay();
            //    SuggestDrawCellsContent();
            //}
        }

        /// <summary>
        /// Redraws the grid when scrolling horizontally
        /// </summary>
        private void DrawGridVirtualH(HierarchyGridViewModel hgvm)
        {
            //DrawingHHeaders = true;
            ClearH();
            DrawHeaders(hgvm.ColumnsElements, hgvm.ColumnLevels, ScrollGrid.ActualWidth * 1 / ScaleFactor - VGrid.ColumnDefinitions[0].Width.Value, hgvm.HScrollPos, true);

            DrawCellsContent(hgvm);
            //DrawHeaders(ColumnsElements, ScrollGrid.ActualWidth * 1 / ScaleFactor - VGrid.ColumnDefinitions[0].Width.Value, HScrollPos, true);
            //DrawingHHeaders = false;
            //SuggestDrawCellsContent();
        }

        /// <summary>
        /// Redraws the grid when scrolling vertically
        /// </summary>
        private void DrawGridVirtualV(HierarchyGridViewModel hgvm)
        {
            //DrawingVHeaders = true;
            ClearV();
            DrawHeaders(hgvm.RowsElements, hgvm.RowLevels, ScrollGrid.RowDefinitions[0].ActualHeight * 1 / ScaleFactor, hgvm.VScrollPos, false);

            DrawCellsContent(hgvm);
            //DrawHeaders(RowsElements, ScrollGrid.RowDefinitions[0].ActualHeight * 1 / ScaleFactor, VScrollPos, false);
            //DrawingVHeaders = false;
            //SuggestDrawCellsContent();
        }

        private int _previousVPos, _previousHPos;

        private void DrawCellsContent(HierarchyGridViewModel hgvm)
        {
            int cpt = 0;
            int childCount = VCellsGrid.Children.Count;

            for (int col = 0; col < VCellsGrid.ColumnDefinitions.Count; col++)
                for (int row = 0; row < VCellsGrid.RowDefinitions.Count; row++)
                    cpt = DrawCellContent(hgvm, cpt, childCount, row, col);

            /* Remove unused elements */
            if (cpt < childCount)
                VCellsGrid.Children.RemoveRange(cpt, childCount);

            _previousHPos = hgvm.HScrollPos;
            _previousVPos = hgvm.VScrollPos;
        }

        /// <summary>
        /// Draw content of a cell, recycling components when it can.
        /// </summary>
        /// <param name="cpt">Position of the cell in control.</param>
        /// <param name="childCount">Current child elements count in control. Elements alternate between Rectangle and GridTextBlock.</param>
        /// <param name="row">Current row.</param>
        /// <param name="col">Current column.</param>
        private int DrawCellContent(HierarchyGridViewModel hgvm, int cpt, int childCount, int row, int col)
        {
            ToggleButton rH = GetRowHeader(row);
            ToggleButton cH = GetColumnHeader(col);

            if (cH == null || rH == null)
                return 0;

            var producer = (ProducerDefinition)(!hgvm.IsTransposed ? rH.Tag : cH.Tag);
            var consumer = (ConsumerDefinition)(!hgvm.IsTransposed ? cH.Tag : rH.Tag);
            //var coord = new GridCoordinates(producer, consumer);

            var viewModel = hgvm.FindCell(producer, consumer);

            var cell = GetCell(ref cpt, childCount, row, col, viewModel);

            //// TODO Set decors
            ////if ( HasDecor )
            ////{
            ////    Decor decors = GetDecor( ref cpt , childCount , row , col );
            ////    DrawDecor( decors , coord );
            ////}

            //cell.ClickAction = (o, e) => CellSingleClickAction(o, e, coord, col, row);
            //cell.RightClickAction = (o, e) => RightClickAction(o, e, coord, cell.ClickAction);
            //if (!producer.IsReadOnly && !consumer.IsReadOnly && consumer.Edit != null)
            //    cell.DoubleClickAction = (o, e) => CellDoubleClickAction(cell, e, coord, col, row);

            return cpt;
        }

        /// <summary>
        /// Creates or recycles a VirtualHierarchyGridCell (with mouse behavior)
        /// </summary>
        /// <param name="cpt">Current cell count</param>
        /// <param name="childCount">Cells already existing</param>
        /// <param name="row">Grid row</param>
        /// <param name="col">Grid column</param>
        /// <param name="rowHDef">Row definition</param>
        /// <param name="columnHDef">Column definition</param>
        /// <returns>Cell newly created or recycled</returns>
        private HierarchyGridCell GetCell(ref int cpt, int childCount, int row, int col, HierarchyGridCellViewModel viewModel)
        {
            HierarchyGridCell cell = null;

            if (cpt < childCount)
            {
                cell = VCellsGrid.Children[cpt] as HierarchyGridCell;

                //if (cell != null)
                //{
                //    cell.ClickAction = null;
                //    cell.DoubleClickAction = null;
                //    cell.RightClickAction = null;
                //    cell.Clear();
                //}
            }

            if (cell == null) // We're out of bounds or element wasn't a cell
            {
                cell = new HierarchyGridCell();
                VCellsGrid.Children.Add(cell);
            }

            cell.ViewModel = viewModel;

            //cell.TextAlignment = CellTextAlignment;
            //cell.HasDecor = HasDecor;
            //cell.Classification = CellClassification.Normal;
            //cell.EnableCrosshair = EnableCrosshair;

            Grid.SetColumn(cell, col);
            Grid.SetRow(cell, row);

            cpt++;
            return cell;
        }
    }
}