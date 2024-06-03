using System.Linq;

namespace HierarchyGrid.Definitions;

public readonly record struct SimplifiedHierarchyDefinitionRef
{
    public bool IsProducer { get; init; }
    public bool IsConsumer { get; init; }
    public int Position { get; init; }
    public string FullPath { get; init; }
    public string[] ChildrenPaths { get; init; }

    public SimplifiedHierarchyDefinitionRef(HierarchyDefinition definition)
    {
        IsProducer = definition is ProducerDefinition;
        IsConsumer = definition is ConsumerDefinition;

        Position = definition.Position;
        FullPath = definition.ToString();
        ChildrenPaths = definition.Children.Select(c => c.ToString()).ToArray();
    }
}
