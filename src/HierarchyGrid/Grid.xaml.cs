using HierarchyGrid.Definitions;
using HierarchyGrid.Skia;
using LanguageExt;
using ReactiveMarbles.ObservableEvents;
using ReactiveUI;
using SkiaSharp;
using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Controls;
using System.Windows.Input;
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
                    ctx.SetOutput( Unit.Default );
                } )
                .DisposeWith( disposables );

            viewModel.StartEditionInteraction
                .RegisterHandler( ctx =>
                {
                    var cell = ctx.Input;

                    cell.ResultSet.Editor.IfSome( editor =>
                    {
                        ClearTextBoxes( view );
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
                                    case System.Windows.Input.Key.Escape:
                                        Observable.Return( false )
                                            .InvokeCommand( viewModel.EndEditionCommand );
                                        break;

                                    case System.Windows.Input.Key.Enter:
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
                    ClearTextBoxes( view );
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

        private static void ClearTextBoxes( Grid view )
        {
            var textBoxes = view.Canvas.Children.OfType<TextBox>().ToArray();
            foreach ( var textBox in textBoxes )
                view.Canvas.Children.Remove( textBox );
        }
    }
}