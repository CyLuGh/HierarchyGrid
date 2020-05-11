using HierarchyGrid.Definitions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace HierarchyGrid
{
    partial class VirtualHierarchyGrid
    {
        /// <summary>
        /// Draws the upper levels of the hierarchy, on top of the leaves.
        /// </summary>
        /// <param name="scrollPosition">Position of scrollbar as index</param>
        private void DrawColumnHeaderExpanded(int scrollPosition, Dictionary<int, LinkedList<HierarchyDefinition>> columnsLevel)
        {
            for (int stage = 0; stage < VColumnHeadersGrid.RowDefinitions.Count - 1; stage++)
            {
                int pos = FixedColumns;
                // Fill gap ?
                int col = 0;
                while (col < pos)
                {
                    var elements = VColumnHeadersGrid.GetElements(stage, col);
                    var def = columnsLevel[stage].At(col);

                    if (elements.Length == 0 && def.HasChild && def.IsExpanded)
                    {
                        CreateColumnHeader(def, col, stage, def.Span, VColumnHeadersGrid);
                        col += def.Span;
                    }
                    else
                        col++;
                }

                while (pos < VColumnHeadersGrid.ColumnDefinitions.Count)
                {
                    HierarchyDefinition def = columnsLevel[stage].At(pos + scrollPosition);
                    if (def != null && def.HasChild && def.IsExpanded)
                    {
                        CreateColumnHeader(def, pos, stage, def.Span, VColumnHeadersGrid);
                        pos += def.Span;
                    }
                    else
                        pos++;
                }
            }
        }

        /// <summary>
        /// Draws the lower levels of column headers.
        /// </summary>
        /// <param name="leaves">Leaves in the hierarchy definitions composing the headers</param>
        /// <param name="availSpace">Available room for display</param>
        /// <param name="depth">Number of levels in the hierarchy</param>
        /// <param name="scrollPosition">Position of scrollbar as index</param>
        /// <param name="incomplete">Whether or not the hierarchy is fully expanded. True is some elements are hidden.</param>
        private void DrawColumnHeaderLeaves(List<HierarchyDefinition> leaves, double availSpace, int depth, int scrollPosition, bool incomplete = false)
        {
            CreateColumnHeaderRows(depth, incomplete);

            ColumnDefinition cDef;
            LockableToggleButton header;
            double d = 0;
            int idx = scrollPosition + FixedColumns;
            int pos = 0;

            /* FIXED FIRST COLUMN */
            for (int fix = 0; fix < FixedColumns; fix++)
            {
                CreateColumnDefinition(fix, leaves, VColumnHeadersGrid);
                /* Create toggle button acting as header */
                HierarchyDefinition fdef = leaves[fix];
                header = CreateColumnHeader(fdef, pos, depth, VColumnHeadersGrid, incomplete);
                if (!fdef.HasChild)
                {
                    //header.Click += (x, y) =>
                    //{
                    //    fdef.IsHighlighted = !fdef.IsHighlighted;
                    //};
                }

                /* Create and add grid splitter if not on last element */
                if (0 < leaves.Count - 1)
                    CreateColumnSplitter(header, fdef, pos, depth, VColumnHeadersGrid);

                /* Add column to cells */
                cDef = new ColumnDefinition();
                if (fix < leaves.Count - 1)
                    cDef.SharedSizeGroup = "C" + fix;
                VCellsGrid.ColumnDefinitions.Add(cDef);

                d += leaves[fix].Size;
                pos++;
            }
            /* END FIXED */

            while (d < availSpace && idx < leaves.Count)
            {
                CreateColumnDefinition(idx, leaves, VColumnHeadersGrid);

                /* Create toggle button acting as header */
                HierarchyDefinition def = leaves[idx];
                header = CreateColumnHeader(def, pos, depth, VColumnHeadersGrid);
                if (!def.HasChild)
                {
                    //header.Click += (x, y) =>
                    //{
                    //    def.IsHighlighted = !def.IsHighlighted;
                    //};
                }

                /* Create and add grid splitter if not on last element */
                if (idx < leaves.Count - 1)
                    CreateColumnSplitter(header, def, pos, depth, VColumnHeadersGrid);

                /* Add column to cells */
                cDef = new ColumnDefinition();
                if (idx < leaves.Count - 1)
                    cDef.SharedSizeGroup = "C" + idx;
                VCellsGrid.ColumnDefinitions.Add(cDef);

                d += leaves[idx].Size;
                idx++;
                pos++;
            }
        }

        private LockableToggleButton CreateColumnHeader(HierarchyDefinition def, int pos, int depth, Grid parent, bool incomplete = false)
        {
            var header = CreateHeader(def);
            header.LockToggle = !def.CanToggle;
            // Add header in grid
            Grid.SetColumn(header, pos);
            Grid.SetRow(header, def.Level);
            if (depth - def.Level > 0)
                Grid.SetRowSpan(header, depth - def.Level + 1 + (incomplete ? 1 : 0));

            parent.Children.Add(header);

            return header;
        }

        private LockableToggleButton CreateColumnHeader(HierarchyDefinition def, int column, int row, int span, Grid parent)
        {
            var header = CreateHeader(def);
            header.LockToggle = !def.CanToggle;
            Grid.SetColumn(header, column);
            Grid.SetRow(header, row);
            Grid.SetColumnSpan(header, span);
            parent.Children.Add(header);

            return header;
        }

        /// <summary>
        /// Creates the rows in the column headers.
        /// </summary>
        /// <param name="depth">Number of levels in the hierarchy</param>
        /// <param name="incomplete">Whether or not the hierarchy is fully expanded. True is some elements are hidden.</param>
        private void CreateColumnHeaderRows(int depth, bool incomplete = false)
        {
            for (int i = 0; i < depth; i++)
            {
                CreateColumnHeaderRow();
                if (i == depth - 1 && incomplete)
                    CreateColumnHeaderRow();
            }
        }

        private void CreateColumnHeaderRow()
        {
            var rowDef = new RowDefinition();
            rowDef.Height = new GridLength(30, GridUnitType.Pixel);
            VColumnHeadersGrid.RowDefinitions.Add(rowDef);
        }
    }
}