using System;

namespace HierarchyGrid.Definitions;

public sealed class PositionedCell : IEquatable<PositionedCell>, IComparable<PositionedCell>
{
    public ProducerDefinition? ProducerDefinition { get; init; }
    public ConsumerDefinition? ConsumerDefinition { get; init; }
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

        return ProducerDefinition?.Guid == other.ProducerDefinition?.Guid
               && ConsumerDefinition?.Guid == other.ConsumerDefinition?.Guid;
    }

    public override bool Equals(object? obj) => Equals(obj as PositionedCell);

    public override int GetHashCode() =>
        HashCode.Combine(ProducerDefinition?.Guid, ConsumerDefinition?.Guid);
}