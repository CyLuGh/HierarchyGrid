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

namespace VirtualHierarchyGrid
{
    partial class HierarchyGrid
    {
        private void DrawGrid(Size size)
        {
            HierarchyGridCanvas.Children.Clear();

            DrawColumnsHeaders(ViewModel.ColumnsDefinitions, size.Width);
            DrawRowsHeaders(ViewModel.RowsDefinitions, size.Height);
        }

        private void DrawColumnsHeaders(HierarchyDefinition[] definitions, double availableWidth)
        {
            double currentPosition = ViewModel.RowsHeadersWidth.Sum();
            int index = 0;
            int column = 0;

            var hdefs = definitions.FlatList(false).ToArray();
            var splitters = new List<GridSplitter>();

            while (index < hdefs.Length && currentPosition < availableWidth)
            {
                var hdef = hdefs[index];

                var width = hdef.HasChild ?
                    Enumerable.Range(column, hdef.Count()).Select(x => ViewModel.ColumnsWidths[x]).Sum() :
                    ViewModel.ColumnsWidths[column];

                var height = hdef.IsExpanded ?
                    ViewModel.ColumnsHeadersHeight[hdef.Level] :
                    Enumerable.Range(hdef.Level, hdef.Depth()).Select(x => ViewModel.ColumnsHeadersHeight[x]).Sum();

                var tb = new ToggleButton
                {
                    Content = hdef.Content,
                    Height = height,
                    Width = width,
                    IsChecked = hdef.HasChild && hdef.IsExpanded
                };

                if (hdef.HasChild)
                {
                    tb.Events().Checked
                        .Do(_ => hdef.IsExpanded = true)
                        .Select(_ => Unit.Default)
                        .InvokeCommand(ViewModel, x => x.DrawGridCommand);
                    tb.Events().Unchecked
                        .Do(_ => hdef.IsExpanded = false)
                        .Select(_ => Unit.Default)
                        .InvokeCommand(ViewModel, x => x.DrawGridCommand);
                }
                else
                    tb.IsHitTestVisible = false;

                var top = Enumerable.Range(0, hdef.Level).Select(x => ViewModel.ColumnsHeadersHeight[x]).Sum();
                Canvas.SetLeft(tb, currentPosition);
                Canvas.SetTop(tb, top);
                HierarchyGridCanvas.Children.Add(tb);

                var gridSplitter = new GridSplitter
                {
                    Width = 2,
                    Height = height,
                    Background = Brushes.Transparent,
                    Tag = column
                };
                gridSplitter.Events().DragCompleted
                    .Do(e =>
                    {
                        var currentColumn = (int)(e.Source as GridSplitter)?.Tag;
                        ViewModel.ColumnsWidths[currentColumn] = (double)Math.Max(ViewModel.ColumnsWidths[currentColumn] + e.HorizontalChange, 10d);
                    })
                    .Select(_ => Unit.Default)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .InvokeCommand(ViewModel, x => x.DrawGridCommand);
                Canvas.SetLeft(gridSplitter, currentPosition + width - 1);
                Canvas.SetTop(gridSplitter, top);
                splitters.Add(gridSplitter);

                index++;
                if (!hdef.IsExpanded || !hdef.HasChild)
                {
                    column++;
                    currentPosition += width;
                }
            }

            foreach (var gridSplitter in splitters)
                HierarchyGridCanvas.Children.Add(gridSplitter);
        }

        private void DrawRowsHeaders(HierarchyDefinition[] definitions, double availableHeight)
        {
            double currentPosition = ViewModel.ColumnsHeadersHeight.Sum();
            int index = 0;
            int row = 0;

            var hdefs = definitions.FlatList(false).ToArray();
            var splitters = new List<GridSplitter>();

            while (index < hdefs.Length && currentPosition < availableHeight)
            {
                var hdef = hdefs[index];

                var height = hdef.HasChild ?
                    Enumerable.Range(row, hdef.Count()).Select(x => ViewModel.RowsHeights[x]).Sum() :
                    ViewModel.RowsHeights[row];

                var width = hdef.IsExpanded ?
                    ViewModel.RowsHeadersWidth[hdef.Level] :
                    Enumerable.Range(hdef.Level, hdef.Depth()).Select(x => ViewModel.RowsHeadersWidth[x]).Sum();

                var tb = new ToggleButton
                {
                    Content = hdef.Content,
                    Height = height,
                    Width = width,
                    IsChecked = hdef.HasChild && hdef.IsExpanded
                };

                if (hdef.HasChild)
                {
                    tb.Events().Checked
                        .Do(_ => hdef.IsExpanded = true)
                        .Select(_ => Unit.Default)
                        .InvokeCommand(ViewModel, x => x.DrawGridCommand);
                    tb.Events().Unchecked
                        .Do(_ => hdef.IsExpanded = false)
                        .Select(_ => Unit.Default)
                        .InvokeCommand(ViewModel, x => x.DrawGridCommand);
                }
                else
                    tb.IsHitTestVisible = false;

                var left = Enumerable.Range(0, hdef.Level).Select(x => ViewModel.RowsHeadersWidth[x]).Sum();
                Canvas.SetLeft(tb, left);
                Canvas.SetTop(tb, currentPosition);
                HierarchyGridCanvas.Children.Add(tb);

                var gridSplitter = new GridSplitter
                {
                    Width = width,
                    Height = 2,
                    ResizeDirection = GridResizeDirection.Rows,
                    Background = Brushes.Transparent,
                    Tag = row
                };
                gridSplitter.Events().DragCompleted
                    .Do(e =>
                    {
                        var currentRow = (int)(e.Source as GridSplitter)?.Tag;
                        ViewModel.RowsHeights[currentRow] = (double)Math.Max(ViewModel.RowsHeights[currentRow] + e.VerticalChange, 10d);
                    })
                    .Select(_ => Unit.Default)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .InvokeCommand(ViewModel, x => x.DrawGridCommand);
                Canvas.SetLeft(gridSplitter, left);
                Canvas.SetTop(gridSplitter, currentPosition + height - 1);
                splitters.Add(gridSplitter);

                index++;
                if (!hdef.IsExpanded || !hdef.HasChild)
                {
                    row++;
                    currentPosition += height;
                }
            }

            foreach (var gridSplitter in splitters)
                HierarchyGridCanvas.Children.Add(gridSplitter);
        }
    }
}