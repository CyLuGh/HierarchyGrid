using System;
using LanguageExt;

namespace HierarchyGrid.Definitions;

/// <summary>
/// Represents a definition for a producer within a hierarchy. This class provides
/// mechanisms to generate input sets and manage producer-specific properties.
/// </summary>
public class ProducerDefinition : HierarchyDefinition
{
    public ProducerDefinitionId ProducerDefinitionId { get; private set; }
    public override Guid DefinitionId => ProducerDefinitionId;

    public override void SetId(Guid id)
    {
        ProducerDefinitionId = new ProducerDefinitionId(id);
    }

    public ProducerDefinition(Guid? id = null)
        : base(id) { }

    /// <summary>
    /// Represents a delegate that produces an object when invoked.
    /// Typically used for defining dynamic content generation in the hierarchy structure.
    /// </summary>
    public Func<object>? Producer { get; set; }

    /// <summary>
    /// Represents a delegate to determine the qualification of an input or element within the hierarchical grid structure.
    /// The <see cref="Qualify"/> property provides a way to dynamically evaluate and assign a
    /// <see cref="Qualification"/> based on custom logic at runtime.
    /// </summary>
    /// <remarks>
    /// The qualification status is useful for categorizing or applying specific rules or behaviors
    /// to elements in the hierarchy, such as marking them as read-only, highlighted, or in an error state.
    /// When not explicitly defined, defaults to <see cref="Qualification.Unset"/>.
    /// </remarks>
    public Func<Qualification>? Qualify { get; set; } = () => Qualification.Unset;

    /// <summary>
    /// Indicates that the entire row shouldn't be editable.
    /// </summary>
    public bool IsLocked { get; set; }

    public Option<InputSet> Produce() =>
        Producer != null
            ? Option<InputSet>.Some(
                new InputSet
                {
                    Input = Producer(),
                    ProducerId = ProducerDefinitionId,
                    Qualifier = Qualify?.Invoke() ?? Qualification.Unset,
                    IsLocked = IsLocked,
                }
            )
            : Option<InputSet>.None;
}
