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
        private ToggleButton GetColumnHeader(int idx)
        {
            ToggleButton cH = null;
            for (int i = VColumnHeadersGrid.RowDefinitions.Count - 1; i >= 0; i--)
            {
                cH = VColumnHeadersGrid.Children.OfType<ToggleButton>()
                    .FirstOrDefault(e => Grid.GetColumn(e) == idx && Grid.GetRow(e) == i);
                if (cH != null)
                    break;
            }

            return cH;
        }

        private ToggleButton GetColumnHeader(UIElement e)
        {
            return GetColumnHeader(Grid.GetColumn(e));
        }

        private ToggleButton GetColumnHeader(HierarchyDefinition def)
        {
            return GetHeader(VColumnHeadersGrid, def);
        }

        private ToggleButton GetRowHeader(int idx)
        {
            ToggleButton rH = null;
            for (int i = VRowHeadersGrid.ColumnDefinitions.Count - 1; i >= 0; i--)
            {
                rH = VRowHeadersGrid.Children.OfType<ToggleButton>()
                    .FirstOrDefault(e => Grid.GetColumn(e) == i && Grid.GetRow(e) == idx);
                if (rH != null)
                    break;
            }

            return rH;
        }

        private ToggleButton GetRowHeader(UIElement e)
        {
            return GetRowHeader(Grid.GetRow(e));
        }

        private ToggleButton GetRowHeader(HierarchyDefinition def)
        {
            return GetHeader(VRowHeadersGrid, def);
        }

        private ToggleButton GetHeader(Grid grid, HierarchyDefinition def)
        {
            return grid.Children.OfType<ToggleButton>().FirstOrDefault(e => e.Tag is HierarchyDefinition && (HierarchyDefinition)e.Tag == def);
        }
    }
}