using HierarchyGrid.Definitions;
using LanguageExt;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                BackgroundColor = FindBackgroundColor( hdef , hoveredCell ) ,
                ForegroundColor = FindForegroundColor( hdef , hoveredCell )
            };
        }

        private static SKColor FindBackgroundColor( HierarchyDefinition hdef , Option<PositionedCell> hoveredCell )
            => hoveredCell.Match( cell =>
            {
                if ( ( hdef is ConsumerDefinition consumer && cell.ConsumerDefinition.Equals( consumer ) )
                        || ( hdef is ProducerDefinition producer && cell.ProducerDefinition.Equals( producer ) ) )
                {
                    return SKColors.SeaGreen;
                }

                return FindBackgroundColor( hdef );
            } , () => FindBackgroundColor( hdef ) );

        private static SKColor FindBackgroundColor( HierarchyDefinition hdef )
            => hdef?.IsHighlighted == true ? SKColors.LightBlue : SKColors.LightGray;

        private static SKColor FindForegroundColor( HierarchyDefinition hdef , Option<PositionedCell> hoveredCell )
            => hoveredCell.Match( cell =>
            {
                if ( ( hdef is ConsumerDefinition consumer && cell.ConsumerDefinition.Equals( consumer ) )
                        || ( hdef is ProducerDefinition producer && cell.ProducerDefinition.Equals( producer ) ) )
                {
                    return SKColors.White;
                }

                return FindForegroundColor( hdef );
            } , () => FindForegroundColor( hdef ) );

        private static SKColor FindForegroundColor( HierarchyDefinition hdef )
            => SKColors.Black;
    }
}