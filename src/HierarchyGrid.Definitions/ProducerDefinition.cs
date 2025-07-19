using System;
using LanguageExt;

namespace HierarchyGrid.Definitions;

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

    public Func<object>? Producer { get; set; }
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
                    ProducerId = DefinitionId,
                    Qualifier = Qualify?.Invoke() ?? Qualification.Unset,
                    IsLocked = IsLocked,
                }
            )
            : Option<InputSet>.None;
}
