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
            DrawColumnsHeaders(size.Width, ref headerCount, ref splitterCount);
            DrawRowsHeaders(size.Height, ref headerCount, ref splitterCount);

            DrawCells(size);
        }

        private void DrawCells(Size size)
        {
            double horizontalPosition = ViewModel.RowsHeadersWidth.Sum();
            int horizontalIdx = ViewModel.HorizontalOffset;

            var rowDefinitions = ViewModel.RowsDefinitions.Leaves().ToArray();
            var colDefinitions = ViewModel.ColumnsDefinitions.Leaves().ToArray();

            int idx = 0;

            while (horizontalIdx < colDefinitions.Length && horizontalPosition < size.Width)
            {
                var width = ViewModel.ColumnsWidths[horizontalIdx];

                double verticalPosition = ViewModel.ColumnsHeadersHeight.Sum();
                int verticalIdx = ViewModel.VerticalOffset;

                while (verticalIdx < rowDefinitions.Length && verticalPosition < size.Height)
                {
                    var height = ViewModel.RowsHeights[verticalIdx];

                    HierarchyGridCell cell;
                    if (idx < _cells.Count)
                    {
                        cell = _cells[idx];
                    }
                    else
                    {
                        cell = new HierarchyGridCell();
                        _cells.Add(cell);
                    }

                    cell.Width = width;
                    cell.Height = height;

                    Canvas.SetLeft(cell, horizontalPosition);
                    Canvas.SetTop(cell, verticalPosition);

                    HierarchyGridCanvas.Children.Add(cell);

                    verticalPosition += height;
                    verticalIdx++;

                    idx++;
                }

                horizontalPosition += width;
                horizontalIdx++;
            }

            if (idx < _cells.Count)
                _cells.RemoveRange(idx, _cells.Count - idx);
        }

        private void DrawColumnsHeaders(double availableWidth, ref int headerCount, ref int splitterCount)
        {
            double currentPosition = ViewModel.RowsHeadersWidth.Sum();
            int column = ViewModel.HorizontalOffset;

            var hdefs = ViewModel.ColumnsDefinitions.Leaves().ToArray();
            ViewModel.MaxHorizontalOffset = hdefs.Length - 1;
            var splitters = new List<GridSplitter>();

            while (column < hdefs.Length && currentPosition < availableWidth)
            {
                var hdef = hdefs[column];

                var width = ViewModel.ColumnsWidths[column];

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

                DrawParentColumnHeader(hdef, column, currentPosition, ref headerCount);

                column++;
                currentPosition += width;
            }

            foreach (var gridSplitter in splitters)
                HierarchyGridCanvas.Children.Add(gridSplitter);
        }

        private void DrawParentColumnHeader(HierarchyDefinition src, int column, double currentPosition, ref int headerCount)
        {
            if (src.Parent == null)
                return;

            var hdef = src.Parent;

            if (_columnsParents.Contains(hdef))
                return;

            var width = Enumerable.Range(column, hdef.Count() - src.RelativePosition)
                .Select(x => ViewModel.ColumnsWidths.TryGetValue(x, out var size) ? size : 0).Sum();

            var height = ViewModel.ColumnsHeadersHeight[hdef.Level];

            var tb = BuildHeader(ref headerCount, hdef, width, height);

            var top = Enumerable.Range(0, hdef.Level).Select(x => ViewModel.ColumnsHeadersHeight[x]).Sum();
            Canvas.SetLeft(tb, currentPosition);
            Canvas.SetTop(tb, top);
            HierarchyGridCanvas.Children.Add(tb);

            _columnsParents.Add(hdef);

            DrawParentColumnHeader(hdef, column, currentPosition, ref headerCount);
        }

        private void DrawRowsHeaders(double availableHeight, ref int headerCount, ref int splitterCount)
        {
            double currentPosition = ViewModel.ColumnsHeadersHeight.Sum();
            int row = ViewModel.VerticalOffset;

            var hdefs = ViewModel.RowsDefinitions.Leaves().ToArray();
            ViewModel.MaxVerticalOffset = hdefs.Length - 1;
            var splitters = new List<GridSplitter>();

            while (row < hdefs.Length && currentPosition < availableHeight)
            {
                var hdef = hdefs[row];

                var height = hdef.HasChild ?
                    Enumerable.Range(row, hdef.Count()).Select(x => ViewModel.RowsHeights.TryGetValue(x, out var size) ? size : 0).Sum() :
                    ViewModel.RowsHeights[row];

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

                DrawParentRowHeader(hdef, row, currentPosition, ref headerCount);

                row++;
                currentPosition += height;
            }

            foreach (var gridSplitter in splitters)
                HierarchyGridCanvas.Children.Add(gridSplitter);
        }

        private void DrawParentRowHeader(HierarchyDefinition src, int row, double currentPosition, ref int headerCount)
        {
            if (src.Parent == null)
                return;

            var hdef = src.Parent;

            if (_rowsParents.Contains(hdef))
                return;

            var height = Enumerable.Range(row, hdef.Count() - src.RelativePosition)
                .Select(x => ViewModel.RowsHeights.TryGetValue(x, out var size) ? size : 0).Sum();

            var width = ViewModel.RowsHeadersWidth[hdef.Level];

            var tb = BuildHeader(ref headerCount, hdef, width, height);

            var left = Enumerable.Range(0, hdef.Level).Where(x => x < ViewModel.RowsHeadersWidth.Length).Select(x => ViewModel.RowsHeadersWidth[x]).Sum();
            Canvas.SetLeft(tb, left);
            Canvas.SetTop(tb, currentPosition);
            HierarchyGridCanvas.Children.Add(tb);

            _rowsParents.Add(hdef);

            DrawParentRowHeader(hdef, row, currentPosition, ref headerCount);
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