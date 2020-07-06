using HierarchyGrid.Definitions;
using ReactiveUI;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using MoreLinq;
using DynamicData;

namespace VirtualHierarchyGrid
{
    partial class HierarchyGrid
    {
        // Keep a cache of cells to be reused when redrawing -- it costs less to reuse than create
        private List<HierarchyGridCell> _cells = new List<HierarchyGridCell>();

        private List<ToggleButton> _headers = new List<ToggleButton>();
        private List<GridSplitter> _splitters = new List<GridSplitter>();

        private HashSet<HierarchyDefinition> _columnsParents = new HashSet<HierarchyDefinition>();
        private HashSet<HierarchyDefinition> _rowsParents = new HashSet<HierarchyDefinition>();

        private void DrawGrid(Size size)
        {
            HierarchyGridCanvas.Children.Clear();

            if (!ViewModel.IsValid)
                return;

            _columnsParents.Clear();
            _rowsParents.Clear();

            int headerCount = 0;
            int splitterCount = 0;

            var rowDefinitions = ViewModel.RowsDefinitions.Leaves().ToArray();
            var colDefinitions = ViewModel.ColumnsDefinitions.Leaves().ToArray();

            DrawColumnsHeaders(colDefinitions, size.Width, ref headerCount, ref splitterCount);
            DrawRowsHeaders(rowDefinitions, size.Height, ref headerCount, ref splitterCount);

            DrawCells(size, rowDefinitions, colDefinitions);
        }

        private void DrawCells(Size size, HierarchyDefinition[] rowDefinitions, HierarchyDefinition[] colDefinitions)
        {
            var frozenRows = rowDefinitions.Where(x => x.Frozen).ToArray();
            var frozenCols = colDefinitions.Where(x => x.Frozen).ToArray();

            double horizontalPosition = ViewModel.RowsHeadersWidth.Sum();
            double verticalPosition = 0d;
            int idx = 0;

            // Draw intersection for frozen rows & columns
            foreach (var fCol in frozenCols)
            {
                int colIdx = colDefinitions.IndexOf(fCol);
                var width = ViewModel.ColumnsWidths[colIdx];
                verticalPosition = ViewModel.ColumnsHeadersHeight.Sum();

                foreach (var fRow in frozenRows)
                {
                    int rowIdx = rowDefinitions.IndexOf(fRow);
                    var height = ViewModel.RowsHeights[rowIdx];
                    DrawCell(ref idx, rowIdx, colIdx, width, height, horizontalPosition, verticalPosition, rowDefinitions, colDefinitions);
                    verticalPosition += height;
                }

                horizontalPosition += width;
            }

            // Draw rows for frozen columns
            horizontalPosition = ViewModel.RowsHeadersWidth.Sum();
            foreach (var fCol in frozenCols)
            {
                int colIdx = colDefinitions.IndexOf(fCol);
                var width = ViewModel.ColumnsWidths[colIdx];
                int rowIdx = ViewModel.VerticalOffset + frozenRows.Length;

                verticalPosition = ViewModel.ColumnsHeadersHeight.Sum() + frozenRows.Sum(f => ViewModel.RowsHeights[rowDefinitions.IndexOf(f)]);
                while (rowIdx < rowDefinitions.Length && verticalPosition < size.Height)
                {
                    var height = ViewModel.RowsHeights[rowIdx];
                    DrawCell(ref idx, rowIdx, colIdx, width, height, horizontalPosition, verticalPosition, rowDefinitions, colDefinitions);
                    verticalPosition += height;
                    rowIdx++;
                }

                horizontalPosition += width;
            }

            // Draw columns for frozen rows
            verticalPosition = ViewModel.ColumnsHeadersHeight.Sum();
            foreach (var fRow in frozenRows)
            {
                int rowIdx = rowDefinitions.IndexOf(fRow);
                var height = ViewModel.RowsHeights[rowIdx];
                int colIdx = ViewModel.HorizontalOffset + frozenCols.Length;

                horizontalPosition = ViewModel.RowsHeadersWidth.Sum() + frozenCols.Sum(f => ViewModel.ColumnsWidths[colDefinitions.IndexOf(f)]);
                while (colIdx < colDefinitions.Length && horizontalPosition < size.Width)
                {
                    var width = ViewModel.ColumnsWidths[colIdx];
                    DrawCell(ref idx, rowIdx, colIdx, width, height, horizontalPosition, verticalPosition, rowDefinitions, colDefinitions);
                    horizontalPosition += width;
                    colIdx++;
                }

                verticalPosition += height;
            }

            // Draw non frozen elements
            int horizontalIdx = ViewModel.HorizontalOffset + frozenCols.Length;
            horizontalPosition = ViewModel.RowsHeadersWidth.Sum() + frozenCols.Sum(f => ViewModel.ColumnsWidths[colDefinitions.IndexOf(f)]);
            while (horizontalIdx < colDefinitions.Length && horizontalPosition < size.Width)
            {
                var width = ViewModel.ColumnsWidths[horizontalIdx];

                verticalPosition = ViewModel.ColumnsHeadersHeight.Sum() + frozenRows.Sum(f => ViewModel.RowsHeights[rowDefinitions.IndexOf(f)]);
                int verticalIdx = ViewModel.VerticalOffset + frozenRows.Length;

                while (verticalIdx < rowDefinitions.Length && verticalPosition < size.Height)
                {
                    var height = ViewModel.RowsHeights[verticalIdx];
                    DrawCell(ref idx, verticalIdx, horizontalIdx, width, height, horizontalPosition, verticalPosition, rowDefinitions, colDefinitions);
                    verticalPosition += height;
                    verticalIdx++;
                }

                horizontalPosition += width;
                horizontalIdx++;
            }

            if (idx < _cells.Count)
                _cells.RemoveRange(idx, _cells.Count - idx);
        }

        private void DrawCell(ref int idx, int verticalIdx, int horizontalIdx, double width, double height, double horizontalPosition, double verticalPosition, HierarchyDefinition[] rowDefinitions, HierarchyDefinition[] colDefinitions)
        {
            HierarchyGridCell cell;
            if (idx < _cells.Count)
            {
                cell = _cells[idx];
            }
            else
            {
                cell = new HierarchyGridCell { ViewModel = new HierarchyGridCellViewModel(ViewModel) };
                _cells.Add(cell);
            }

            cell.ViewModel.IsSelected = ViewModel.Selections.Any(x => x.row == verticalIdx && x.col == horizontalIdx);
            cell.ViewModel.ColumnIndex = horizontalIdx;
            cell.ViewModel.RowIndex = verticalIdx;

            var producer = (ProducerDefinition)(!ViewModel.IsTransposed ? rowDefinitions[verticalIdx] : colDefinitions[horizontalIdx]);
            var consumer = (ConsumerDefinition)(!ViewModel.IsTransposed ? colDefinitions[horizontalIdx] : rowDefinitions[verticalIdx]);

            if (ViewModel.ResultSets.TryGetValue((producer.Position, consumer.Position), out var rs))
                cell.ViewModel.ResultSet = rs;

            cell.Width = width;
            cell.Height = height;

            Canvas.SetLeft(cell, horizontalPosition);
            Canvas.SetTop(cell, verticalPosition);

            HierarchyGridCanvas.Children.Add(cell);
            idx++;
        }

        private void DrawColumnsHeaders(HierarchyDefinition[] hdefs, double availableWidth, ref int headerCount, ref int splitterCount)
        {
            double currentPosition = ViewModel.RowsHeadersWidth.Sum();
            int column = ViewModel.HorizontalOffset;

            var frozen = hdefs.Where(x => x.Frozen).ToArray();

            ViewModel.MaxHorizontalOffset = hdefs.Length - (1 + frozen.Length);
            var splitters = new List<GridSplitter>();

            foreach (var hdef in frozen)
            {
                var width = ViewModel.ColumnsWidths[hdefs.IndexOf(hdef)];
                DrawColumnHeader(ref headerCount, ref splitterCount, ref currentPosition, column, splitters, hdef, width);
            }

            column += frozen.Length;

            while (column < hdefs.Length && currentPosition < availableWidth)
            {
                var hdef = hdefs[column];
                var width = ViewModel.ColumnsWidths[column];

                DrawColumnHeader(ref headerCount, ref splitterCount, ref currentPosition, column, splitters, hdef, width);
                column++;
            }

            foreach (var gridSplitter in splitters)
                HierarchyGridCanvas.Children.Add(gridSplitter);
        }

        private void DrawColumnHeader(ref int headerCount, ref int splitterCount, ref double currentPosition, int column, List<GridSplitter> splitters, HierarchyDefinition hdef, double width)
        {
            var height = hdef.IsExpanded && hdef.HasChild ?
                                ViewModel.ColumnsHeadersHeight[hdef.Level] :
                                Enumerable.Range(hdef.Level, ViewModel.ColumnsHeadersHeight.Length - hdef.Level)
                                    .Select(x => ViewModel.ColumnsHeadersHeight[x]).Sum();

            var tb = BuildHeader(ref headerCount, hdef, width, height);

            var top = Enumerable.Range(0, hdef.Level).Select(x => ViewModel.ColumnsHeadersHeight[x]).Sum();
            Canvas.SetLeft(tb, currentPosition);
            Canvas.SetTop(tb, top);
            HierarchyGridCanvas.Children.Add(tb);

            Action<DragCompletedEventArgs> action = e =>
            {
                (int position, IDisposable drag) tag = ((int, IDisposable))((GridSplitter)e.Source).Tag;
                var currentColumn = tag.position;
                ViewModel.ColumnsWidths[currentColumn] = (double)Math.Max(ViewModel.ColumnsWidths[currentColumn] + e.HorizontalChange, 10d);
            };
            var gridSplitter = BuildSplitter(ref splitterCount, 2, height, GridResizeDirection.Columns, column, action);

            Canvas.SetLeft(gridSplitter, currentPosition + width - 1);
            Canvas.SetTop(gridSplitter, top);
            splitters.Add(gridSplitter);

            DrawParentColumnHeader(hdef, hdef, column, currentPosition, ref headerCount);
            currentPosition += width;
        }

        private void DrawParentColumnHeader(HierarchyDefinition src, HierarchyDefinition origin, int column, double currentPosition, ref int headerCount)
        {
            if (src.Parent == null)
                return;

            var hdef = src.Parent;

            if (_columnsParents.Contains(hdef))
                return;

            var width = Enumerable.Range(column, hdef.Count() - origin.RelativePositionFrom(hdef))
                .Select(x => ViewModel.ColumnsWidths.TryGetValue(x, out var size) ? size : 0).Sum();

            var height = ViewModel.ColumnsHeadersHeight[hdef.Level];

            var tb = BuildHeader(ref headerCount, hdef, width, height);

            var top = Enumerable.Range(0, hdef.Level).Select(x => ViewModel.ColumnsHeadersHeight[x]).Sum();
            Canvas.SetLeft(tb, currentPosition);
            Canvas.SetTop(tb, top);
            HierarchyGridCanvas.Children.Add(tb);

            _columnsParents.Add(hdef);

            DrawParentColumnHeader(hdef, origin, column, currentPosition, ref headerCount);
        }

        private void DrawRowsHeaders(HierarchyDefinition[] hdefs, double availableHeight, ref int headerCount, ref int splitterCount)
        {
            double currentPosition = ViewModel.ColumnsHeadersHeight.Sum();
            int row = ViewModel.VerticalOffset;

            var frozen = hdefs.Where(x => x.Frozen).ToArray();

            ViewModel.MaxVerticalOffset = hdefs.Length - (1 + frozen.Length);
            var splitters = new List<GridSplitter>();

            foreach (var hdef in frozen)
            {
                var height = ViewModel.RowsHeights[hdefs.IndexOf(hdef)];
                DrawRowHeader(ref headerCount, ref splitterCount, ref currentPosition, row, splitters, hdef, height);
            }

            row += frozen.Length;

            while (row < hdefs.Length && currentPosition < availableHeight)
            {
                var hdef = hdefs[row];
                var height = ViewModel.RowsHeights[row];
                DrawRowHeader(ref headerCount, ref splitterCount, ref currentPosition, row, splitters, hdef, height);

                row++;
            }

            foreach (var gridSplitter in splitters)
                HierarchyGridCanvas.Children.Add(gridSplitter);
        }

        private void DrawRowHeader(ref int headerCount, ref int splitterCount, ref double currentPosition, int row, List<GridSplitter> splitters, HierarchyDefinition hdef, double height)
        {
            var width = hdef.IsExpanded && hdef.HasChild ?
                                ViewModel.RowsHeadersWidth[hdef.Level] :
                                Enumerable.Range(hdef.Level, ViewModel.RowsHeadersWidth.Length - hdef.Level)
                                    .Where(x => x < ViewModel.RowsHeadersWidth.Length)
                                    .Select(x => ViewModel.RowsHeadersWidth[x]).Sum();

            var tb = BuildHeader(ref headerCount, hdef, width, height);

            var left = Enumerable.Range(0, hdef.Level).Where(x => x < ViewModel.RowsHeadersWidth.Length).Select(x => ViewModel.RowsHeadersWidth[x]).Sum();
            Canvas.SetLeft(tb, left);
            Canvas.SetTop(tb, currentPosition);
            HierarchyGridCanvas.Children.Add(tb);

            Action<DragCompletedEventArgs> action = e =>
            {
                (int position, IDisposable drag) tag = ((int, IDisposable))((GridSplitter)e.Source).Tag;
                var currentRow = tag.position;
                ViewModel.RowsHeights[currentRow] = (double)Math.Max(ViewModel.RowsHeights[currentRow] + e.VerticalChange, 10d);
            };
            var gridSplitter = BuildSplitter(ref splitterCount, width, 2, GridResizeDirection.Rows, row, action);

            Canvas.SetLeft(gridSplitter, left);
            Canvas.SetTop(gridSplitter, currentPosition + height - 1);
            splitters.Add(gridSplitter);

            DrawParentRowHeader(hdef, hdef, row, currentPosition, ref headerCount);

            currentPosition += height;
        }

        private void DrawParentRowHeader(HierarchyDefinition src, HierarchyDefinition origin, int row, double currentPosition, ref int headerCount)
        {
            if (src.Parent == null)
                return;

            var hdef = src.Parent;

            if (_rowsParents.Contains(hdef))
                return;

            var height = Enumerable.Range(row, hdef.Count() - origin.RelativePositionFrom(hdef))
                .Select(x => ViewModel.RowsHeights.TryGetValue(x, out var size) ? size : 0).Sum();

            var width = ViewModel.RowsHeadersWidth[hdef.Level];

            var tb = BuildHeader(ref headerCount, hdef, width, height);

            var left = Enumerable.Range(0, hdef.Level).Where(x => x < ViewModel.RowsHeadersWidth.Length).Select(x => ViewModel.RowsHeadersWidth[x]).Sum();
            Canvas.SetLeft(tb, left);
            Canvas.SetTop(tb, currentPosition);
            HierarchyGridCanvas.Children.Add(tb);

            _rowsParents.Add(hdef);

            DrawParentRowHeader(hdef, origin, row, currentPosition, ref headerCount);
        }

        private ToggleButton BuildHeader(ref int headerCount, HierarchyDefinition hdef, double width, double height)
        {
            ToggleButton tb = null;
            if (headerCount < _headers.Count)
            {
                tb = _headers[headerCount];
                if (tb.Tag is Queue<IDisposable> evts)
                    evts.ForEach(e => e.Dispose());
                tb.Tag = null;
            }
            else
            {
                tb = new ToggleButton();
                _headers.Add(tb);
            }

            tb.Content = hdef.Content;
            tb.Height = height;
            tb.Width = width;
            tb.IsChecked = hdef.HasChild && hdef.IsExpanded;

            if (hdef.HasChild)
            {
                var evts = new Queue<IDisposable>();
                evts.Enqueue(tb.Events().Checked
                    .Do(_ => hdef.IsExpanded = true)
                    .Select(_ => Unit.Default)
                    .InvokeCommand(ViewModel, x => x.DrawGridCommand));
                evts.Enqueue(tb.Events().Unchecked
                    .Do(_ => hdef.IsExpanded = false)
                    .Select(_ => Unit.Default)
                    .InvokeCommand(ViewModel, x => x.DrawGridCommand));
                tb.Tag = evts;
            }

            headerCount++;

            return tb;
        }

        private GridSplitter BuildSplitter(ref int splitterCount, double width, double height, GridResizeDirection direction, int position, Action<DragCompletedEventArgs> action)
        {
            GridSplitter gridSplitter = null;

            if (splitterCount < _splitters.Count)
            {
                gridSplitter = _splitters[splitterCount];

                (int position, IDisposable drag) tag = ((int, IDisposable))gridSplitter.Tag;
                tag.drag.Dispose();
            }
            else
            {
                gridSplitter = new GridSplitter();
                _splitters.Add(gridSplitter);
            }

            gridSplitter.Width = width;
            gridSplitter.Height = height;
            gridSplitter.ResizeDirection = direction;
            gridSplitter.Background = Brushes.Transparent;

            var drag = gridSplitter.Events().DragCompleted
                    .Do(e => action(e))
                    .Select(_ => Unit.Default)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .InvokeCommand(ViewModel, x => x.DrawGridCommand);

            gridSplitter.Tag = (position, drag);

            splitterCount++;
            return gridSplitter;
        }
    }
}