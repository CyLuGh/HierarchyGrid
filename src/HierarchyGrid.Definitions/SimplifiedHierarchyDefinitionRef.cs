using System.Linq;

namespace HierarchyGrid.Definitions;

public record SimplifiedHierarchyDefinitionRef
{
    public bool IsProducer { get; set; }
    public bool IsConsumer { get; set; }
    public int Position { get; set; }
    public string FullPath { get; set; }
    public string[] ChildrenPaths { get; set; }

    public SimplifiedHierarchyDefinitionRef()
    {

    }

    public SimplifiedHierarchyDefinitionRef( HierarchyDefinition definition )
    {
        IsProducer = definition is ProducerDefinition;
        IsConsumer = definition is ConsumerDefinition;

        Position = definition.Position;
        FullPath = definition.ToString();
        ChildrenPaths = definition.Children.Select( c => c.ToString() ).ToArray();
    }
}
