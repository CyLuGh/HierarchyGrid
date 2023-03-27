using HierarchyGrid.Definitions;
using HierarchyGrid.Skia;
using LanguageExt;
using LanguageExt.Pipes;
using ReactiveMarbles.ObservableEvents;
using ReactiveUI;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
using TextCopy;
using WpfScreenHelper;
using Unit = System.Reactive.Unit;

namespace HierarchyGrid
{
    public partial class Grid
    {
        private readonly ToolTip _tooltip = new();

        public Grid()
        {
            InitializeComponent();

            this.WhenActivated( disposables =>
            {
                this.WhenAnyValue( x => x.ViewModel )
                    .WhereNotNull()
                    .Do( vm => PopulateFromViewModel( this , vm , disposables ) )
                    .Subscribe()
                    .DisposeWith( disposables );
            } );
        }

        private static void PopulateFromViewModel( Grid view , HierarchyGridViewModel viewModel , CompositeDisposable disposables )
        {
            ApplyDependencyProperties( view , viewModel );

            viewModel.DrawGridInteraction
                .RegisterHandler( ctx =>
                {
                    view.SkiaElement.InvalidateVisual();
                    DrawSplitters( view , viewModel );

                    ctx.SetOutput( Unit.Default );
                } )
                .DisposeWith( disposables );

            viewModel.FillClipboardInteraction
                .RegisterHandler( async ctx =>
                {
                    await ClipboardService.SetTextAsync( ctx.Input );
                    ctx.SetOutput( Unit.Default );
                } )
                .DisposeWith( disposables );

            RegisterStartEditionInteraction( view , viewModel , disposables );
            RegisterEndEditionInteraction( view , viewModel , disposables );
            RegisterToolTipInteractions( view , viewModel , disposables );

            view.SkiaElement.Events()
                .PaintSurface
                .Subscribe( args =>
                {
                    SKImageInfo info = args.Info;
                    SKSurface surface = args.Surface;
                    SKCanvas canvas = surface.Canvas;

                    // TODO: Try to find the UI scaling that's applied in Display settings

                    // var screen = Screen.FromHandle( new WindowInteropHelper( Window.GetWindow( view ) ).Handle );
                    // var scale = screen.WorkingArea.Width / screen.Bounds.Width;
                    var scale = view.ScreenScale;

                    HierarchyGridDrawer.Draw( viewModel , canvas , info.Width , info.Height , scale , false );
                } )
                .DisposeWith( disposables );

            view.SkiaElement.Events()
                .MouseLeave
                .Subscribe( _ =>
                {
                    viewModel.HandleMouseLeft();
                } )
                .DisposeWith( disposables );

            view.SkiaElement.Events()
                .MouseMove
                .Subscribe( args =>
                {
                    var position = args.GetPosition( view.SkiaElement );
                    viewModel.HandleMouseOver( position.X , position.Y , view.ScreenScale );
                } )
                .DisposeWith( disposables );

            view.SkiaElement.Events()
                .MouseLeftButtonDown
                .Subscribe( args =>
                {
                    var position = args.GetPosition( view.SkiaElement );
                    if ( args.ClickCount == 2 )
                    {
                        viewModel.HandleDoubleClick( position.X , position.Y , view.ScreenScale );
                    }
                    else
                    {
                        var ctrl = Keyboard.IsKeyDown( Key.LeftCtrl ) || Keyboard.IsKeyDown( Key.RightCtrl );
                        var shift = Keyboard.IsKeyDown( Key.LeftShift ) || Keyboard.IsKeyDown( Key.RightShift );

                        viewModel.HandleMouseDown( position.X , position.Y , shift , ctrl , screenScale: view.ScreenScale );
                    }

                    args.Handled = true;
                } )
                .DisposeWith( disposables );

            view.SkiaElement.Events()
                .MouseRightButtonDown
                .Subscribe( args =>
                {
                    var position = args.GetPosition( view.SkiaElement );
                    viewModel.HandleMouseDown( position.X , position.Y , false , false , true , view.ScreenScale );

                    // Show context menu
                    if ( viewModel.IsValid && viewModel.HasData )
                    {
                        var contextMenu = BuildContextMenu( viewModel , position.X , position.Y , view.ScreenScale );
                        contextMenu.IsOpen = true;
                    }
                } )
                .DisposeWith( disposables );

            view.SkiaElement.Events()
                .MouseWheel
                .Subscribe( e =>
                {
                    if ( Keyboard.IsKeyDown( Key.LeftCtrl ) || Keyboard.IsKeyDown( Key.RightCtrl ) )
                        viewModel.Scale += .05 * ( e.Delta < 0 ? 1 : -1 );
                    else if ( Keyboard.IsKeyDown( Key.LeftShift ) || Keyboard.IsKeyDown( Key.RightShift ) )
                        viewModel.HorizontalOffset += 5 * ( e.Delta < 0 ? 1 : -1 );
                    else
                        viewModel.VerticalOffset += 5 * ( e.Delta < 0 ? 1 : -1 );
                } )
                .DisposeWith( disposables );

            view.Bind( viewModel ,
                vm => vm.HorizontalOffset ,
                v => v.HorizontalScrollBar.Value ,
                view.HorizontalScrollBar.Events().Scroll ,
                vmToViewConverter: i => Convert.ToDouble( i ) ,
                viewToVmConverter: d => Convert.ToInt32( d ) )
                .DisposeWith( disposables );

            view.Bind( viewModel ,
                vm => vm.VerticalOffset ,
                v => v.VerticalScrollBar.Value ,
                view.VerticalScrollBar.Events().Scroll ,
                vmToViewConverter: i => Convert.ToDouble( i ) ,
                viewToVmConverter: d => Convert.ToInt32( d ) )
                .DisposeWith( disposables );

            view.OneWayBind( viewModel ,
                vm => vm.MaxHorizontalOffset ,
                v => v.HorizontalScrollBar.Maximum )
                .DisposeWith( disposables );

            view.OneWayBind( viewModel ,
                vm => vm.MaxVerticalOffset ,
                v => v.VerticalScrollBar.Maximum )
                .DisposeWith( disposables );

            view.SkiaElement.InvalidateVisual();
        }

        private static void ApplyDependencyProperties( Grid view , HierarchyGridViewModel viewModel )
        {
            viewModel.DefaultColumnWidth = view.DefaultColumnWidth;
            viewModel.DefaultRowHeight = view.DefaultRowHeight;
            viewModel.DefaultHeaderHeight = view.DefaultHeaderHeight;
            viewModel.DefaultHeaderWidth = view.DefaultHeaderWidth;
            viewModel.StatusMessage = view.StatusMessage;
            viewModel.EnableCrosshair = view.EnableCrosshair;
        }

        private static void RegisterStartEditionInteraction( Grid view , HierarchyGridViewModel viewModel , CompositeDisposable disposables )
        {
            viewModel.StartEditionInteraction
                .RegisterHandler( ctx =>
                {
                    var cell = ctx.Input;

                    cell.ResultSet.Editor.IfSome( editor =>
                    {
                        Clear<TextBox>( view );
                        var textBox = new TextBox
                        {
                            Width = cell.Width ,
                            Height = cell.Height ,
                            VerticalContentAlignment = System.Windows.VerticalAlignment.Center ,
                            TextAlignment = System.Windows.TextAlignment.Right
                        };

                        Canvas.SetLeft( textBox , cell.Left );
                        Canvas.SetTop( textBox , cell.Top );

                        textBox.Events()
                            .KeyDown
                            .Subscribe( e =>
                            {
                                switch ( e.Key )
                                {
                                    case Key.Escape:
                                        Observable.Return( false )
                                            .InvokeCommand( viewModel.EndEditionCommand );
                                        break;

                                    case Key.Enter:
                                        Observable.Return( false )
                                            .InvokeCommand( viewModel.EndEditionCommand );
                                        Observable.Return( editor( textBox.Text ) )
                                            .InvokeCommand( viewModel.DrawGridCommand );
                                        break;
                                }
                            } )
                            .DisposeWith( disposables );

                        view.Canvas.Children.Add( textBox );
                        textBox.Focus();
                    } );

                    ctx.SetOutput( Unit.Default );
                } )
                .DisposeWith( disposables );
        }

        private static void RegisterToolTipInteractions( Grid view , HierarchyGridViewModel viewModel , CompositeDisposable disposables )
        {
            viewModel.ShowTooltipInteraction
                .RegisterHandler( ctx =>
                {
                    view._tooltip.IsOpen = false;

                    ctx.Input.ResultSet.TooltipText
                        .IfSome( text =>
                        {
                            view._tooltip.Content = text;
                            view._tooltip.Placement = System.Windows.Controls.Primitives.PlacementMode.Mouse;
                            view._tooltip.IsOpen = true;
                        } );
                    ctx.SetOutput( Unit.Default );
                } )
                .DisposeWith( disposables );

            viewModel.CloseTooltipInteraction
                .RegisterHandler( ctx =>
                {
                    view._tooltip.IsOpen = false;
                    ctx.SetOutput( Unit.Default );
                } )
                .DisposeWith( disposables );
        }

        private static void RegisterEndEditionInteraction( Grid view , HierarchyGridViewModel viewModel , CompositeDisposable disposables )
        {
            viewModel.EndEditionInteraction
                .RegisterHandler( ctx =>
                {
                    Clear<TextBox>( view );
                    ctx.SetOutput( Unit.Default );
                } )
                .DisposeWith( disposables );
        }

        private static IEnumerable<MenuItem> BuildCustomItems( (string, ICommand)[] commands )
        {
            var items = new Dictionary<(int, string) , MenuItem>();

            foreach ( var t in commands )
            {
                var (header, command) = t;
                var splits = header.Split( '|' );

                if ( splits.Length == 1 )
                {
                    yield return new MenuItem { Header = header , Command = command };
                }
                else
                {
                    MenuItem? parent = null;
                    for ( int i = 0 ; i < splits.Length ; i++ )
                    {
                        if ( i == splits.Length - 1 && parent != null )
                        {
                            parent.Items.Add( new MenuItem { Header = splits[i] , Command = command } );
                        }
                        else
                        {
                            if ( items.TryGetValue( (0, splits[i]) , out var mi ) )
                            {
                                parent = mi;
                            }
                            else
                            {
                                var menuItem = new MenuItem { Header = splits[i] };
                                if ( parent != null )
                                    parent.Items.Add( menuItem );

                                parent = menuItem;
                                items.Add( (i, splits[i]) , menuItem );
                            }
                        }
                    }
                }
            }

            foreach ( var i in items.Values.Where( x => x.Parent == null ) )
                yield return i;
        }

        private static ContextMenu BuildContextMenu( HierarchyGridViewModel viewModel , double x , double y , double screenScale )
        {
            var coord = viewModel.FindCoordinates( x , y , screenScale );
            var contextMenu = new ContextMenu();

            var items = coord.Match( r =>
                r.Match( c =>
                    c.ResultSet.ContextCommands.Match(
                        cmds => BuildCustomItems( cmds ).ToArray() ,
                        () => Array.Empty<MenuItem>() ) ,
                    () => Array.Empty<MenuItem>() ) ,
                _ => Array.Empty<MenuItem>() );

            if ( items.Length > 0 )
            {
                foreach ( var i in items )
                    contextMenu.Items.Add( i );
                contextMenu.Items.Add( new Separator() );
            }

            MenuItem highlightsMenuItem = new() { Header = "Highlights" };
            highlightsMenuItem.Items.Add( new MenuItem
            {
                Header = "Enable crosshair" ,
                IsChecked = viewModel.EnableCrosshair ,
                IsCheckable = true ,
                Command = viewModel.ToggleCrosshairCommand
            } );
            highlightsMenuItem.Items.Add( new MenuItem
            {
                Header = "Clear highlights" ,
                Command = viewModel.ClearHighlightsCommand
            } );

            contextMenu.Items.Add( highlightsMenuItem );

            contextMenu.Items.Add( new MenuItem
            {
                Header = "Clear selection" ,
                Command = ReactiveCommand.Create( () => viewModel.SelectedCells.Clear() )
            } );

            contextMenu.Items.Add( new MenuItem
            {
                Header = "Expand all" ,
                Command = viewModel.ToggleStatesCommand ,
                CommandParameter = true
            } );
            contextMenu.Items.Add( new MenuItem
            {
                Header = "Collapse all" ,
                Command = viewModel.ToggleStatesCommand ,
                CommandParameter = false
            } );
            //contextMenu.Items.Add( new MenuItem
            //{
            //    Header = "Transposed" ,
            //    IsChecked = viewModel.IsTransposed ,
            //    IsCheckable = true ,
            //    Command = viewModel.ToggleTransposeCommand
            //} );

            contextMenu.Items.Add( new Separator() );

            MenuItem copyMenuItem = new() { Header = "Copy to clipboard" };
            copyMenuItem.Items.Add( new MenuItem { Header = "with tree structure" , Command = viewModel.CopyToClipboardCommand , CommandParameter = CopyMode.Structure } );
            copyMenuItem.Items.Add( new MenuItem { Header = "without tree structure" , Command = viewModel.CopyToClipboardCommand , CommandParameter = CopyMode.Flat } );
            copyMenuItem.Items.Add( new MenuItem { Header = "highlighted elements" , Command = viewModel.CopyToClipboardCommand , CommandParameter = CopyMode.Highlights } );
            copyMenuItem.Items.Add( new MenuItem { Header = "selection" , Command = viewModel.CopyToClipboardCommand , CommandParameter = CopyMode.Selection } );
            contextMenu.Items.Add( copyMenuItem );

            return contextMenu;
        }

        private static void DrawSplitters( Grid view , HierarchyGridViewModel viewModel )
        {
            /* Dispose previous resize events */
            foreach ( var disposables in viewModel.ResizeObservables )
                disposables.Dispose();

            viewModel.ResizeObservables.Clear();

            var splitters = view.Canvas.Children.OfType<GridSplitter>().ToArray();

            GridSplitter GetSplitter( int idx )
            {
                if ( idx < splitters.Length )
                {
                    return splitters[idx];
                }
                else
                {
                    var splitter = new GridSplitter
                    {
                        BorderThickness = new Thickness( 2d ) ,
                        BorderBrush = Brushes.Transparent ,
                    };
                    view.Canvas.Children.Add( splitter );
                    return splitter;
                }
            }

            int splitterCount = 0;

            var headers = viewModel.HeadersCoordinates
                                    .Where( x => x.Definition.Count() == 1 )
                                    .ToArray();

            foreach ( var c in
                headers.Where( t => t.Definition is ConsumerDefinition ) )
            {
                var (coord, def) = c;
                var splitter = GetSplitter( splitterCount++ );
                splitter.Height = coord.Height;
                splitter.Width = 2;
                splitter.ResizeDirection = GridResizeDirection.Columns;
                var dsp = splitter.Events()
                    .DragCompleted
                    .Subscribe( args =>
                    {
                        var pos = viewModel.ColumnsDefinitions.GetPosition( def );
                        viewModel.ColumnsWidths[pos] = Math.Max( viewModel.ColumnsWidths[pos] + args.HorizontalChange , 10d );
                        Clear<Rectangle>( view );
                    } );
                viewModel.ResizeObservables.Enqueue( dsp );

                var posX = coord.Right;
                var delta = splitter.Events()
                     .DragDelta
                     .Do( args =>
                     {
                         Clear<Rectangle>( view );
                         var rect = new Rectangle
                         {
                             Fill = Brushes.DarkSlateGray ,
                             Height = coord.Height ,
                             Width = 2d
                         };
                         view.Canvas.Children.Add( rect );

                         Canvas.SetTop( rect , coord.Top );
                         Canvas.SetLeft( rect , ( posX + args.HorizontalChange ) );
                     } ).Subscribe();
                viewModel.ResizeObservables.Enqueue( delta );

                Canvas.SetTop( splitter , coord.Top );
                Canvas.SetLeft( splitter , coord.Right );
            }

            foreach ( var p in headers.Where( t => t.Definition is ProducerDefinition ) )
            {
                var (coord, def) = p;
                var splitter = GetSplitter( splitterCount++ );
                splitter.Height = 2;
                splitter.Width = coord.Width;
                splitter.ResizeDirection = GridResizeDirection.Rows;
                var dsp = splitter.Events()
                    .DragCompleted
                    .Subscribe( args =>
                    {
                        var pos = viewModel.RowsDefinitions.GetPosition( def );
                        viewModel.RowsHeights[pos] = Math.Max( viewModel.RowsHeights[pos] + args.VerticalChange , 10d );
                        Clear<Rectangle>( view );
                    } );
                viewModel.ResizeObservables.Enqueue( dsp );

                var posY = coord.Bottom;
                var delta = splitter.Events()
                     .DragDelta
                     .Do( args =>
                     {
                         Clear<Rectangle>( view );
                         var rect = new Rectangle
                         {
                             Fill = Brushes.DarkSlateGray ,
                             Height = 2d ,
                             Width = coord.Width
                         };
                         view.Canvas.Children.Add( rect );

                         Canvas.SetTop( rect , ( posY + args.VerticalChange ) );
                         Canvas.SetLeft( rect , coord.Left );
                     } ).Subscribe();
                viewModel.ResizeObservables.Enqueue( delta );

                Canvas.SetTop( splitter , coord.Bottom );
                Canvas.SetLeft( splitter , coord.Left );
            }

            var currentX = 0d;
            var currentY = ( viewModel.ColumnsHeadersHeight?.Take( viewModel.ColumnsHeadersHeight.Length - 1 ).Sum() ) ?? 0d;
            var height = viewModel.ColumnsHeadersHeight?.LastOrDefault( 0d ) ?? 0d;
            for ( int i = 0 ; i < viewModel.RowsHeadersWidth?.Length ; i++ )
            {
                var currentIndex = i;
                var width = viewModel.RowsHeadersWidth[currentIndex];
                var splitter = GetSplitter( splitterCount++ );
                splitter.Height = height;
                splitter.Width = 2;
                splitter.ResizeDirection = GridResizeDirection.Columns;
                currentX += width;

                var dsp = splitter.Events().DragCompleted
                    .Do( args =>
                    {
                        viewModel.RowsHeadersWidth[currentIndex] =
                            Math.Max( viewModel.RowsHeadersWidth[currentIndex] + args.HorizontalChange , 10d );
                        Clear<Rectangle>( view );
                    } )
                    .Select( _ => false )
                    .InvokeCommand( viewModel , x => x.DrawGridCommand );
                viewModel.ResizeObservables.Enqueue( dsp );

                var posX = currentX;
                var delta = splitter.Events()
                     .DragDelta
                     .Do( args =>
                     {
                         Clear<Rectangle>( view );
                         var rect = new Rectangle
                         {
                             Fill = Brushes.DarkSlateGray ,
                             Height = height ,
                             Width = 2d
                         };
                         view.Canvas.Children.Add( rect );

                         Canvas.SetTop( rect , currentY );
                         Canvas.SetLeft( rect , ( posX + args.HorizontalChange ) );
                     } ).Subscribe();
                viewModel.ResizeObservables.Enqueue( delta );

                Canvas.SetTop( splitter , currentY );
                Canvas.SetLeft( splitter , currentX );
            }

            var exceeding = splitters.Skip( splitterCount ).ToArray();
            Clear( view , exceeding );
        }

        private static void Clear<T>( Grid view ) where T : UIElement
        {
            foreach ( var o in view.Canvas.Children.OfType<T>().ToArray() )
                view.Canvas.Children.Remove( o );
        }

        private static void Clear<T>( Grid view , IEnumerable<T> items ) where T : UIElement
        {
            foreach ( var o in items )
                view.Canvas.Children.Remove( o );
        }
    }
}