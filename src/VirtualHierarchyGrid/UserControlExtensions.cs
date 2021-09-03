using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace VirtualHierarchyGrid
{
    public static class UserControlExtensions
    {
        public static IEnumerable<T> FindVisualChildren<T>(this DependencyObject @this) where T : DependencyObject
        {
            if (@this != null)
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(@this); i++)
                {
                    var child = VisualTreeHelper.GetChild(@this, i);
                    if (child != null && child is T)
                        yield return (T)child;

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
        }

        public static T GetVisualParent<T>(this DependencyObject @this) where T : DependencyObject
        {
            var parent = @this;

            while (parent != null)
            {
                parent = VisualTreeHelper.GetParent(parent);
                if (parent is T visualParent)
                    return visualParent;
            }

            return null;
        }
    }
}