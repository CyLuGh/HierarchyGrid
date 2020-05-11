using HierarchyGrid.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HierarchyGrid
{
    partial class VirtualHierarchyGrid
    {
        private void DrawGlobalHeaders()
        {
            GridHeaders.Children.Clear();
            GridHeaders.ColumnDefinitions.Clear();
            GridHeaders.RowDefinitions.Clear();

            //DrawGlobalColumnHeaders();
            //DrawGlobalRowHeaders();
            //DrawGlobalCornerHeader();
        }

        /// <summary>
        /// Creates a last level node, either for a leaf in the hierarchy or a folded element.
        /// </summary>
        /// <param name="hdef">Hierarchy definition of the element to be displayed</param>
        /// <returns>Properly created toggle button, with correct style</returns>
        private LockableToggleButton CreateHeader(HierarchyDefinition hdef)
        {
            var header = new LockableToggleButton()
            {
                Content = hdef.Content,
                DataContext = hdef,
                Tag = hdef,
                IsThreeState = false,
                IsChecked = hdef.IsExpanded,
                //ToolTip = hdef.ToolTip,
                Focusable = false
            };

            //header.MouseEnter += (x, y) => { hdef.IsHovered = false; hdef.IsContentHovered = EnableCrosshair; };
            //header.MouseLeave += (x, y) => hdef.IsHovered = hdef.IsContentHovered = false;

            if (hdef.HasChild)
            {
                // Folded node
                //header.Style = (Style)FindResource("HierarchyToggle");
                //header.Checked += Expand;
                //header.Unchecked += Collapse;
            }
            else
            {
                // Leaf
                //header.Style = (Style)FindResource("HierarchyLeaf");
            }

            return header;
        }

        /// <summary>
        /// Redraws the header part of the grid.
        /// </summary>
        /// <param name="defs">Hierarchy definitions composing the headers</param>
        /// <param name="availSpace">Available room for display</param>
        /// <param name="scrollPosition">Position of scrollbar as index</param>
        /// <param name="isColumns">Whether the headers are columns or rows</param>
        private void DrawHeaders(HierarchyDefinition[] defs, Dictionary<int, LinkedList<HierarchyDefinition>> levels, double availSpace, int scrollPosition, bool isColumns)
        {
            if (defs == null || defs.Length == 0)
                return;

            int depth = defs.TotalDepth(false); /* Levels in the hierarchy */
            int maxDepth = defs.TotalDepth(true);

            List<HierarchyDefinition> leaves = defs.Leaves().ToList();

            if (isColumns)
            {
                DrawColumnHeaderLeaves(leaves, availSpace, depth, scrollPosition, maxDepth != depth);
                DrawColumnHeaderExpanded(scrollPosition, levels);
            }
            else
            {
                DrawRowHeaderLeaves(leaves, availSpace, depth, scrollPosition, maxDepth != depth);
                DrawRowHeaderExpanded(scrollPosition, levels);
            }
        }
    }
}