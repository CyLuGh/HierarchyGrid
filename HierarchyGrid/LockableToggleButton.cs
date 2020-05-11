using System.Windows;
using System.Windows.Controls.Primitives;

namespace HierarchyGrid
{
    internal class LockableToggleButton : ToggleButton
    {
        public bool LockToggle
        {
            get { return (bool)GetValue(LockToggleProperty); }
            set { SetValue(LockToggleProperty, value); }
        }

        public static readonly DependencyProperty LockToggleProperty =
            DependencyProperty.Register("LockToggle", typeof(bool), typeof(LockableToggleButton), new UIPropertyMetadata(false));

        protected override void OnToggle()
        {
            if (!LockToggle)
                base.OnToggle();
        }
    }
}