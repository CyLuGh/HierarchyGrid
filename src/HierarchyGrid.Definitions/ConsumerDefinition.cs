using System;
using System.Linq;
using System.Reactive.Linq;
using LanguageExt;
using ReactiveUI;

namespace HierarchyGrid.Definitions;

/// <summary>
/// Represents a definition for a consumer node in a hierarchy-based grid system.
/// This class extends the functionality provided by the <see cref="HierarchyDefinition"/> base class
/// and includes specific properties and methods related to consumer functionality.
/// </summary>
public class ConsumerDefinition : HierarchyDefinition
{
    public ConsumerDefinitionId ConsumerDefinitionId { get; private set; }
    public override Guid DefinitionId => ConsumerDefinitionId;

    public override void SetId(Guid id)
    {
        ConsumerDefinitionId = new ConsumerDefinitionId(id);
    }

    public ConsumerDefinition(Guid? id = null)
        : base(id) { }

    public Func<object, object>? Consumer { get; set; } = o => o;
    public Func<object, string>? Formatter { get; set; } = o => o.ToString() ?? string.Empty;
    public Func<object, Qualification>? Qualify { get; set; } = _ => Qualification.Normal;
    public Func<object, (ThemeColor, ThemeColor)>? Colorize { get; set; }
    public Func<object, object, string>? TooltipCreator { get; set; }

    /// <summary>
    /// Function that provides custom left-side decorations for display in the UI.
    /// Takes two input parameters: the raw input object and its transformed data,
    /// returning a string that represents the left-side decoration content.
    /// </summary>
    public Func<object, object, string>? LeftDecor { get; set; }

    /// <summary>
    /// Function that provides custom right-side decorations for display in the UI.
    /// Takes two input parameters: the raw input object and its transformed data,
    /// returning a string that represents the right-side decoration content.
    /// </summary>
    public Func<object, object, string>? RightDecor { get; set; }

    /// <summary>
    /// Func that will be called from editing textbox, input being string from textbox and bool being the success state of the update.
    /// </summary>
    public Func<object, object, string, bool>? Editor { get; set; }

    /// <summary>
    /// Indicates that the cell can't be edited. The first parameter is raw data from the producer, and the second is the result from the consumer.
    /// </summary>
    public Func<object, object, bool>? IsLocked { get; set; }

    /// <summary>
    /// A function that defines context-specific items for display or interaction.
    /// Input is given by the producer.
    /// Returns an array of tuples, each containing
    /// a string description and an associated action to be executed with a ResultSet parameter.
    /// </summary>
    public Func<
        object,
        (string description, Action<ResultSet> action)[]
    >? ContextItems { get; set; }

    private Qualification GetQualification(InputSet inputSet, object data) =>
        inputSet.Qualifier != Qualification.Unset
            ? inputSet.Qualifier
            : Qualify?.Invoke(data) ?? Qualification.Normal;

    public ResultSet Process(InputSet inputSet)
    {
        var data = Consumer is not null ? Consumer(inputSet.Input) : inputSet.Input;

        var (background, foreground) = inputSet.CustomColors.Match(
            c => c,
            () => Colorize?.Invoke(data) ?? (Option<ThemeColor>.None, Option<ThemeColor>.None)
        );

        /* A cell can't be edited if the whole producer is read-only, or if the IsLocked func returns true. */
        var locked = inputSet.IsLocked || (IsLocked != null && IsLocked(inputSet.Input, data));

        var editor = Option<Func<string, bool>>.None;
        if (Editor is not null && !locked)
        {
            bool Edit(string input) => Editor(inputSet.Input, data, input);
            editor = Option<Func<string, bool>>.Some(Edit);
        }

        var tooltipText = GenerateTooltipContent(inputSet, data);

        var contextCommands = Option<(string, Action<ResultSet>)[]>.None;

        if (ContextItems != null)
        {
            contextCommands = ContextItems(inputSet.Input);
        }

        Option<string> leftDecor = LeftDecor is not null
            ? LeftDecor(inputSet.Input, data)
            : Option<string>.None;
        Option<string> rightDecor = RightDecor is not null
            ? RightDecor(inputSet.Input, data)
            : Option<string>.None;

        var resultSet = new ResultSet
        {
            ProducerId = inputSet.ProducerId,
            ConsumerId = ConsumerDefinitionId,
            Qualifier = GetQualification(inputSet, data),
            Result = (Formatter is not null ? Formatter(data) : data.ToString()) ?? string.Empty,
            BackgroundColor = background,
            ForegroundColor = foreground,
            Editor = editor,
            TooltipText = tooltipText,
            ContextCommands = contextCommands,
            LeftDecor = leftDecor,
            RightDecor = rightDecor,
        };

        return resultSet;
    }

    private Option<string> GenerateTooltipContent(InputSet inputSet, object data)
    {
        var tooltipText = TooltipCreator is not null
            ? TooltipCreator(inputSet.Input, data)
            : string.Empty;

        return !string.IsNullOrEmpty(tooltipText)
            ? Option<string>.Some(tooltipText)
            : Option<string>.None;
    }
}
