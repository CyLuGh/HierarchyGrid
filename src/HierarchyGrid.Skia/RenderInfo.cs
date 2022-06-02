using HierarchyGrid.Definitions;
using LanguageExt;
using SkiaSharp;

namespace HierarchyGrid.Skia
{
    internal struct RenderInfo
    {
        public SKColor BackgroundColor { get; set; }
        public SKColor BorderColor { get; set; }
        public SKColor ForegroundColor { get; set; }

        private static SKColor FindBackgroundColor( HierarchyGridViewModel viewModel , PositionedCell cell )
        {
            if ( cell.VerticalPosition == viewModel.HoveredRow
                && cell.HorizontalPosition == viewModel.HoveredColumn )
            {
                return SKColors.LightSeaGreen;
            }

            if ( viewModel.EnableCrosshair
                && ( cell.VerticalPosition == viewModel.HoveredRow
                    || cell.HorizontalPosition == viewModel.HoveredColumn ) )
            {
                return SKColors.LightSeaGreen;
            }

            if ( cell.ProducerDefinition.IsHighlighted || cell.ConsumerDefinition.IsHighlighted )
                return SKColors.LightBlue;

            return cell.ResultSet.Qualifier switch
            {
                Qualification.Error => SKColors.IndianRed,
                Qualification.Warning => SKColors.YellowGreen,
                Qualification.Remark => SKColors.GreenYellow,
                Qualification.Custom => cell.ResultSet.BackgroundColor.Match( t => new SKColor( t.r , t.g , t.b , t.a ) , () => SKColors.White ),
                _ => SKColors.White
            };
        }

        private static SKColor FindForegroundColor( HierarchyGridViewModel viewModel , PositionedCell cell )
        {
            if ( cell.VerticalPosition == viewModel.HoveredRow
                && cell.HorizontalPosition == viewModel.HoveredColumn )
            {
                return SKColors.Black;
            }

            if ( viewModel.EnableCrosshair
                && ( cell.VerticalPosition == viewModel.HoveredRow
                    || cell.HorizontalPosition == viewModel.HoveredColumn ) )
            {
                return SKColors.Black;
            }

            if ( cell.ProducerDefinition.IsHighlighted || cell.ConsumerDefinition.IsHighlighted )
                return SKColors.Black;

            return cell.ResultSet.Qualifier switch
            {
                Qualification.Error => SKColors.Black,
                Qualification.Warning => SKColors.Black,
                Qualification.Remark => SKColors.Black,
                Qualification.Custom => cell.ResultSet.ForegroundColor.Match( t => new SKColor( t.r , t.g , t.b , t.a ) , () => SKColors.Black ),
                _ => SKColors.Black
            };
        }

        internal static RenderInfo FindRender( HierarchyGridViewModel viewModel , PositionedCell cell )
            => new()
            {
                BackgroundColor = FindBackgroundColor( viewModel , cell ) ,
                ForegroundColor = FindForegroundColor( viewModel , cell )
            };

        internal static RenderInfo FindRender( HierarchyGridViewModel viewModel , HierarchyDefinition hdef )
        {
            var hoveredCell = viewModel.FindHoveredCell();
            return new RenderInfo
            {
                BackgroundColor = FindBackgroundColor( viewModel , hdef , hoveredCell ) ,
                ForegroundColor = FindForegroundColor( viewModel , hdef , hoveredCell )
            };
        }

        private static SKColor FindBackgroundColor( HierarchyGridViewModel viewModel , HierarchyDefinition hdef , Option<PositionedCell> hoveredCell )
            => hoveredCell.Match( cell =>
            {
                if ( ( hdef is ConsumerDefinition consumer && cell.ConsumerDefinition.Equals( consumer ) )
                        || ( hdef is ProducerDefinition producer && cell.ProducerDefinition.Equals( producer ) ) )
                {
                    return SKColors.SeaGreen;
                }

                return FindBackgroundColor( viewModel , hdef );
            } , () => FindBackgroundColor( viewModel , hdef ) );

        private static SKColor FindBackgroundColor( HierarchyGridViewModel viewModel , HierarchyDefinition hdef )
        {
            if ( hdef == null || hdef.Count() > 1 )
                return SKColors.LightGray;

            if ( hdef.IsHighlighted )
                return SKColors.LightBlue;

            return IsHovered( viewModel , hdef ) ? SKColors.SeaGreen : SKColors.LightGray;
        }

        private static SKColor FindForegroundColor( HierarchyGridViewModel viewModel , HierarchyDefinition hdef , Option<PositionedCell> hoveredCell )
            => hoveredCell.Match( cell =>
            {
                if ( ( hdef is ConsumerDefinition consumer && cell.ConsumerDefinition.Equals( consumer ) )
                        || ( hdef is ProducerDefinition producer && cell.ProducerDefinition.Equals( producer ) ) )
                {
                    return SKColors.White;
                }

                return FindForegroundColor( viewModel , hdef );
            } , () => FindForegroundColor( viewModel , hdef ) );

        private static SKColor FindForegroundColor( HierarchyGridViewModel viewModel , HierarchyDefinition hdef )
        {
            if ( hdef == null || hdef.Count() > 1 )
                return SKColors.Black;

            if ( hdef.IsHighlighted )
                return SKColors.Black;

            return IsHovered( viewModel , hdef ) ? SKColors.White : SKColors.Black;
        }

        private static bool IsHovered( HierarchyGridViewModel viewModel , HierarchyDefinition hdef )
        {
            var isColumn = hdef is ConsumerDefinition && !viewModel.IsTransposed;
            var position = isColumn ? viewModel.ColumnsDefinitions.GetPosition( hdef ) : viewModel.RowsDefinitions.GetPosition( hdef );
            var isHovered = isColumn ? viewModel.HoveredColumn == position : viewModel.HoveredRow == position;
            return isHovered;
        }
    }
}