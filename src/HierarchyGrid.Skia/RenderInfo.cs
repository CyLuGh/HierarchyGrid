﻿using HierarchyGrid.Definitions;
using LanguageExt;
using SkiaSharp;

namespace HierarchyGrid.Skia
{
    internal struct RenderInfo
    {
        public SKColor BackgroundColor { get; set; }
        public SKColor ForegroundColor { get; set; }

        private static SKColor FindBackgroundColor( HierarchyGridViewModel viewModel , SkiaTheme theme , PositionedCell cell )
        {
            if ( cell.HasHoverState( viewModel ) )
                return theme.HoverBackgroundColor;

            if ( cell.IsHighlighted() )
                return theme.HighlightBackgroundColor;

            return cell.ResultSet.Qualifier switch
            {
                Qualification.Error => theme.ErrorBackgroundColor,
                Qualification.Warning => theme.WarningBackgroundColor,
                Qualification.Remark => theme.RemarkBackgroundColor,
                Qualification.Empty => theme.EmptyBackgroundColor,
                Qualification.ReadOnly => theme.ReadOnlyBackgroundColor,
                Qualification.Computed => theme.ComputedBackgroundColor,
                Qualification.Custom => cell.ResultSet.BackgroundColor.Match( t => new SKColor( t.R , t.G , t.B , t.A ) , () => theme.BackgroundColor ),
                _ => theme.BackgroundColor
            };
        }

        private static SKColor FindForegroundColor( HierarchyGridViewModel viewModel , SkiaTheme theme , PositionedCell cell )
        {
            if ( cell.HasHoverState( viewModel ) )
                return theme.HoverForegroundColor;

            if ( cell.IsHighlighted() )
                return theme.HighlightForegroundColor;

            return cell.ResultSet.Qualifier switch
            {
                Qualification.Error => theme.ErrorForegroundColor,
                Qualification.Warning => theme.WarningForegroundColor,
                Qualification.Remark => theme.RemarkForegroundColor,
                Qualification.Empty => theme.EmptyForegroundColor,
                Qualification.ReadOnly => theme.ReadOnlyForegroundColor,
                Qualification.Computed => theme.ComputedForegroundColor,
                Qualification.Custom => cell.ResultSet.ForegroundColor.Match( t => new SKColor( t.R , t.G , t.B , t.A ) , () => theme.ForegroundColor ),
                _ => theme.ForegroundColor
            };
        }

        internal static RenderInfo FindRender( HierarchyGridViewModel viewModel , SkiaTheme theme , PositionedCell cell )
            => new()
            {
                BackgroundColor = FindBackgroundColor( viewModel , theme , cell ) ,
                ForegroundColor = FindForegroundColor( viewModel , theme , cell )
            };

        internal static RenderInfo FindRender( HierarchyGridViewModel viewModel , SkiaTheme theme , HierarchyDefinition hdef )
        {
            var hoveredCell = viewModel.FindHoveredCell();
            return new RenderInfo
            {
                BackgroundColor = FindBackgroundColor( viewModel , theme , hdef , hoveredCell ) ,
                ForegroundColor = FindForegroundColor( viewModel , theme , hdef , hoveredCell )
            };
        }

        private static SKColor FindBackgroundColor( HierarchyGridViewModel viewModel , SkiaTheme theme , HierarchyDefinition hdef , Option<PositionedCell> hoveredCell )
            => hoveredCell.Match( cell =>
            {
                if ( ( hdef is ConsumerDefinition consumer && cell.ConsumerDefinition.Equals( consumer ) )
                        || ( hdef is ProducerDefinition producer && cell.ProducerDefinition.Equals( producer ) ) )
                {
                    return theme.HoverHeaderBackgroundColor;
                }

                return FindBackgroundColor( viewModel , theme , hdef );
            } , () => FindBackgroundColor( viewModel , theme , hdef ) );

        private static SKColor FindBackgroundColor( HierarchyGridViewModel viewModel , SkiaTheme theme , HierarchyDefinition hdef )
        {
            if ( hdef?.Guid.Equals( viewModel.HoveredElementId ) == true )
                return theme.HoverHeaderBackgroundColor;

            if ( hdef == null || hdef.Count() > 1 )
                return theme.HeaderBackgroundColor;

            if ( hdef.IsHighlighted )
                return theme.HighlightHeaderBackgroundColor;

            return IsHovered( viewModel , hdef ) ? theme.HoverHeaderBackgroundColor : theme.HeaderBackgroundColor;
        }

        private static SKColor FindForegroundColor( HierarchyGridViewModel viewModel , SkiaTheme theme , HierarchyDefinition hdef , Option<PositionedCell> hoveredCell )
            => hoveredCell.Match( cell =>
            {
                if ( ( hdef is ConsumerDefinition consumer && cell.ConsumerDefinition.Equals( consumer ) )
                        || ( hdef is ProducerDefinition producer && cell.ProducerDefinition.Equals( producer ) ) )
                {
                    return theme.HoverHeaderForegroundColor;
                }

                return FindForegroundColor( viewModel , theme , hdef );
            } , () => FindForegroundColor( viewModel , theme , hdef ) );

        private static SKColor FindForegroundColor( HierarchyGridViewModel viewModel , SkiaTheme theme , HierarchyDefinition hdef )
        {
            if ( hdef?.Guid.Equals( viewModel.HoveredElementId ) == true )
                return theme.HoverHeaderForegroundColor;

            if ( hdef == null || hdef.Count() > 1 )
                return theme.HeaderForegroundColor;

            if ( hdef.IsHighlighted )
                return theme.HighlightHeaderForegroundColor;

            return IsHovered( viewModel , hdef ) ? theme.HoverHeaderForegroundColor : theme.HeaderForegroundColor;
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