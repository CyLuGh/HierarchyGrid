using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using HierarchyGrid.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HierarchyGrid.Avalonia;

public class GridControl : Control
{
    private readonly HierarchyGridViewModel _viewModel;

    public GridControl( HierarchyGridViewModel viewModel )
    {
        _viewModel = viewModel;
    }

    public override void Render( DrawingContext context )
    {
        //var noSkia = new FormattedText()
        //{
        //    Text = "Current rendering API is not Skia"
        //};
        //context.Custom( new GridCustomDrawOperation( new Rect( 0 , 0 , Bounds.Width , Bounds.Height ) , noSkia , _viewModel ) );
        //Dispatcher.UIThread.InvokeAsync( InvalidateVisual , DispatcherPriority.Background );
    }
}