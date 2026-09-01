using System;
using System.Collections.Generic;
using System.Linq;
using LanguageExt;

namespace HierarchyGrid.Definitions
{
    public static class PositionedCellExtensions
    {
        extension(PositionedCell cell)
        {
            public bool IsHovered( HierarchyGridViewModel viewModel) =>
                cell.VerticalPosition == viewModel.HoveredRow
                && cell.HorizontalPosition == viewModel.HoveredColumn;

            public bool IsCrosshaired( HierarchyGridViewModel viewModel
            ) =>
                viewModel.EnableCrosshair
                && (
                    cell.VerticalPosition == viewModel.HoveredRow
                    || cell.HorizontalPosition == viewModel.HoveredColumn
                );

            public bool HasHoverState( HierarchyGridViewModel viewModel
            ) => cell.IsHovered(viewModel) || cell.IsCrosshaired(viewModel);

            public bool IsHighlighted() =>
                cell.ProducerDefinition?.IsHighlighted == true
                || cell.ConsumerDefinition?.IsHighlighted == true;

            public bool HasSpecialRenderStatus( HierarchyGridViewModel viewModel
            ) => cell.HasHoverState(viewModel) || cell.IsHighlighted();
        }

        extension(HierarchyGridViewModel viewModel)
        {
            public Option<PositionedCell> FindPositionedCell( SimplifiedCellPosition simplifiedCellPosition
            )
            {
                var producers = viewModel.Producers.FlatList();
                var consumers = viewModel.Consumers.FlatList();
                return FindPositionedCell(producers, consumers, simplifiedCellPosition);
            }

            public Seq<PositionedCell> FindPositionedCells( IEnumerable<SimplifiedCellPosition> simplifiedCellPositions
            )
            {
                var producers = viewModel.Producers.FlatList();
                var consumers = viewModel.Consumers.FlatList();
                return simplifiedCellPositions
                    .Select(scp => FindPositionedCell(producers, consumers, scp))
                    .ToSeq()
                    .Somes();
            }
        }

        private static Option<PositionedCell> FindPositionedCell(
            Seq<ProducerDefinition> producers,
            Seq<ConsumerDefinition> consumers,
            SimplifiedCellPosition simplifiedCellPosition
        )
        {
            var cell =
                from p in producers.Find(x =>
                    x.Position == simplifiedCellPosition.Producer.Position
                    && string.Equals(
                        x.ToString(),
                        simplifiedCellPosition.Producer.FullPath,
                        StringComparison.OrdinalIgnoreCase
                    )
                    && simplifiedCellPosition.Producer.ChildrenPaths.SequenceEqual(
                        x.Children.Select(c => c.ToString())
                    )
                )
                from c in consumers.Find(x =>
                    x.Position == simplifiedCellPosition.Consumer.Position
                    && string.Equals(
                        x.ToString(),
                        simplifiedCellPosition.Consumer.FullPath,
                        StringComparison.OrdinalIgnoreCase
                    )
                    && simplifiedCellPosition.Consumer.ChildrenPaths.SequenceEqual(
                        x.Children.Select(c => c.ToString())
                    )
                )
                select new PositionedCell { ProducerDefinition = p, ConsumerDefinition = c };
            return cell;
        }
    }
}
