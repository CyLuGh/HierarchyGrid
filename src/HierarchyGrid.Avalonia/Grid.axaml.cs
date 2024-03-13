using Avalonia.Controls;
using Avalonia.ReactiveUI;
using HierarchyGrid.Definitions;
using HierarchyGrid.Skia;
using ReactiveUI;
using SkiaSharp;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using SkiaSharp.Views.Desktop;
using Avalonia.Input;
using System.Windows.Input;
using Avalonia.Media;
using Avalonia;
using LanguageExt;

namespace HierarchyGrid.Avalonia;

public partial class Grid : ReactiveUserControl<HierarchyGridViewModel>
{
    private readonly Flyout _tooltip;

    public Grid()
    {
        InitializeComponent();
        _tooltip = new()
        {
            ShowMode = FlyoutShowMode.TransientWithDismissOnPointerMoveAway ,
            OverlayInputPassThroughElement = this
        };

        this.WhenActivated( disposables =>
        {
            this.WhenAnyValue( x => x.ViewModel )
                .WhereNotNull()
                .Throttle( TimeSpan.FromMilliseconds( 50 ) )
                .ObserveOn( RxApp.MainThreadScheduler )
                .Do( vm => PopulateFromViewModel( this , vm , disposables ) )
                .Subscribe()
                .DisposeWith( disposables );
        } );
    }

    private static void PopulateFromViewModel( Grid view , HierarchyGridViewModel viewModel , CompositeDisposable disposables )
    {
        viewModel.DrawGridInteraction.RegisterHandler( ctx =>
            {
                System.Diagnostics.Debug.WriteLine( "DrawGridInteraction" );
                view.SkiaElement.Invalidate();
                DrawSplitters( view , viewModel );
                ctx.SetOutput( System.Reactive.Unit.Default );
            } )
            .DisposeWith( disposables );

        viewModel.DrawEditionTextBoxInteraction.RegisterHandler( ctx =>
            {
                DrawEditingTextBox( view , viewModel , ctx.Input , disposables );
                ctx.SetOutput( System.Reactive.Unit.Default );
            } )
            .DisposeWith( disposables );

        viewModel.FillClipboardInteraction
                .RegisterHandler( async ctx =>
                {
                    var clipboard = TopLevel.GetTopLevel( view )?.Clipboard;
                    if ( clipboard is not null )
                    {
                        var dataObject = new DataObject();
                        dataObject.Set( DataFormats.Text , ctx.Input );
                        await clipboard.SetDataObjectAsync( dataObject );
                    }
                    ctx.SetOutput( System.Reactive.Unit.Default );
                } )
                .DisposeWith( disposables );

        Observable.FromEventPattern<EventHandler<SKPaintSurfaceEventArgs> , SKPaintSurfaceEventArgs>( handler =>
            async ( sender , args ) => await SkiaElement_PaintSurface( args , viewModel ) ,
            handler => view.SkiaElement.PaintSurface += handler ,
            handler => view.SkiaElement.PaintSurface -= handler )
            .ObserveOn( RxApp.MainThreadScheduler )
            .Subscribe()
            .DisposeWith( disposables );

        Observable.FromEventPattern<EventHandler<PointerEventArgs> , PointerEventArgs>( handler =>
            ( sender , args ) => SkiaElement_PointerMove( args , view.SkiaElement , viewModel ) ,
            handler => view.SkiaElement.PointerMoved += handler ,
            handler => view.SkiaElement.PointerMoved -= handler )
            .ObserveOn( RxApp.MainThreadScheduler )
            .Subscribe()
            .DisposeWith( disposables );

        Observable.FromEventPattern<EventHandler<PointerEventArgs> , PointerEventArgs>( handler =>
            ( sender , args ) => SkiaElement_PointerExit( viewModel ) ,
            handler => view.SkiaElement.PointerExited += handler ,
            handler => view.SkiaElement.PointerExited -= handler )
            .Subscribe()
            .DisposeWith( disposables );

        Observable.FromEventPattern<EventHandler<PointerPressedEventArgs> , PointerPressedEventArgs>( handler =>
            ( sender , args ) => SkiaElement_PointerPressed( args , view.SkiaElement , viewModel ) ,
            handler => view.SkiaElement.PointerPressed += handler ,
            handler => view.SkiaElement.PointerPressed -= handler )
            .ObserveOn( RxApp.MainThreadScheduler )
            .Subscribe()
            .DisposeWith( disposables );

        Observable.FromEventPattern<EventHandler<PointerWheelEventArgs> , PointerWheelEventArgs>( handler =>
            ( sender , args ) => SkiaElement_PointerWheel( args , viewModel ) ,
            handler => view.SkiaElement.PointerWheelChanged += handler ,
            handler => view.SkiaElement.PointerWheelChanged -= handler )
            .ObserveOn( RxApp.MainThreadScheduler )
            .Subscribe()
            .DisposeWith( disposables );

        viewModel.ShowTooltipInteraction.RegisterHandler( ctx =>
            {
                view._tooltip.Hide();

                var text = string.Join( Environment.NewLine ,
                    ctx.Input.ResultSet.TooltipText.Match( text => text , () => string.Empty ) ,
                    viewModel.FocusCells.Find( ctx.Input ).Match( fci => fci.TooltipInfo , () => string.Empty ) );

                if ( !string.IsNullOrWhiteSpace( text ) )
                {
                    view._tooltip.Content = text.Trim();
                    view._tooltip.Placement = PlacementMode.Pointer;
                    view._tooltip.ShowAt( view );
                }

                ctx.SetOutput( System.Reactive.Unit.Default );
            } )
            .DisposeWith( disposables );

        viewModel.CloseTooltipInteraction.RegisterHandler( ctx =>
            {
                // As of now, closing the flyout is handled through its mode
                ctx.SetOutput( System.Reactive.Unit.Default );
            } )
            .DisposeWith( disposables );

        view.Bind( viewModel ,
               vm => vm.HorizontalOffset ,
               v => v.HorizontalScrollBar.Value ,
               vmToViewConverter: i => Convert.ToDouble( i ) ,
               viewToVmConverter: d => Convert.ToInt32( d ) )
               .DisposeWith( disposables );

        view.Bind( viewModel ,
            vm => vm.VerticalOffset ,
            v => v.VerticalScrollBar.Value ,
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

        view.SkiaElement.Invalidate();
    }

    private static async Task SkiaElement_PaintSurface( SKPaintSurfaceEventArgs args , HierarchyGridViewModel viewModel )
    {
        SKImageInfo info = args.Info;
        SKSurface surface = args.Surface;
        SKCanvas canvas = surface.Canvas;

        //// TODO: Try to find the UI scaling that's applied in Display settings
        //var scale = view.ScreenScale;
        var scale = 1d;
        await HierarchyGridDrawer.Draw( viewModel , canvas , info.Width , info.Height , scale , false );
    }

    private static void SkiaElement_PointerMove( PointerEventArgs args , SKXamlCanvas element , HierarchyGridViewModel viewModel )
    {
        var position = args.GetPosition( element );
        viewModel.HandleMouseOver( position.X , position.Y , 1 );
    }

    private static void SkiaElement_PointerExit( HierarchyGridViewModel viewModel )
    {
        viewModel.HandleMouseLeft();
    }

    private static void SkiaElement_PointerWheel( PointerWheelEventArgs args , HierarchyGridViewModel viewModel )
    {
        var delta = args.Delta.Y;

        if ( args.KeyModifiers.HasFlag( KeyModifiers.Control ) )
            viewModel.Scale += .05 * ( delta < 0 ? 1 : -1 );
        else if ( args.KeyModifiers.HasFlag( KeyModifiers.Shift ) )
            viewModel.HorizontalOffset += 5 * ( delta < 0 ? 1 : -1 );
        else
            viewModel.VerticalOffset += 5 * ( delta < 0 ? 1 : -1 );

        args.Handled = true;
    }

    private static void SkiaElement_PointerPressed( PointerPressedEventArgs args , SKXamlCanvas element , HierarchyGridViewModel viewModel )
    {
        var position = args.GetPosition( element );
        var point = args.GetCurrentPoint( element );

        if ( point.Properties.IsLeftButtonPressed )
        {
            if ( args.ClickCount == 2 )
            {
                //viewModel.HandleDoubleClick( position.X , position.Y , view.ScreenScale );
                viewModel.HandleDoubleClick( position.X , position.Y , 1 );
            }
            else
            {
                var ctrl = args.KeyModifiers.HasFlag( KeyModifiers.Control );
                var shift = args.KeyModifiers.HasFlag( KeyModifiers.Shift );

                //viewModel.HandleMouseDown( position.X , position.Y , shift , ctrl , screenScale: view.ScreenScale );
                viewModel.HandleMouseDown( position.X , position.Y , shift , ctrl , screenScale: 1 );
            }
        }
        else
        {
            //viewModel.HandleMouseDown( position.X , position.Y , false , false , screenScale: view.ScreenScale );
            viewModel.HandleMouseDown( position.X , position.Y , false , false , screenScale: 1 );

            // Show context menu
            if ( viewModel.IsValid && viewModel.HasData )
            {
                //var contextMenu = BuildContextMenu( viewModel , position.X , position.Y , view.ScreenScale );
                var contextMenu = BuildContextMenu( viewModel , position.X , position.Y , 1 );
                contextMenu.Open( element );
            }
        }

        args.Handled = true;
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
            //IsChecked = viewModel.EnableCrosshair ,
            //IsCheckable = true ,
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
                    Opacity = 0
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

            var dsp = Observable.FromEventPattern<EventHandler<VectorEventArgs> , VectorEventArgs>( handler =>
                ( sender , args ) => Splitter_DragComplete( args , viewModel , def ) ,
                handler => splitter.DragCompleted += handler ,
                handler => splitter.DragCompleted -= handler )
                .Subscribe();
            viewModel.ResizeObservables.Enqueue( dsp );

            Canvas.SetTop( splitter , coord.Top );
            Canvas.SetLeft( splitter , coord.Right - 2 );
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

            var dsp = Observable.FromEventPattern<EventHandler<VectorEventArgs> , VectorEventArgs>( handler =>
                ( sender , args ) => Splitter_Header_DragComplete( args , viewModel , currentIndex ) ,
                handler => splitter.DragCompleted += handler ,
                handler => splitter.DragCompleted -= handler )
                .Subscribe();
            viewModel.ResizeObservables.Enqueue( dsp );

            Canvas.SetTop( splitter , currentY );
            Canvas.SetLeft( splitter , currentX - 2 );
        }

        var exceeding = splitters.Skip( splitterCount ).ToArray();
        Clear( view , exceeding );
    }

    private static void Splitter_DragComplete( VectorEventArgs args , HierarchyGridViewModel viewModel , HierarchyDefinition definition )
    {
        var pos = viewModel.ColumnsDefinitions.GetPosition( definition );
        viewModel.ColumnsWidths[pos] = Math.Max( viewModel.ColumnsWidths[pos] + args.Vector.X , 10d );

        Observable.Return( false )
            .Delay( TimeSpan.FromMilliseconds( 100 ) )
            .InvokeCommand( viewModel , x => x.DrawGridCommand );
    }

    private static void Splitter_Header_DragComplete( VectorEventArgs args , HierarchyGridViewModel viewModel , int currentIndex )
    {
        viewModel.RowsHeadersWidth[currentIndex] =
            Math.Max( viewModel.RowsHeadersWidth[currentIndex] + args.Vector.X , 10d );

        Observable.Return( false )
            .Delay( TimeSpan.FromMilliseconds( 100 ) )
            .InvokeCommand( viewModel , x => x.DrawGridCommand );
    }

    private static void EditorKeyDown( TextBox tb , KeyEventArgs args , HierarchyGridViewModel viewModel , Func<string , bool> editor )
    {
        switch ( args.Key )
        {
            case Key.Escape:
                viewModel.EditedCell = Option<PositionedCell>.None;
                break;

            case Key.Enter:
                viewModel.EditedCell = Option<PositionedCell>.None;
                Observable.Return( editor( tb.Text ?? string.Empty ) )
                    .InvokeCommand( viewModel.DrawGridCommand );
                break;
        }
    }

    private static void DrawEditingTextBox( Grid view , HierarchyGridViewModel viewModel , Seq<PositionedCell> drawnCells , CompositeDisposable disposables )
    {
        /* Make sure there's no editing textbox when there is no edition */
        if ( !viewModel.IsEditing )
        {
            Clear<TextBox>( view );
            return;
        }

        var currentPositionEditedCell =
            from editedCell in viewModel.EditedCell
            from drawnCell in drawnCells.Find( x => x.Equals( editedCell ) )
            from editor in drawnCell.ResultSet.Editor
            select (drawnCell, editor);

        currentPositionEditedCell
            .Some( t =>
            {
                var (cell, editor) = t;

                /* Create or reuse textbox */
                var textBox = FindUniqueComponent<TextBox>( view , v =>
                {
                    var tb = new TextBox();

                    Observable.FromEventPattern<EventHandler<KeyEventArgs> , KeyEventArgs>( handler =>
                        ( sender , args ) => EditorKeyDown( tb , args , viewModel , editor ) ,
                        handler => tb.KeyDown += handler ,
                        handler => tb.KeyDown -= handler )
                        .Subscribe()
                        .DisposeWith( disposables );

                    v.Canvas.Children.Add( tb );
                    return tb;
                } );

                textBox.Width = cell.Width;
                textBox.Height = cell.Height;
                textBox.TextAlignment = TextAlignment.Right;
                textBox.Text = cell.ResultSet.Result;

                Canvas.SetLeft( textBox , cell.Left );
                Canvas.SetTop( textBox , cell.Top );

                textBox.Focus();
            } )
            .None( () =>
            {
                Clear<TextBox>( view );
            } );


    }

    private static void Clear<T>( Grid view ) where T : Control
    {
        foreach ( var o in view.Canvas.Children.OfType<T>().ToArray() )
            view.Canvas.Children.Remove( o );
    }

    private static void Clear<T>( Grid view , IEnumerable<T> items ) where T : Control
    {
        foreach ( var o in items )
            view.Canvas.Children.Remove( o );
    }

    private static T FindUniqueComponent<T>( Grid view , Func<Grid , T> create ) where T : Control
    {
        return view.Canvas.Children.OfType<T>().SingleOrDefault() ?? create( view );
    }
}
