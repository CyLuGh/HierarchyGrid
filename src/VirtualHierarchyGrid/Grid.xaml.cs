using HierarchyGrid.Definitions;
using ReactiveUI;
using Splat;
using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Accessibility;
using ReactiveMarbles.ObservableEvents;

namespace VirtualHierarchyGrid
{
    public partial class Grid : IEnableLogger
    {
        public Grid()
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

        private static void PopulateFromViewModel( HierarchyGrid view , HierarchyGridViewModel viewModel , CompositeDisposable disposables )
        {
            viewModel.DrawGridInteraction.RegisterHandler( ctx =>
            {
                viewModel.Log().Debug( "Drawing grid" );
                //view.DrawGrid( view.RenderSize );
                ctx.SetOutput( Unit.Default );
            } ).DisposeWith( disposables );

            //viewModel.DrawCellsInteraction.RegisterHandler( ctx =>
            //{
            //    var (pCells, invalidate) = ctx.Input;
            //    view.DrawCells( pCells , invalidate );
            //    ctx.SetOutput( Unit.Default );
            //} ).DisposeWith( disposables );

            //viewModel.EndEditionInteraction.RegisterHandler( ctx =>
            //{
            //    //var textBoxes = view.HierarchyGridCanvas.Children.OfType<TextBox>().ToArray();
            //    //foreach ( var tb in textBoxes )
            //    //{
            //    //    if ( tb.Tag is IDisposable d )
            //    //        d.Dispose();
            //    //    view.HierarchyGridCanvas.Children.Remove( tb );
            //    //}

            //    ctx.SetOutput( Unit.Default );
            //} )
            //    .DisposeWith( disposables );

            //viewModel.EditInteraction.RegisterHandler( ctx =>
            //{
            //    //var (rowIdx, colIdx, resultSet) = ctx.Input;

            //    //var cell = view.HierarchyGridCanvas.Children.OfType<HierarchyGridCell>()
            //    //    .FirstOrDefault( o => o.ViewModel.RowIndex == rowIdx && o.ViewModel.ColumnIndex == colIdx );

            //    //if ( cell != null )
            //    //{
            //    //    var textBox = new TextBox();
            //    //    textBox.Width = cell.Width;
            //    //    textBox.Height = cell.Height;
            //    //    Canvas.SetLeft( textBox , Canvas.GetLeft( cell ) );
            //    //    Canvas.SetTop( textBox , Canvas.GetTop( cell ) );
            //    //    view.HierarchyGridCanvas.Children.Add( textBox );

            //    //    textBox.Tag = textBox.Events().KeyDown.Subscribe( e =>
            //    //    {
            //    //        if ( e.Key == Key.Enter || e.Key == Key.Return )
            //    //            if ( resultSet.Editor.Match( edt => edt( textBox.Text ) , () => false ) )
            //    //                Observable.Return( (viewModel.HorizontalOffset, viewModel.VerticalOffset, viewModel.Width, viewModel.Height, viewModel.Scale, true) )
            //    //                    .InvokeCommand( viewModel , x => x.FindCellsToDrawCommand );
            //    //        if ( e.Key == Key.Escape || e.Key == Key.Enter || e.Key == Key.Return )
            //    //            viewModel.IsEditing = false;
            //    //    } );

            //    //    textBox.Focus();
            //    //}

            //    ctx.SetOutput( Unit.Default );
            //} )
            //    .DisposeWith( disposables );

            view.Background = (Brush) view.TryFindResource( "GridBackground" ) ?? Brushes.LightGray;
            view.Corner.Fill = (Brush) view.TryFindResource( "GridBackground" ) ?? Brushes.LightGray;

            //view.OneWayBind( viewModel ,
            //    vm => vm.Scale ,
            //    v => v.ScaleTransform.ScaleX )
            //    .DisposeWith( disposables );

            //view.OneWayBind( viewModel ,
            //    vm => vm.Scale ,
            //    v => v.ScaleTransform.ScaleY )
            //    .DisposeWith( disposables );

            view.SkiaElement.Events()
                .LayoutUpdated
                .Throttle( TimeSpan.FromMilliseconds( 75 ) )
                .SubscribeSafe( _ =>
                {
                    viewModel.Height = view.SkiaElement.ActualHeight;
                    viewModel.Width = view.SkiaElement.ActualWidth;
                } );

            //view.HierarchyGridCanvas.Events()
            //    .MouseWheel
            //    .SubscribeSafe( e =>
            //    {
            //        if ( Keyboard.IsKeyDown( Key.LeftCtrl ) || Keyboard.IsKeyDown( Key.RightCtrl ) )
            //            viewModel.Scale += .05 * ( e.Delta < 0 ? 1 : -1 );
            //        else if ( Keyboard.IsKeyDown( Key.LeftShift ) || Keyboard.IsKeyDown( Key.RightShift ) )
            //            viewModel.HorizontalOffset += 5 * ( e.Delta < 0 ? 1 : -1 );
            //        else
            //            viewModel.VerticalOffset += 5 * ( e.Delta < 0 ? 1 : -1 );
            //    } )
            //    .DisposeWith( disposables );

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

            view.OneWayBind( viewModel ,
                    vm => vm.HasData ,
                    v => v.MessageBorder.Visibility ,
                    b => b ? Visibility.Collapsed : Visibility.Visible )
                .DisposeWith( disposables );

            view.OneWayBind( viewModel ,
                    vm => vm.StatusMessage ,
                    v => v.MessageTextBlock.Text )
                .DisposeWith( disposables );
        }
    }
}