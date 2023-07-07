namespace HierarchyGrid.Definitions;

public record SimplifiedCellPosition
{
    public SimplifiedHierarchyDefinitionRef Producer { get; set; }
    public SimplifiedHierarchyDefinitionRef Consumer { get; set; }

    public SimplifiedCellPosition()
    {

    }

    public SimplifiedCellPosition( PositionedCell positionedCell )
    {
        Producer = new( positionedCell.ProducerDefinition );
        Consumer = new( positionedCell.ConsumerDefinition );
    }
}
