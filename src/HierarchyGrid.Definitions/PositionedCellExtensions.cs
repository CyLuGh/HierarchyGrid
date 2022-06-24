namespace HierarchyGrid.Definitions
{
    public static class PositionedCellExtensions
    {
        public static bool IsHovered( this PositionedCell cell , HierarchyGridViewModel viewModel )
            => cell.VerticalPosition == viewModel.HoveredRow
                && cell.HorizontalPosition == viewModel.HoveredColumn;

        public static bool IsCrosshaired( this PositionedCell cell , HierarchyGridViewModel viewModel )
            => viewModel.EnableCrosshair
                && ( cell.VerticalPosition == viewModel.HoveredRow
                    || cell.HorizontalPosition == viewModel.HoveredColumn );

        public static bool HasHoverState( this PositionedCell cell , HierarchyGridViewModel viewModel )
            => cell.IsHovered( viewModel ) || cell.IsCrosshaired( viewModel );

        public static bool IsHighlighted( this PositionedCell cell )
            => cell.ProducerDefinition.IsHighlighted || cell.ConsumerDefinition.IsHighlighted;

        public static bool HasSpecialRenderStatus( this PositionedCell cell , HierarchyGridViewModel viewModel )
            => cell.HasHoverState( viewModel ) || cell.IsHighlighted();
    }
}