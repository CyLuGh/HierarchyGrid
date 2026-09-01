using System;

namespace HierarchyGrid.Definitions;

/// <summary>
/// Defines a contract for identifiable definitions within a hierarchical grid structure.
/// This interface ensures that implementing classes or structures possess a unique identifier (<see cref="Guid"/>)
/// and provide mechanisms to modify this identifier.
/// It serves as a foundational interface for hierarchical entities to ensure consistency
/// in identification across the system.
/// </summary>
public interface IIdentifiedDefinition
{
    Guid DefinitionId { get; }
    void SetId(Guid id);
}

/// <summary>
/// Represents a uniquely identifiable producer definition within a hierarchical grid structure.
/// This structure is implemented as a value type and ensures the encapsulation of a unique identifier (GUID).
/// Additionally, it supports implicit conversions to and from a <see cref="Guid"/>,
/// enabling seamless integration with other components that rely on GUIDs for identification.
/// </summary>
public readonly record struct ProducerDefinitionId(Guid Id)
{
    public static ProducerDefinitionId Default => new(Guid.Empty);

    public static implicit operator Guid(ProducerDefinitionId id) => id.Id;

    public static implicit operator ProducerDefinitionId(Guid id) => new(id);
}

/// <summary>
/// Represents a uniquely identifiable consumer definition within a hierarchical grid structure.
/// This structure is implemented as a value type and encapsulates a unique identifier (GUID).
/// It supports implicit conversions to and from a <see cref="Guid"/>,
/// simplifying interoperability with systems or components that utilize GUIDs for identification purposes.
/// </summary>
public readonly record struct ConsumerDefinitionId(Guid Id)
{
    public static ConsumerDefinitionId Default => new(Guid.Empty);

    public static implicit operator Guid(ConsumerDefinitionId id) => id.Id;

    public static implicit operator ConsumerDefinitionId(Guid id) => new(id);
}

/// <summary>
/// Represents a unique identifier for the relationship between a producer and a consumer
/// within a hierarchical grid structure. This structure encapsulates a combination of
/// unique producer and consumer identifiers, ensuring distinct identification of
/// the producer-consumer pair.
/// </summary>
public readonly record struct CellId(
    ProducerDefinitionId ProducerId,
    ConsumerDefinitionId ConsumerId
);
