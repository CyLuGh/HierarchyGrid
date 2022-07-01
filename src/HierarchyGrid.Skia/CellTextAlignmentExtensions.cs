using HierarchyGrid.Definitions;

namespace HierarchyGrid.Skia;

internal static class CellTextAlignmentExtensions
{
    internal static Topten.RichTextKit.TextAlignment ToRichTextKitTextAlignment( this CellTextAlignment cta )
        => cta switch
        {
            CellTextAlignment.Auto => Topten.RichTextKit.TextAlignment.Auto,
            CellTextAlignment.Left => Topten.RichTextKit.TextAlignment.Left,
            CellTextAlignment.Center => Topten.RichTextKit.TextAlignment.Center,
            CellTextAlignment.Right => Topten.RichTextKit.TextAlignment.Right,
            _ => Topten.RichTextKit.TextAlignment.Auto
        };
}