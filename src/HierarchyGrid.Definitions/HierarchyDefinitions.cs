using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace HierarchyGrid.Definitions;

/// <summary>
/// Represents the default structure of a grid, with rows as producers and columns as consumers.
/// </summary>
public class HierarchyDefinitions
{
    public ImmutableList<ProducerDefinition> Producers { get; }
    public ImmutableList<ConsumerDefinition> Consumers { get; }

    public bool HasDefinitions => Producers?.Any() == true && Consumers?.Any() == true;

    public HierarchyDefinitions(IEnumerable<ProducerDefinition> producers, IEnumerable<ConsumerDefinition> consumers)
    {
        Producers = Build(producers).ToImmutableList();
        Consumers = Build(consumers).ToImmutableList();
    }

    private IEnumerable<T> Build<T>(IEnumerable<T> input) where T : HierarchyDefinition
    {
        int position = -1;
        return input.Select(s =>
        {
            s.UpdatePosition(ref position);
            return s;
        });
    }
}