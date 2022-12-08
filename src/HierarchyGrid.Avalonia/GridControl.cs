using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using HierarchyGrid.Definitions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HierarchyGrid.Avalonia;

public class GridControl : Control
{
    private readonly HierarchyGridViewModel _viewModel;
    private readonly GridCustomDrawOperation _drawOperation;

    public GridControl( HierarchyGridViewModel viewModel )
    {
        _viewModel = viewModel;

        var noSkia = new FormattedText( "Current rendering API is not Skia" , CultureInfo.CurrentCulture , FlowDirection.LeftToRight , Typeface.Default , 14 , Brushes.Black );
        _drawOperation = new GridCustomDrawOperation( new Rect( 0 , 0 , Bounds.Width , Bounds.Height ) , noSkia , _viewModel );
    }

    public override void Render( DrawingContext context )
    {
        _drawOperation.Bounds = new Rect( 0 , 0 , Bounds.Width , Bounds.Height );
        context.Custom( _drawOperation );
        Dispatcher.UIThread.InvokeAsync( InvalidateVisual , DispatcherPriority.Background );
    }
}