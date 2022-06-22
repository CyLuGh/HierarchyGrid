using Avalonia.Controls;
using Avalonia.Media;
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
        base.Render( context );
    }
}