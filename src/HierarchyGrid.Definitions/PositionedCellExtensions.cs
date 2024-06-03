using LanguageExt;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HierarchyGrid.Definitions
{
    public static class PositionedCellExtensions
    {
        public static bool IsHovered( this PositionedCell cell , HierarchyGridViewModel viewModel ) =>
            cell.VerticalPosition == viewModel.HoveredRow && cell.HorizontalPosition == viewModel.HoveredColumn;

        public static bool IsCrosshaired( this PositionedCell cell , HierarchyGridViewModel viewModel ) =>
            viewModel.EnableCrosshair && ( cell.VerticalPosition == viewModel.HoveredRow ||
                                           cell.HorizontalPosition == viewModel.HoveredColumn );

        public static bool HasHoverState( this PositionedCell cell , HierarchyGridViewModel viewModel ) =>
            cell.IsHovered( viewModel ) || cell.IsCrosshaired( viewModel );

        public static bool IsHighlighted( this PositionedCell cell ) =>
            cell.ProducerDefinition?.IsHighlighted == true || cell.ConsumerDefinition?.IsHighlighted == true;

        public static bool HasSpecialRenderStatus( this PositionedCell cell , HierarchyGridViewModel viewModel ) =>
            cell.HasHoverState( viewModel ) || cell.IsHighlighted();

        public static Option<PositionedCell> FindPositionedCell( this HierarchyGridViewModel viewModel ,
            SimplifiedCellPosition simplifiedCellPosition )
        {
            var producers = viewModel.ProducersCache.Items.FlatList().ToSeq();
            var consumers = viewModel.ConsumersCache.Items.FlatList().ToSeq();
            return FindPositionedCell( producers , consumers , simplifiedCellPosition );
        }

        public static Seq<PositionedCell> FindPositionedCells( this HierarchyGridViewModel viewModel ,
            IEnumerable<SimplifiedCellPosition> simplifiedCellPositions )
        {
            var producers = viewModel.ProducersCache.Items.FlatList().ToSeq();
            var consumers = viewModel.ConsumersCache.Items.FlatList().ToSeq();
            return simplifiedCellPositions.Select( scp => FindPositionedCell( producers , consumers , scp ) ).ToSeq()
                .Somes();
        }

        private static Option<PositionedCell> FindPositionedCell( Seq<ProducerDefinition> producers ,
            Seq<ConsumerDefinition> consumers , SimplifiedCellPosition simplifiedCellPosition )
        {
            var cell =
                from p in producers.Find( x =>
                    x.Position == simplifiedCellPosition.Producer.Position &&
                    string.Equals( x.ToString() , simplifiedCellPosition.Producer.FullPath ,
                        StringComparison.OrdinalIgnoreCase ) &&
                    simplifiedCellPosition.Producer.ChildrenPaths.SequenceEqual(
                        x.Children.Select( c => c.ToString() ) ) )
                from c in consumers.Find( x =>
                    x.Position == simplifiedCellPosition.Consumer.Position &&
                    string.Equals( x.ToString() , simplifiedCellPosition.Consumer.FullPath ,
                        StringComparison.OrdinalIgnoreCase ) &&
                    simplifiedCellPosition.Consumer.ChildrenPaths.SequenceEqual(
                        x.Children.Select( c => c.ToString() ) ) )
                select new PositionedCell { ProducerDefinition = p , ConsumerDefinition = c };
            return cell;
        }
    }
}