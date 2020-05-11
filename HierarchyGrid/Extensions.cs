using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace HierarchyGrid
{
    public static class Extensions
    {
        public static UIElement[] GetElements(this Grid grid, int row, int column)
            => grid.Children.OfType<UIElement>().Where(x => Grid.GetRow(x) == row && Grid.GetColumn(x) == column).ToArray();

        public static int Add(this Grid grid, UIElement element, int row, int column, int rowSpan = 0, int columnSpan = 0)
        {
            Grid.SetRow(element, row);
            Grid.SetColumn(element, column);

            if (rowSpan != 0)
                Grid.SetRowSpan(element, rowSpan);

            if (columnSpan != 0)
                Grid.SetColumnSpan(element, columnSpan);

            return grid.Children.Add(element);
        }
    }
}