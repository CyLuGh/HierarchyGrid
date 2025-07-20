using System;
using LanguageExt;
using ReactiveUI;

namespace HierarchyGrid.Definitions;

public readonly record struct ResultSet
{
    public ResultSet()
    {
        Result = string.Empty;
        Qualifier = Qualification.Unset;
        TooltipText = default;
        ProducerId = ProducerDefinitionId.Default;
        ConsumerId = ConsumerDefinitionId.Default;
    }

    public ProducerDefinitionId ProducerId { get; init; }
    public ConsumerDefinitionId ConsumerId { get; init; }

    public static ResultSet Default { get; } = new() { Qualifier = Qualification.Empty };

    public string Result { get; init; }

    /// <summary>
    /// Represents the qualification of a given result. This property typically categorizes
    /// or defines the state associated with the result, such as an error, warning, or a computed value.
    /// The value is assigned based on the processing logic or rules applied within the context of
    /// consumer or producer definitions.
    /// </summary>
    public Qualification Qualifier { get; init; }

    public Option<ThemeColor> BackgroundColor { get; init; } = Option<ThemeColor>.None;
    public Option<ThemeColor> ForegroundColor { get; init; } = Option<ThemeColor>.None;
    public Option<string> TooltipText { get; init; }

    /// <summary>
    /// Gets or sets an optional editor function that defines the editing behavior for the cell's value.
    /// The editor function, when provided, validates or defines constraints for the input during editing.
    /// If the cell is locked, this property will have no effect on the editing process. If no editor is
    /// provided, the cell isn't editable.
    /// </summary>
    public Option<Func<string, bool>> Editor { get; init; } = Option<Func<string, bool>>.None;

    /// <summary>
    /// Gets or initializes a collection of context-specific commands that can be associated with a result set.
    /// The commands are represented as an optional array of tuples, where each tuple consists of a display name
    /// (string) and a corresponding reactive command. These commands can provide additional interactive actions
    /// or functionalities for the result set in a reactive application.
    /// If no context commands are defined, the property defaults to being empty.
    /// </summary>
    public Option<(
        string,
        ReactiveCommand<ResultSet, System.Reactive.Unit>
    )[]> ContextCommands { get; init; } =
        Option<(string, ReactiveCommand<ResultSet, System.Reactive.Unit>)[]>.None;

    // TODO: replace object by a real decor object, what should be used for svg?
    public Option<object> LeftDecor { get; init; }
    public Option<object> RightDecor { get; init; }
}
