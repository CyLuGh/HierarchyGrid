namespace HierarchyGrid.Definitions;

public readonly record struct SimplifiedCellPosition
{
    public SimplifiedHierarchyDefinitionRef Producer { get; }
    public SimplifiedHierarchyDefinitionRef Consumer { get; }

    public SimplifiedCellPosition(PositionedCell positionedCell)
    {
        Producer = new(positionedCell.ProducerDefinition);
        Consumer = new(positionedCell.ConsumerDefinition);
    }
}
