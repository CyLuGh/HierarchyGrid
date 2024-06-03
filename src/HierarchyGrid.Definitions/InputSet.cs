using LanguageExt;
using System;

namespace HierarchyGrid.Definitions;

public readonly record struct InputSet
{
    public InputSet()
    {
        Input = null;
        Qualifier = Qualification.Unset;
        IsLocked = false;
        ProducerId = default;
    }

    public object? Input { get; init; }

    /// <summary>
    /// Qualifier required by producer for all consumer results
    /// </summary>
    public Qualification Qualifier { get; init; }

    /// <summary>
    /// Brush color required by producer for all consumer results
    /// </summary>
    public Option<(ThemeColor, ThemeColor)> CustomColors { get; init; } =
        Option<(ThemeColor, ThemeColor)>.None;

    public bool IsLocked { get; init; }

    internal Guid ProducerId { get; init; }
}