using HierarchyGrid.Definitions;
using SkiaSharp;

namespace HierarchyGrid.Skia
{
    internal static class ElementCoordinatesExtensions
    {
        internal static SKRect ToRectangle( this ElementCoordinates coordinates )
            => SKRect.Create( (float) coordinates.Left , (float) coordinates.Top , (float) coordinates.Width , (float) coordinates.Height );
    }
}