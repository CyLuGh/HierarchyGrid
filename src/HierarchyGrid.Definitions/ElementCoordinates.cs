using System;

namespace HierarchyGrid.Definitions;

/// <summary>
/// Represents the coordinates of an element with boundaries defined by its left, top, right, and bottom edges.
/// This struct provides methods to calculate dimensions and determine spatial relationships.
/// </summary>
public readonly record struct ElementCoordinates
{
    public double Left { get; init; }
    public double Top { get; init; }
    public double Right { get; init; }
    public double Bottom { get; init; }

    public ElementCoordinates(double left, double top, double right, double bottom) =>
        (Left, Top, Right, Bottom) = (left, top, right, bottom);

    public ElementCoordinates(PositionedCell cell) =>
        (Left, Top, Right, Bottom) = (
            cell.Left,
            cell.Top,
            cell.Left + cell.Width,
            cell.Top + cell.Height
        );

    public bool Contains(double x, double y) => Left <= x && x <= Right && Top <= y && y <= Bottom;

    public double Height => Math.Abs(Top - Bottom);
    public double Width => Math.Abs(Right - Left);
}
