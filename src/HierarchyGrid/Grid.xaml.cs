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
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Unit = System.Reactive.Unit;

namespace HierarchyGrid
{
    public partial class Grid
    {
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
            viewModel.DrawGridInteraction
                .RegisterHandler( ctx =>
                {
                    view.SkiaElement.InvalidateVisual();
                    DrawSplitters( view , viewModel );

                    ctx.SetOutput( Unit.Default );
                } )
                .DisposeWith( disposables );

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

            viewModel.EndEditionInteraction
                .RegisterHandler( ctx =>
                {
                    Clear<TextBox>( view );
                    ctx.SetOutput( Unit.Default );
                } )
                .DisposeWith( disposables );

            view.SkiaElement.Events()
                .PaintSurface
                .Subscribe( args =>
                {
                    SKImageInfo info = args.Info;
                    SKSurface surface = args.Surface;
                    SKCanvas canvas = surface.Canvas;
                    HierarchyGridDrawer.Draw( viewModel , canvas , info.Width , info.Height , false );
                } )
                .DisposeWith( disposables );

            view.SkiaElement.Events()
                .MouseLeave
                .Subscribe( _ =>
                {
                    viewModel.ClearCrosshair();
                } )
                .DisposeWith( disposables );

            view.SkiaElement.Events()
                .MouseMove
                .Subscribe( args =>
                {
                    var position = args.GetPosition( view.SkiaElement );
                    viewModel.HandleMouseOver( position.X , position.Y );
                } )
                .DisposeWith( disposables );

            view.SkiaElement.Events()
                .MouseLeftButtonDown
                .Subscribe( args =>
                {
                    var position = args.GetPosition( view.SkiaElement );
                    if ( args.ClickCount == 2 )
                    {
                        viewModel.HandleDoubleClick( position.X , position.Y );
                    }
                    else
                    {
                        viewModel.HandleMouseDown( position.X , position.Y );
                    }
                } )
                .DisposeWith( disposables );

            view.SkiaElement.Events()
                .MouseRightButtonDown
                .Subscribe( args =>
                {
                    var position = args.GetPosition( view.SkiaElement );
                    viewModel.HandleMouseDown( position.X , position.Y , true );

                    // Show context menu
                    var contextMenu = BuidContextMenu( viewModel );
                    contextMenu.IsOpen = true;
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

        private static ContextMenu BuidContextMenu( HierarchyGridViewModel viewModel )
        {
            var contextMenu = new ContextMenu();

            MenuItem highlightsMenuItem = new() { Header = "Highlights" };
            highlightsMenuItem.Items.Add( new MenuItem
            {
                Header = "Enable crosshair",
                IsChecked = viewModel.EnableCrosshair,
                IsCheckable = true ,
            } );
            highlightsMenuItem.Items.Add( new MenuItem
            {
                Header = "Enable highlights" ,
                IsCheckable = true ,
            } );
            highlightsMenuItem.Items.Add( new MenuItem
            {
                Header = "Clear highlights" ,
            } );

            contextMenu.Items.Add( highlightsMenuItem );
            
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

            foreach ( var c in headers.Where( t => t.Definition is ConsumerDefinition ) )
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
                         Canvas.SetLeft( rect , posX + args.HorizontalChange );
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

                         Canvas.SetTop( rect , posY + args.VerticalChange );
                         Canvas.SetLeft( rect , coord.Left );
                     } ).Subscribe();
                viewModel.ResizeObservables.Enqueue( delta );

                Canvas.SetTop( splitter , coord.Bottom );
                Canvas.SetLeft( splitter , coord.Left );
            }

            var currentX = 0d;
            var currentY =
                viewModel.ColumnsHeadersHeight != null
                ? viewModel.ColumnsHeadersHeight.Take( viewModel.ColumnsHeadersHeight.Length - 1 ).Sum()
                : 0d;
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
                         Canvas.SetLeft( rect , posX + args.HorizontalChange );
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