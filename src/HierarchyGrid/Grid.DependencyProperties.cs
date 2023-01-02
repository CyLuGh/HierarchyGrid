using System.Windows;

namespace HierarchyGrid
{
    public partial class Grid
    {
        public int DefaultColumnWidth
        {
            get { return (int) GetValue( DefaultColumnWidthProperty ); }
            set { SetValue( DefaultColumnWidthProperty , value ); }
        }

        // Using a DependencyProperty as the backing store for DefaultColumnWidth.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DefaultColumnWidthProperty =
            DependencyProperty.Register( "DefaultColumnWidth" , typeof( int ) , typeof( Grid ) , new FrameworkPropertyMetadata( 120 ) );

        public int DefaultRowHeight
        {
            get { return (int) GetValue( DefaultRowHeightProperty ); }
            set { SetValue( DefaultRowHeightProperty , value ); }
        }

        // Using a DependencyProperty as the backing store for DefaultRowHeight.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DefaultRowHeightProperty =
            DependencyProperty.Register( "DefaultRowHeight" , typeof( int ) , typeof( Grid ) , new FrameworkPropertyMetadata( 30 ) );

        public int DefaultHeaderWidth
        {
            get { return (int) GetValue( DefaultHeaderWidthProperty ); }
            set { SetValue( DefaultHeaderWidthProperty , value ); }
        }

        // Using a DependencyProperty as the backing store for DefaultHeaderWidth.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DefaultHeaderWidthProperty =
            DependencyProperty.Register( "DefaultHeaderWidth" , typeof( int ) , typeof( Grid ) , new FrameworkPropertyMetadata( 80 ) );

        public int DefaultHeaderHeight
        {
            get { return (int) GetValue( DefaultHeaderHeightProperty ); }
            set { SetValue( DefaultHeaderHeightProperty , value ); }
        }

        // Using a DependencyProperty as the backing store for DefaultHeaderHeight.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DefaultHeaderHeightProperty =
            DependencyProperty.Register( "DefaultHeaderHeight" , typeof( int ) , typeof( Grid ) , new FrameworkPropertyMetadata( 30 ) );

        public string? StatusMessage
        {
            get { return (string) GetValue( StatusMessageProperty ); }
            set { SetValue( StatusMessageProperty , value ); }
        }

        // Using a DependencyProperty as the backing store for StatusMessage.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty StatusMessageProperty =
            DependencyProperty.Register( "StatusMessage" , typeof( string ) , typeof( Grid ) , new FrameworkPropertyMetadata( null ) );

        public bool EnableCrosshair
        {
            get { return (bool) GetValue( EnableCrosshairProperty ); }
            set { SetValue( EnableCrosshairProperty , value ); }
        }

        // Using a DependencyProperty as the backing store for EnableCrosshair.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty EnableCrosshairProperty =
            DependencyProperty.Register( "EnableCrosshair" , typeof( bool ) , typeof( Grid ) , new FrameworkPropertyMetadata( false ) );
    }
}