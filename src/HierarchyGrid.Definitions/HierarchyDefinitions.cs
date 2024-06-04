using System.Collections.Generic;
using System.Linq;
using LanguageExt;

namespace HierarchyGrid.Definitions;

/// <summary>
/// Represents the default structure of a grid, with rows as producers and columns as consumers.
/// </summary>
public class HierarchyDefinitions
{
    public Seq<ProducerDefinition> Producers { get; }
    public Seq<ConsumerDefinition> Consumers { get; }

    public bool HasDefinitions => !Producers.IsEmpty && !Consumers.IsEmpty;

    public HierarchyDefinitions(
        IEnumerable<ProducerDefinition> producers,
        IEnumerable<ConsumerDefinition> consumers
    )
    {
        Producers = Build(producers);
        Consumers = Build(consumers);
    }

    private Seq<T> Build<T>(IEnumerable<T> input)
        where T : HierarchyDefinition
    {
        int position = -1;
        return input
            .Select(s =>
            {
                s.UpdatePosition(ref position);
                return s;
            })
            .ToSeq()
            .Strict();
    }
}
