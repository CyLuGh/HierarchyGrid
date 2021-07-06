using DynamicData;
using HierarchyGrid.Definitions;
using LanguageExt;
using MoreLinq;
using ReactiveUI;
using Splat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace VirtualHierarchyGrid
{
    public partial class HierarchyGridCell : IEnableLogger
    {
        internal static Thickness UnselectedThickness { get; } = new Thickness( 1 );
        internal static Thickness SelectedThickness { get; } = new Thickness( 2 );

        internal static Brush CellBackground { get; set; }
        internal static Brush CellForeground { get; set; }
        internal static Brush CellBorderBrush { get; set; }
        internal static Brush CellSelectedBorder { get; set; }
        internal static Brush CellHighlightBackground { get; set; }
        internal static Brush CellHighlightForeground { get; set; }
        internal static Brush CellHoverBackground { get; set; }
        internal static Brush CellHoverForeground { get; set; }
        internal static Brush CellErrorBackground { get; set; }
        internal static Brush CellErrorForeground { get; set; }
        internal static Brush CellWarningBackground { get; set; }
        internal static Brush CellWarningForeground { get; set; }
        internal static Brush CellRemarkBackground { get; set; }
        internal static Brush CellRemarkForeground { get; set; }
        internal static Brush CellReadOnlyBackground { get; set; }
        internal static Brush CellReadOnlyForeground { get; set; }
        internal static Brush EmptyBrush { get; set; }

        static HierarchyGridCell()
        {
            var rect = new Rectangle();
            CellBackground = (Brush) rect.TryFindResource( "CellBackground" ) ?? Brushes.White;
            CellForeground = (Brush) rect.TryFindResource( "CellForeground" ) ?? Brushes.Black;
            CellBorderBrush = (Brush) rect.TryFindResource( "CellBorder" ) ?? Brushes.DarkGray;

            CellSelectedBorder = (Brush) rect.TryFindResource( "CellSelectedBorder" ) ?? Brushes.BlueViolet;

            CellHighlightBackground = (Brush) rect.TryFindResource( "CellHighlightBackground" ) ?? Brushes.LightBlue;
            CellHighlightForeground = (Brush) rect.TryFindResource( "CellHighlightForeground" ) ?? Brushes.Black;

            CellHoverBackground = (Brush) rect.TryFindResource( "CellHoverBackground" ) ?? Brushes.LightSeaGreen;
            CellHoverForeground = (Brush) rect.TryFindResource( "CellHoverForeground" ) ?? Brushes.Black;

            CellErrorBackground = (Brush) rect.TryFindResource( "CellErrorBackground" ) ?? Brushes.IndianRed;
            CellErrorForeground = (Brush) rect.TryFindResource( "CellErrorForeground" ) ?? Brushes.Black;

            CellWarningBackground = (Brush) rect.TryFindResource( "CellWarningBackground" ) ?? Brushes.YellowGreen;
            CellWarningForeground = (Brush) rect.TryFindResource( "CellWarningForeground" ) ?? Brushes.Black;

            CellRemarkBackground = (Brush) rect.TryFindResource( "CellRemarkBackground" ) ?? Brushes.GreenYellow;
            CellRemarkForeground = (Brush) rect.TryFindResource( "CellRemarkForeground" ) ?? Brushes.Black;

            CellReadOnlyBackground = (Brush) rect.TryFindResource( "CellReadOnlyBackground" ) ?? Brushes.GreenYellow;
            CellReadOnlyForeground = (Brush) rect.TryFindResource( "CellReadOnlyForeground" ) ?? Brushes.Black;

            EmptyBrush = (Brush) rect.TryFindResource( "EmptyBrush" ) ?? Brushes.Transparent;

            rect = null;
        }

        public HierarchyGridCell()
        {
            InitializeComponent();

            this.WhenActivated( disposables =>
             {
                 this.WhenAnyValue( x => x.ViewModel )
                     .WhereNotNull()
                     .Do( vm => PopulateFromViewModel( this , vm , disposables ) )
                     .SubscribeSafe()
                     .DisposeWith( disposables );
             } );
        }

        private static void PopulateFromViewModel( HierarchyGridCell cell , HierarchyGridCellViewModel viewModel , CompositeDisposable disposables )
        {
            cell.OneWayBind( viewModel ,
                vm => vm.ResultSet ,
                v => v.TextBlockResult.Text ,
                r => r?.Result )
                .DisposeWith( disposables );

            cell.OneWayBind( viewModel ,
                vm => vm.HierarchyGridViewModel.TextAlignment ,
                v => v.TextBlockResult.TextAlignment )
                .DisposeWith( disposables );

            cell.OneWayBind( viewModel ,
                vm => vm.Qualifier ,
                v => v.CellBorder.Background ,
                q => q switch
                    {
                        Qualification.Error => CellErrorBackground,
                        Qualification.Warning => CellWarningBackground,
                        Qualification.Remark => CellRemarkBackground,
                        Qualification.ReadOnly => CellReadOnlyBackground,
                        Qualification.Highlighted => CellHighlightBackground,
                        Qualification.Hovered => CellHoverBackground,
                        Qualification.Empty => EmptyBrush,
                        Qualification.Custom => viewModel.ResultSet.BackgroundColor.Some( c => (Brush) new SolidColorBrush( Color.FromArgb( c.a , c.r , c.g , c.b ) ) )
                                                                        .None( () => CellBackground ),
                        _ => CellBackground
                    }
                )
                .DisposeWith( disposables );

            cell.OneWayBind( viewModel ,
                vm => vm.Qualifier ,
                v => v.Foreground ,
                q => q switch
                    {
                        Qualification.Error => CellErrorForeground,
                        Qualification.Warning => CellWarningForeground,
                        Qualification.Remark => CellRemarkForeground,
                        Qualification.ReadOnly => CellReadOnlyForeground,
                        Qualification.Highlighted => CellHighlightForeground,
                        Qualification.Hovered => CellHoverForeground,
                        Qualification.Empty => Brushes.Transparent,
                        Qualification.Custom => viewModel.ResultSet.ForegroundColor.Some( c => (Brush) new SolidColorBrush( Color.FromArgb( c.a , c.r , c.g , c.b ) ) )
                                                                        .None( () => CellForeground ),
                        _ => CellForeground
                    }
                )
                .DisposeWith( disposables );

            cell.OneWayBind( viewModel ,
                vm => vm.IsSelected ,
                v => v.CellBorder.BorderBrush ,
                selected => selected ? CellSelectedBorder : CellBorderBrush )
                .DisposeWith( disposables );

            cell.OneWayBind( viewModel ,
                vm => vm.IsSelected ,
                v => v.CellBorder.BorderThickness ,
                selected => selected ? SelectedThickness : UnselectedThickness )
                .DisposeWith( disposables );

            cell.Events().MouseEnter
                .SubscribeSafe( _ =>
                 {
                     viewModel.IsHovered = true;
                     viewModel.HierarchyGridViewModel.HoveredColumn = viewModel.ColumnIndex;
                     viewModel.HierarchyGridViewModel.HoveredRow = viewModel.RowIndex;
                 } )
                .DisposeWith( disposables );

            cell.Events().MouseLeave
                .SubscribeSafe( _ =>
                 {
                     viewModel.IsHovered = false;
                     viewModel.HierarchyGridViewModel.HoveredColumn = -1;
                     viewModel.HierarchyGridViewModel.HoveredRow = -1;
                 } )
                .DisposeWith( disposables );

            cell.Events().MouseDoubleClick
                .SubscribeSafe( e =>
                 {
                     if ( e.ChangedButton == MouseButton.Left )
                     {
                         if ( !viewModel.HierarchyGridViewModel.EnableMultiSelection )
                             viewModel.HierarchyGridViewModel.SelectedPositions.Clear();
                         viewModel.HierarchyGridViewModel.SelectedPositions.AddOrUpdate( (viewModel.RowIndex, viewModel.ColumnIndex, viewModel.ResultSet) );

                         if ( viewModel.CanEdit )
                             Observable.Return( (viewModel.RowIndex, viewModel.ColumnIndex, viewModel.ResultSet) )
                                       .InvokeCommand( viewModel.HierarchyGridViewModel , x => x.EditCommand );
                     }
                 } )
                .DisposeWith( disposables );

            cell.Events().MouseLeftButtonDown
                .SubscribeSafe( e =>
                 {
                     SelectCell( viewModel , e );
                 } )
                .DisposeWith( disposables );

            viewModel.ShowContextMenuInteraction.RegisterHandler( ctx =>
                 {
                     var contextMenu = new ContextMenu { PlacementTarget = cell };
                     //TODO: Add custom items for cell
                     if ( ctx.Input.Length > 0 )
                     {
                         ctx.Input.ForEach( t =>
                         {
                             var (header, command) = t;
                             contextMenu.Items.Add( new MenuItem { Header = header , Command = command } );
                         } );
                         contextMenu.Items.Add( new Separator() );
                     }

                     CreateDefaultContextMenuItems( viewModel )
                         .ForEach( o => contextMenu.Items.Add( o ) );
                     contextMenu.IsOpen = true;
                     ctx.SetOutput( System.Reactive.Unit.Default );
                 } )
                .DisposeWith( disposables );

            cell.Events().MouseRightButtonDown
                .SubscribeSafe( e =>
                 {
                     SelectCell( viewModel , e );
                     Observable.Return( System.Reactive.Unit.Default )
                         .InvokeCommand( viewModel , x => x.ShowContextMenuCommand );
                 } )
                .DisposeWith( disposables );
        }

        private static IEnumerable<object> CreateDefaultContextMenuItems( HierarchyGridCellViewModel hierarchyGridCellViewModel )
        {
            var enableCrosshairCommand = ReactiveCommand.Create( ()
                 => hierarchyGridCellViewModel.HierarchyGridViewModel.EnableCrosshair
                     = !hierarchyGridCellViewModel.HierarchyGridViewModel.EnableCrosshair );

            /* Highlights */
            var highlightsMenu = new MenuItem
            {
                Header = "Highlights" ,
            };

            highlightsMenu.Items.Add( new MenuItem
            {
                Header = "Enable crosshair" ,
                Command = enableCrosshairCommand ,
                IsCheckable = true ,
                IsChecked = hierarchyGridCellViewModel.HierarchyGridViewModel.EnableCrosshair
            } );

            var clearHighlightsCommand = ReactiveCommand.Create( () => hierarchyGridCellViewModel.HierarchyGridViewModel.Highlights.Clear() );

            highlightsMenu.Items.Add( new MenuItem
            {
                Header = "Clear highlights" ,
                Command = clearHighlightsCommand
            } );

            yield return highlightsMenu;

            /* Expand all */
            yield return new MenuItem
            {
                Header = "Expand all" ,
                Command = ReactiveCommand.Create( () =>
                 {
                     hierarchyGridCellViewModel.HierarchyGridViewModel.ProducersCache.Items.FlatList().ForEach( x => x.IsExpanded = true );
                     hierarchyGridCellViewModel.HierarchyGridViewModel.ConsumersCache.Items.FlatList().ForEach( x => x.IsExpanded = true );
                     Observable.Return( System.Reactive.Unit.Default ).InvokeCommand( hierarchyGridCellViewModel.HierarchyGridViewModel , vm => vm.DrawGridCommand );
                 } )
            };

            /* Collapse all */
            yield return new MenuItem
            {
                Header = "Collapse all" ,
                Command = ReactiveCommand.Create( () =>
                 {
                     hierarchyGridCellViewModel.HierarchyGridViewModel.ProducersCache.Items.FlatList().ForEach( x => x.IsExpanded = false );
                     hierarchyGridCellViewModel.HierarchyGridViewModel.ConsumersCache.Items.FlatList().ForEach( x => x.IsExpanded = false );
                     Observable.Return( System.Reactive.Unit.Default ).InvokeCommand( hierarchyGridCellViewModel.HierarchyGridViewModel , vm => vm.DrawGridCommand );
                 } )
            };

            /* Reset cells to default dimensions */
            yield return new MenuItem
            {
                Header = "Reset cells dimensions" ,
                Command = ReactiveCommand.Create( () =>
                 {
                     hierarchyGridCellViewModel.HierarchyGridViewModel.RowsHeadersWidth =
                         hierarchyGridCellViewModel.HierarchyGridViewModel.RowsHeadersWidth.Select( _ => HierarchyGridViewModel.DEFAULT_HEADER_WIDTH ).ToArray();

                     hierarchyGridCellViewModel.HierarchyGridViewModel.ColumnsHeadersHeight =
                         hierarchyGridCellViewModel.HierarchyGridViewModel.ColumnsHeadersHeight.Select( _ => HierarchyGridViewModel.DEFAULT_HEADER_HEIGHT ).ToArray();

                     var keys = hierarchyGridCellViewModel.HierarchyGridViewModel.ColumnsWidths.Keys.ToArray();
                     keys.ForEach( k => hierarchyGridCellViewModel.HierarchyGridViewModel.ColumnsWidths[k] = HierarchyGridViewModel.DEFAULT_COLUMN_WIDTH );

                     keys = hierarchyGridCellViewModel.HierarchyGridViewModel.RowsHeights.Keys.ToArray();
                     keys.ForEach( k => hierarchyGridCellViewModel.HierarchyGridViewModel.RowsHeights[k] = HierarchyGridViewModel.DEFAULT_ROW_HEIGHT );

                     Observable.Return( System.Reactive.Unit.Default ).InvokeCommand( hierarchyGridCellViewModel.HierarchyGridViewModel , vm => vm.DrawGridCommand );
                 } )
            };

            yield return new Separator();

            var clipboardMenuItem = new MenuItem { Header = "Copy to clipboard" };
            clipboardMenuItem.Items.Add( new MenuItem
            {
                Header = "With tree structure" ,
                Command = hierarchyGridCellViewModel.HierarchyGridViewModel.CopyGridCommand ,
                CommandParameter = true
            } );
            clipboardMenuItem.Items.Add( new MenuItem
            {
                Header = "Without tree structure" ,
                Command = hierarchyGridCellViewModel.HierarchyGridViewModel.CopyGridCommand ,
                CommandParameter = false
            } );

            /*
             * with tree structure
             * without tree structure
             * highlights
             * selection
             */
            yield return clipboardMenuItem;
            var exportMenuItem = new MenuItem { Header = "Export" };
            exportMenuItem.Items.Add( new MenuItem
            {
                Header = "CSV temporary file" ,
                Command = hierarchyGridCellViewModel.HierarchyGridViewModel.ExportCsvFileCommand
            } );
            /*
             * excel
             * excel temp file
             * csv temp file
             */
            yield return exportMenuItem;
        }

        private static void SelectCell( HierarchyGridCellViewModel vm , MouseButtonEventArgs e )
        {
            vm.HierarchyGridViewModel.IsEditing = false;
            if ( e.ClickCount == 1 )
            {
                if ( vm.IsSelected )
                {
                    if ( Keyboard.Modifiers == ModifierKeys.Control )
                        vm.HierarchyGridViewModel.SelectedPositions.Remove( (vm.RowIndex, vm.ColumnIndex) );
                }
                else
                {
                    if ( !vm.HierarchyGridViewModel.EnableMultiSelection )
                        vm.HierarchyGridViewModel.SelectedPositions.Clear();

                    vm.HierarchyGridViewModel.SelectedPositions.AddOrUpdate( (vm.RowIndex, vm.ColumnIndex, vm.ResultSet) );
                }
            }
        }
    }
}