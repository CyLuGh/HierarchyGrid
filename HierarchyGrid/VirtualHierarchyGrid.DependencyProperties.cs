using System.Windows;

namespace HierarchyGrid
{
    partial class VirtualHierarchyGrid
    {
        public double DefaultRowHeight
        {
            get { return (double)GetValue(DefaultRowHeightProperty); }
            set { SetValue(DefaultRowHeightProperty, value); }
        }

        // Using a DependencyProperty as the backing store for DefaultRowHeight.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DefaultRowHeightProperty =
            DependencyProperty.Register("DefaultRowHeight", typeof(double), typeof(VirtualHierarchyGrid), new FrameworkPropertyMetadata(25d));

        public double DefaultColumnWidth
        {
            get { return (double)GetValue(DefaultColumnWidthProperty); }
            set { SetValue(DefaultColumnWidthProperty, value); }
        }

        // Using a DependencyProperty as the backing store for DefaultColumnWidth.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DefaultColumnWidthProperty =
            DependencyProperty.Register("DefaultColumnWidth", typeof(double), typeof(VirtualHierarchyGrid), new FrameworkPropertyMetadata(100d));

        public double MinColumnWidth
        {
            get { return (double)GetValue(MinColumnWidthProperty); }
            set { SetValue(MinColumnWidthProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MinColumnWidth.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MinColumnWidthProperty =
            DependencyProperty.Register("MinColumnWidth", typeof(double), typeof(VirtualHierarchyGrid), new FrameworkPropertyMetadata(60d));

        public double DefaultRowHeaderWidth
        {
            get { return (double)GetValue(DefaultRowHeaderWidthProperty); }
            set { SetValue(DefaultRowHeaderWidthProperty, value); }
        }

        // Using a DependencyProperty as the backing store for DefaultRowHeaderWidth.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DefaultRowHeaderWidthProperty =
            DependencyProperty.Register("DefaultRowHeaderWidth", typeof(double), typeof(VirtualHierarchyGrid), new FrameworkPropertyMetadata(70d));
    }
}