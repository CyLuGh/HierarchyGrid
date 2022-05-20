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

            view.SkiaElement.Events()
                .PaintSurface
                .Subscribe( args =>
                {
                    SKImageInfo info = args.Info;
                    SKSurface surface = args.Surface;
                    SKCanvas canvas = surface.Canvas;
                    HierarchyGridDrawer.Draw( viewModel , canvas , info.Width , info.Height );
                } )
                .DisposeWith( disposables );

            //view.SkiaElement.Events()
            //    .MouseMove
            //    .Subscribe( args =>
            //    {
            //    } )
            //    .DisposeWith( disposables );

            view.SkiaElement.Events()
                .MouseLeftButtonDown
                .Subscribe( args =>
                {
                    if ( viewModel.RowsHeadersWidth?.Any() != true || viewModel.ColumnsHeadersHeight?.Any() != true )
                        return;

                    var position = args.GetPosition( view.SkiaElement );

                    // Find corresponding element
                    if ( position.X <= viewModel.RowsHeadersWidth.Sum() )
                    {
                        Console.WriteLine( "In row headers" );

                        if ( position.Y <= viewModel.ColumnsHeadersHeight.Sum() )
                        {
                            Console.WriteLine( "In global headers" );
                        }
                    }
                    else if ( position.Y <= viewModel.ColumnsHeadersHeight.Sum() )
                    {
                        Console.WriteLine( "In row headers" );
                    }
                    else
                    {
                        Console.WriteLine( "In cells" );
                        var cell = viewModel.CellsCoordinates.Find( t => t.Item1.Contains( position.X , position.Y ) )
                            .Match( s => s.Item2 , () => Option<PositionedCell>.None );

                        cell.Some( c =>
                        {
                            Console.WriteLine( "Cell {0}" , c );
                        } )
                        .None( () =>
                        {
                            Console.WriteLine( "No cell" );
                        } );
                    }
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
    }
}