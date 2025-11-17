using LanguageExt;

namespace HierarchyGrid.Definitions;

public readonly record struct InputSet
{
    public InputSet()
    {
        Qualifier = Qualification.Unset;
        IsLocked = false;
        ProducerId = ProducerDefinitionId.Default;
    }

    public required object Input { get; init; }

    /// <summary>
    /// Qualifier required by the producer for all consumer results
    /// </summary>
    public Qualification Qualifier { get; init; }

    /// <summary>
    /// Brush color required by the producer for all consumer results
    /// </summary>
    public Option<(ThemeColor, ThemeColor)> CustomColors { get; init; } =
        Option<(ThemeColor, ThemeColor)>.None;

    /// <summary>
    /// Indicates whether all the cells depending on this producer are read-only.
    /// When true, the entity is prevented from further modifications or edits.
    /// </summary>
    public bool IsLocked { get; init; }

    internal ProducerDefinitionId ProducerId { get; init; }
}
