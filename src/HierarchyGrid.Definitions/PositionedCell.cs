using System;

namespace HierarchyGrid.Definitions;

/// <summary>
/// Represents a positioned cell within a hierarchy grid, defined by its
/// producer and consumer definitions, spatial properties, and associated result set.
/// </summary>
/// <remarks>
/// This class is immutable and serves as a fundamental unit in a hierarchical grid system,
/// allowing comparison and equality checks based on its properties.
/// </remarks>
public sealed class PositionedCell : IEquatable<PositionedCell>, IComparable<PositionedCell>
{
    public required ProducerDefinition ProducerDefinition { get; init; }
    public required ConsumerDefinition ConsumerDefinition { get; init; }
    public int HorizontalPosition { get; init; }
    public int VerticalPosition { get; init; }
    public double Top { get; init; }
    public double Left { get; init; }
    public double Height { get; init; }
    public double Width { get; init; }
    public ResultSet ResultSet { get; init; } = ResultSet.Default;

    public int CompareTo(PositionedCell? other)
    {
        if (other is null)
            return 1;

        return
            ProducerDefinition?.CompareTo(other.ProducerDefinition) == 0
            && ConsumerDefinition?.CompareTo(other.ConsumerDefinition) == 0
            ? 0
            : 1;
    }

    public bool Equals(PositionedCell? other)
    {
        if (other == null)
            return false;

        return ProducerDefinition.ProducerDefinitionId.Equals(
                other.ProducerDefinition.ProducerDefinitionId
            )
            && ConsumerDefinition.ConsumerDefinitionId.Equals(
                other.ConsumerDefinition.ConsumerDefinitionId
            );
    }

    public override bool Equals(object? obj) => Equals(obj as PositionedCell);

    public override int GetHashCode() =>
        HashCode.Combine(ProducerDefinition?.DefinitionId, ConsumerDefinition?.DefinitionId);
}
