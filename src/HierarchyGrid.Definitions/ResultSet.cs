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
        ProducerId = default;
        ConsumerId = default;
    }

    public static ResultSet Default { get; } = new ResultSet { Qualifier = Qualification.Empty };

    public string Result { get; init; }
    public Qualification Qualifier { get; init; }
    public Option<ThemeColor> BackgroundColor { get; init; } = Option<ThemeColor>.None;
    public Option<ThemeColor> ForegroundColor { get; init; } = Option<ThemeColor>.None;
    public Option<string> TooltipText { get; init; }

    public Option<Func<string, bool>> Editor { get; init; } = Option<Func<string, bool>>.None;
    public Option<(
        string,
        ReactiveCommand<ResultSet, System.Reactive.Unit>
    )[]> ContextCommands { get; init; } =
        Option<(string, ReactiveCommand<ResultSet, System.Reactive.Unit>)[]>.None;

    public Guid ProducerId { get; init; }
    public Guid ConsumerId { get; init; }
}
