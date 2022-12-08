using Avalonia;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using Avalonia.Threading;
using HierarchyGrid.Definitions;
using HierarchyGrid.Skia;

namespace HierarchyGrid.Avalonia;

internal class GridCustomDrawOperation : ICustomDrawOperation
{
    private readonly FormattedText _noSkia;
    private readonly HierarchyGridViewModel _viewModel;

    public Rect Bounds { get; internal set; }

    public GridCustomDrawOperation( Rect bounds , FormattedText noSkia , HierarchyGridViewModel viewModel )
    {
        _noSkia = noSkia;
        _viewModel = viewModel;
        Bounds = bounds;
    }

    public void Dispose()
    {
    }

    public bool Equals( ICustomDrawOperation? other )
        => false;

    public bool HitTest( Point p )
        => true;

    public void Render( IDrawingContextImpl context )
    {
        var leaseFeature = context.GetFeature<ISkiaSharpApiLeaseFeature>();
        if ( leaseFeature == null )
            using ( var c = new DrawingContext( context , false ) )
            {
                c.DrawText( _noSkia , new Point() );
            }
        else
        {
            using var lease = leaseFeature.Lease();
            var canvas = lease.SkCanvas;
            canvas.Save();
            HierarchyGridDrawer.Draw( _viewModel , canvas , (float) Bounds.Width , (float) Bounds.Height );
            canvas.Restore();
        }
    }
}