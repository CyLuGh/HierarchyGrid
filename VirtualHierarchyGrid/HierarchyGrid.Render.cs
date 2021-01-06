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
using System.Diagnostics.CodeAnalysis;
using System.Windows.Input;
using Splat;

namespace VirtualHierarchyGrid
{
    partial class HierarchyGrid
    {
        // Keep a cache of cells to be reused when redrawing -- it costs less to reuse than create
        private readonly List<HierarchyGridCell> _cells = new List<HierarchyGridCell>();

        private readonly List<HierarchyGridHeader> _headers = new List<HierarchyGridHeader>();
        private readonly List<GridSplitter> _splitters = new List<GridSplitter>();

        private readonly HashSet<HierarchyDefinition> _columnsParents = new HashSet<HierarchyDefinition>();
        private readonly HashSet<HierarchyDefinition> _rowsParents = new HashSet<HierarchyDefinition>();

        private void DrawGrid(Size size)
        {
            HierarchyGridCanvas.Children.Clear();
            ViewModel.HoveredRow = -1;
            ViewModel.HoveredColumn = -1;

            if (!ViewModel.IsValid)
                return;

            _columnsParents.Clear();
            _rowsParents.Clear();

            int headerCount = 0;
            int splitterCount = 0;

            var rowDefinitions = ViewModel.RowsDefinitions.Leaves().ToArray();
            var colDefinitions = ViewModel.ColumnsDefinitions.Leaves().ToArray();

            DrawColumnsHeaders(colDefinitions, size.Width / ViewModel.Scale, ref headerCount, ref splitterCount);
            DrawRowsHeaders(rowDefinitions, size.Height / ViewModel.Scale, ref headerCount, ref splitterCount);

            // Draw global headers afterwards or last splitter will be drawn under column headers
            DrawGlobalHeaders(ref headerCount, ref splitterCount);

            DrawCells(size, rowDefinitions, colDefinitions);

            RestoreHighlightedCell();
            RestoreHoveredCell();
        }

        private void RestoreHighlightedCell()
        {
            var rows = ViewModel.Highlights.Items.Where(o => o.isRow).Select(o => o.pos).ToArray();
            var columns = ViewModel.Highlights.Items.Where(o => !o.isRow).Select(o => o.pos).ToArray();

            _cells.Where(c => columns.Contains(c.ViewModel.ColumnIndex) || rows.Contains(c.ViewModel.RowIndex))
                .ForAll(c => c.ViewModel.IsHighlighted = true);
        }

        private void RestoreHoveredCell()
        {
            var hoveredCell = (Mouse.DirectlyOver as DependencyObject)?.GetVisualParent<HierarchyGridCell>();
            if (hoveredCell != null)
            {
                this.Log().Debug(hoveredCell);
                hoveredCell.ViewModel.IsHovered = true;
                ViewModel.HoveredColumn = hoveredCell.ViewModel.ColumnIndex;
                ViewModel.HoveredRow = hoveredCell.ViewModel.RowIndex;
            }
        }

        private void DrawGlobalHeaders(ref int headerCount, ref int splitterCount)
        {
            var rowDepth = ViewModel.RowsDefinitions.TotalDepth();
            var colDepth = ViewModel.ColumnsDefinitions.TotalDepth();

            var splitters = new List<GridSplitter>();
            Action<DragCompletedEventArgs> action = e =>
            {
                (int position, IDisposable drag) tag = ((int, IDisposable))((GridSplitter)e.Source).Tag;
                var idx = tag.position;
                ViewModel.RowsHeadersWidth[idx] = (double)Math.Max(ViewModel.RowsHeadersWidth[idx] + e.HorizontalChange, 10d);
            };

            double currentX = 0, currentY = 0;

            var columnsVerticalSpan = ViewModel.ColumnsHeadersHeight.Take(ViewModel.ColumnsHeadersHeight.Length - 1).Sum();
            var rowsHorizontalSpan = ViewModel.RowsHeadersWidth.Take(ViewModel.RowsHeadersWidth.Length - 1).Sum();

            var foldAllButton = BuildHeader(ref headerCount, null, rowsHorizontalSpan, columnsVerticalSpan);
            foldAllButton.ViewModel.Content = TryFindResource("CollapseAllIcon");
            foldAllButton.ToolTip = "Collapse all";
            var evts = new Queue<IDisposable>();
            evts.Enqueue(foldAllButton.Events().MouseLeftButtonDown
                .Do(_ =>
                {
                    ViewModel.SelectedPositions.Clear();
                    ViewModel.RowsDefinitions.FlatList(true).ForEach(x => x.IsExpanded = false);
                    ViewModel.ColumnsDefinitions.FlatList(true).ForEach(x => x.IsExpanded = false);
                })
                .Select(_ => Unit.Default)
                .InvokeCommand(ViewModel, x => x.DrawGridCommand));
            foldAllButton.Tag = evts;

            Canvas.SetLeft(foldAllButton, currentX);
            Canvas.SetTop(foldAllButton, currentY);
            HierarchyGridCanvas.Children.Add(foldAllButton);

            // Draw row headers
            currentY = columnsVerticalSpan;
            for (int i = 0; i < rowDepth - 1; i++)
            {
                var width = ViewModel.RowsHeadersWidth[i];
                var height = ViewModel.ColumnsHeadersHeight.Last();
                var tb = BuildHeader(ref headerCount, null, width, height);
                var queue = new Queue<IDisposable>();
                var idx = i; // Copy to local variable or else event will always use last value of i
                evts.Enqueue(tb.Events().MouseLeftButtonDown
                .Do(_ =>
                {
                    ViewModel.SelectedPositions.Clear();

                    var defs = ViewModel.RowsDefinitions.FlatList(true)
                                             .Where(x => x.Level == idx)
                                             .ToArray();
                    var desiredState = defs.AsParallel().Any(x => x.IsExpanded);

                    defs.ForEach(x => x.IsExpanded = !desiredState);
                })
                .Select(_ => Unit.Default)
                .InvokeCommand(ViewModel, x => x.DrawGridCommand));
                tb.ViewModel.Content = ViewModel.RowsDefinitions
                                            .FlatList(true)
                                            .AsParallel()
                                            .Where(x => x.Level == idx)
                                            .Any(x => x.IsExpanded) ? TryFindResource("CollapseIcon") : TryFindResource("ExpandIcon");
                tb.Tag = evts;

                Canvas.SetLeft(tb, currentX);
                Canvas.SetTop(tb, currentY);
                HierarchyGridCanvas.Children.Add(tb);
                currentX += width;

                var gSplitter = BuildSplitter(ref splitterCount, 2, height, GridResizeDirection.Columns, i, action);
                Canvas.SetLeft(gSplitter, currentX);
                Canvas.SetTop(gSplitter, currentY);
                splitters.Add(gSplitter);
            }

            // Draw column headers
            currentY = 0;
            for (int i = 0; i < colDepth - 1; i++)
            {
                var width = ViewModel.RowsHeadersWidth.Last();
                var height = ViewModel.ColumnsHeadersHeight[i];
                var tb = BuildHeader(ref headerCount, null, width, height);

                var idx = i; // Copy to local variable or else event will always use last value of i
                evts.Enqueue(tb.Events().MouseLeftButtonDown
                .Do(_ =>
                {
                    ViewModel.SelectedPositions.Clear();
                    var defs = ViewModel.ColumnsDefinitions.FlatList(true)
                                             .Where(x => x.Level == idx)
                                             .ToArray();
                    var desiredState = defs.AsParallel().Any(x => x.IsExpanded);
                    defs.ForEach(x => x.IsExpanded = !desiredState);
                })
                .Select(_ => Unit.Default)
                .InvokeCommand(ViewModel, x => x.DrawGridCommand));
                tb.ViewModel.Content = ViewModel.ColumnsDefinitions
                                            .FlatList(true)
                                            .AsParallel()
                                            .Where(x => x.Level == idx)
                                            .Any(x => x.IsExpanded) ? TryFindResource("CollapseIcon") : TryFindResource("ExpandIcon");
                tb.Tag = evts;

                Canvas.SetLeft(tb, currentX);
                Canvas.SetTop(tb, currentY);
                HierarchyGridCanvas.Children.Add(tb);
                currentY += height;
            }

            var expandAllButton = BuildHeader(ref headerCount, null, ViewModel.RowsHeadersWidth.Last(), ViewModel.ColumnsHeadersHeight.Last());
            expandAllButton.ViewModel.Content = TryFindResource("ExpandAllIcon");

            expandAllButton.ToolTip = "Expand all";
            evts = new Queue<IDisposable>();
            evts.Enqueue(expandAllButton.Events().MouseLeftButtonDown
                .Do(_ =>
                {
                    ViewModel.SelectedPositions.Clear();
                    ViewModel.RowsDefinitions.FlatList(true).ForEach(x => x.IsExpanded = true);
                    ViewModel.ColumnsDefinitions.FlatList(true).ForEach(x => x.IsExpanded = true);
                })
                .Select(_ => Unit.Default)
                .InvokeCommand(ViewModel, x => x.DrawGridCommand));
            expandAllButton.Tag = evts;

            Canvas.SetLeft(expandAllButton, currentX);
            Canvas.SetTop(expandAllButton, currentY);
            HierarchyGridCanvas.Children.Add(expandAllButton);

            var splitter = BuildSplitter(ref splitterCount,
                2,
                ViewModel.ColumnsHeadersHeight.Last(),
                GridResizeDirection.Columns,
                ViewModel.RowsHeadersWidth.Length - 1,
                action);
            Canvas.SetLeft(splitter, ViewModel.RowsHeadersWidth.Sum());
            Canvas.SetTop(splitter, currentY);
            splitters.Add(splitter);

            foreach (var gridSplitter in splitters)
                HierarchyGridCanvas.Children.Add(gridSplitter);
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
                while (rowIdx < rowDefinitions.Length && verticalPosition < size.Height / ViewModel.Scale)
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
                while (colIdx < colDefinitions.Length && horizontalPosition < size.Width / ViewModel.Scale)
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
            while (horizontalIdx < colDefinitions.Length && horizontalPosition < size.Width / ViewModel.Scale)
            {
                var width = ViewModel.ColumnsWidths[horizontalIdx];

                verticalPosition = ViewModel.ColumnsHeadersHeight.Sum() + frozenRows.Sum(f => ViewModel.RowsHeights[rowDefinitions.IndexOf(f)]);
                int verticalIdx = ViewModel.VerticalOffset + frozenRows.Length;

                while (verticalIdx < rowDefinitions.Length && verticalPosition < size.Height / ViewModel.Scale)
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
                cell.ViewModel.Clear();
            }
            else
            {
                cell = new HierarchyGridCell { ViewModel = new HierarchyGridCellViewModel(ViewModel) };
                _cells.Add(cell);
            }

            cell.ViewModel.IsSelected = ViewModel.SelectedPositions.Lookup((verticalIdx, horizontalIdx)).HasValue;
            cell.ViewModel.ColumnIndex = horizontalIdx;
            cell.ViewModel.RowIndex = verticalIdx;

            var producer = (ProducerDefinition)(!ViewModel.IsTransposed ? rowDefinitions[verticalIdx] : colDefinitions[horizontalIdx]);
            var consumer = (ConsumerDefinition)(!ViewModel.IsTransposed ? colDefinitions[horizontalIdx] : rowDefinitions[verticalIdx]);

            var lkp = ViewModel.ResultSets.Lookup((producer.Position, consumer.Position));
            if (lkp.HasValue)
                cell.ViewModel.ResultSet = lkp.Value;

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
                column++;
            }

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
            tb.ViewModel.ColumnIndex = column;

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
            tb.ViewModel.CanToggle = true;

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
                row++;
            }

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
            tb.ViewModel.RowIndex = row;

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
            tb.ViewModel.CanToggle = true;

            var left = Enumerable.Range(0, hdef.Level).Where(x => x < ViewModel.RowsHeadersWidth.Length).Select(x => ViewModel.RowsHeadersWidth[x]).Sum();
            Canvas.SetLeft(tb, left);
            Canvas.SetTop(tb, currentPosition);
            HierarchyGridCanvas.Children.Add(tb);

            _rowsParents.Add(hdef);

            DrawParentRowHeader(hdef, origin, row, currentPosition, ref headerCount);
        }

        private HierarchyGridHeader BuildHeader(ref int headerCount, [AllowNull] HierarchyDefinition hdef, double width, double height)
        {
            HierarchyGridHeader tb = null;
            if (headerCount < _headers.Count)
            {
                tb = _headers[headerCount];
                if (tb.Tag is Queue<IDisposable> previousEvents)
                    previousEvents.ForEach(e => e.Dispose());
                tb.Tag = null;
                tb.ViewModel.RowIndex = null;
                tb.ViewModel.ColumnIndex = null;
                tb.ViewModel.CanToggle = false;
            }
            else
            {
                tb = new HierarchyGridHeader { ViewModel = new HierarchyGridHeaderViewModel(ViewModel) };
                _headers.Add(tb);
            }

            tb.ViewModel.Content = hdef?.Content ?? string.Empty;
            tb.Height = height;
            tb.Width = width;
            tb.ViewModel.IsChecked = hdef?.HasChild == true && hdef?.IsExpanded == true;
            tb.ViewModel.IsHighlighted = hdef?.IsHighlighted ?? false;

            var evts = new Queue<IDisposable>();
            if (hdef?.HasChild == true)
            {
                tb.ViewModel.CanToggle = true;

                evts.Enqueue(tb.Events().MouseLeftButtonDown
                    .Do(_ =>
                    {
                        ViewModel.SelectedPositions.Clear();
                        hdef.IsExpanded = !hdef.IsExpanded;
                    })
                    .Select(_ => Unit.Default)
                    .InvokeCommand(ViewModel, x => x.DrawGridCommand));
            }
            else if (hdef != null)
                // Clicking on header should add/remove from highlights
                evts.Enqueue(tb.Events().MouseLeftButtonDown
                    .Throttle(TimeSpan.FromMilliseconds(200))
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Select(_ =>
                    {
                        hdef.IsHighlighted = !hdef.IsHighlighted;
                        tb.ViewModel.IsHighlighted = hdef.IsHighlighted;

                        return tb.ViewModel;
                    })
                    .InvokeCommand(ViewModel, x => x.UpdateHighlightsCommand));

            tb.Tag = evts;
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