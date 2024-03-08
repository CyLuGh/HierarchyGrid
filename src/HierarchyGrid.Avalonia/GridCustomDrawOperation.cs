using Avalonia;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using HierarchyGrid.Definitions;
using HierarchyGrid.Skia;

namespace HierarchyGrid.Avalonia;

//internal class GridCustomDrawOperation : ICustomDrawOperation
//{
//    private readonly FormattedText _noSkia;
//    private readonly HierarchyGridViewModel _viewModel;

//    public Rect Bounds { get; }

//    public GridCustomDrawOperation( Rect bounds , FormattedText noSkia , HierarchyGridViewModel viewModel )
//    {
//        _noSkia = noSkia;
//        _viewModel = viewModel;
//        Bounds = bounds;
//    }

//    public void Dispose()
//    {
//    }

//    public bool Equals( ICustomDrawOperation? other )
//        => false;

//    public bool HitTest( Point p )
//        => true;

//    public void Render( IDrawingContextImpl context )
//    {
//        var canvas = ( context as ISkiaDrawingContextImpl )?.SkCanvas;

//        if ( canvas == null )
//        {
//            context.DrawText( Brush.Parse( _viewModel.Theme.ForegroundColor.ToCode() ) , new Point() , _noSkia.PlatformImpl );
//        }
//        else
//        {
//            canvas.Save();
//            HierarchyGridDrawer.Draw( _viewModel , canvas , (float) Bounds.Width , (float) Bounds.Height );
//            canvas.Restore();
//        }
//    }
//}