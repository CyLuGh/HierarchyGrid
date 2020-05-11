using HierarchyGrid.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace HierarchyGrid
{
    partial class VirtualHierarchyGrid
    {
        private List<double> RowHeadersWidth { get; } = new List<double>();

        private void ResizeGridRowHeaders(bool columnsOnly, HierarchyDefinition[] rowDefinitions)
        {
            if (columnsOnly)
                VGrid.ColumnDefinitions[0].Width = new GridLength(0d);
            else if (VGrid.ColumnDefinitions.Count > 0)
            {
                var virtualDepth = rowDefinitions.TotalDepth(false);
                var depth = rowDefinitions.TotalDepth();
                if (virtualDepth == depth)
                    VGrid.ColumnDefinitions[0].Width = new GridLength(RowHeadersWidth.Take(virtualDepth).Sum());
                else
                    VGrid.ColumnDefinitions[0].Width = new GridLength(RowHeadersWidth.Take(virtualDepth + 1).Sum());
            }
        }

        /// <summary>
        /// Creates the columns in the row headers.
        /// </summary>
        /// <param name="depth">Number of levels in the hierarchy</param>
        /// <param name="incomplete">Whether or not the hierarchy is fully expanded. True is some elements are hidden.</param>
        private void CreateRowHeaderColumns(HierarchyDefinition[] rowDefinitions, int depth, bool columnsOnly, bool incomplete = false)
        {
            for (int i = 0; i < depth; i++)
            {
                var colDef = new ColumnDefinition();
                if (i < rowDefinitions.TotalDepth(false) - 1)
                {
                    colDef.SharedSizeGroup = "RC" + i;
                }
                else
                {
                    if (incomplete)
                    {
                        colDef.SharedSizeGroup = "RC" + i;
                        VRowHeadersGrid.ColumnDefinitions.Add(colDef);
                        colDef = new ColumnDefinition();
                    }
                    colDef.Width = new GridLength(1, GridUnitType.Star);
                }
                VRowHeadersGrid.ColumnDefinitions.Add(colDef);
            }

            ResizeGridRowHeaders(columnsOnly, rowDefinitions);
        }

        private LockableToggleButton CreateRowHeader(HierarchyDefinition def, int column, int row, int span, Grid parent)
        {
            var header = CreateHeader(def);
            header.LockToggle = !def.CanToggle;
            Grid.SetRow(header, row);
            Grid.SetColumn(header, column);
            Grid.SetRowSpan(header, span);
            parent.Children.Add(header);

            return header;
        }

        private LockableToggleButton CreateRowHeader(HierarchyDefinition def, int pos, int depth, Grid parent, bool incomplete = false)
        {
            var header = CreateHeader(def);
            header.LockToggle = !def.CanToggle;
            // Add header in grid
            Grid.SetRow(header, pos);
            Grid.SetColumn(header, def.Level);
            if (depth - def.Level > 0)
                Grid.SetColumnSpan(header, depth - def.Level + 1 + (incomplete ? 1 : 0));
            parent.Children.Add(header);

            return header;
        }

        /// <summary>
        /// Draws the lower levels of row headers.
        /// </summary>
        /// <param name="leaves">Leaves in the hierarchy definitions composing the headers</param>
        /// <param name="availSpace">Available room for display</param>
        /// <param name="depth">Number of levels in the hierarchy</param>
        /// <param name="scrollPosition">Position of scrollbar as index</param>
        /// <param name="incomplete">Whether or not the hierarchy is fully expanded. True is some elements are hidden.</param>
        private void DrawRowHeaderLeaves(List<HierarchyDefinition> leaves, double availSpace, int depth, int scrollPosition, bool incomplete = false)
        {
            CreateRowHeaderColumns(leaves.ToArray(), depth, incomplete);

            RowDefinition rDef;
            LockableToggleButton header;
            double d = 0;
            int idx = scrollPosition + FixedRows;
            int pos = 0;

            /* FIXED FIRST COLUMN */
            for (int fix = 0; fix < FixedRows; fix++)
            {
                CreateRowDefinition(fix, leaves, VRowHeadersGrid);
                /* Create toggle button acting as header */
                HierarchyDefinition def = leaves[fix];
                header = CreateRowHeader(def, pos, depth, VRowHeadersGrid, incomplete);
                if (!def.HasChild)
                {
                    header.Click += (x, y) =>
                    {
                        //def.IsHighlighted = !def.IsHighlighted;
                    };
                }

                /* Add row to cells */
                rDef = new RowDefinition();
                if (fix < leaves.Count - 1)
                    rDef.SharedSizeGroup = "R" + fix;
                VCellsGrid.RowDefinitions.Add(rDef);

                d += leaves[fix].Size;
                pos++;
            }
            /* END FIXED */

            while (d < availSpace && idx < leaves.Count)
            {
                CreateRowDefinition(idx, leaves, VRowHeadersGrid);

                // Create toggle button acting as header
                HierarchyDefinition def = leaves[idx];
                LockableToggleButton tb = CreateRowHeader(def, pos, depth, VRowHeadersGrid, incomplete);
                if (!def.HasChild)
                {
                    tb.Click += (x, y) =>
                    {
                        //def.IsHighlighted = !def.IsHighlighted;
                    };
                }

                // Add row to cells
                rDef = new RowDefinition();
                rDef.SharedSizeGroup = "R" + idx;
                VCellsGrid.RowDefinitions.Add(rDef);

                d += leaves[idx].Size;
                idx++;
                pos++;
            }
        }

        /// <summary>
        /// Draws the upper levels of the hierarchy, on top of the leaves.
        /// </summary>
        /// <param name="scrollPosition">Position of scrollbar as index</param>
        private void DrawRowHeaderExpanded(int scrollPosition, Dictionary<int, LinkedList<HierarchyDefinition>> rowsLevel)
        {
            for (int stage = 0; stage < VRowHeadersGrid.ColumnDefinitions.Count - 1; stage++)
            {
                int pos = FixedRows;
                while (pos < VRowHeadersGrid.RowDefinitions.Count)
                {
                    HierarchyDefinition def = rowsLevel[stage].At(pos + scrollPosition);
                    if (def != null && def.HasChild && def.IsExpanded)
                    {
                        LockableToggleButton tb = CreateRowHeader(def, stage, pos, def.Span, VRowHeadersGrid);
                        pos += def.Span;
                    }
                    else
                    {
                        pos++;
                    }
                }
            }
        }
    }
}