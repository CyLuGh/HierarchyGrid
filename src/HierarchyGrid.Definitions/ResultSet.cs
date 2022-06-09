using LanguageExt;
using ReactiveUI;
using System;
using System.Windows.Input;

namespace HierarchyGrid.Definitions
{
    public enum Qualification
    {
        Unset,
        Empty,
        Normal,
        Error,
        Warning,
        Remark,
        Custom,
        ReadOnly,
        Computed,
        Highlighted,
        Hovered
    }

    public class InputSet
    {
        public object Input { get; set; }

        /// <summary>
        /// Qualifier required by producer for all consumer results
        /// </summary>
        public Qualification Qualifier { get; set; }

        /// <summary>
        /// Brush color required by producer for all consumer results
        /// </summary>
        public Option<(ThemeColor, ThemeColor)> CustomColors { get; set; }
            = Option<(ThemeColor, ThemeColor)>.None;

        public bool IsLocked { get; set; }

        internal Guid ProducerId { get; set; }
    }

    public class ResultSet
    {
        public static ResultSet Default { get; } = new ResultSet { Qualifier = Qualification.Empty };

        public string Result { get; set; }
        public Qualification Qualifier { get; init; }
        public Option<ThemeColor> BackgroundColor { get; set; } = Option<ThemeColor>.None;
        public Option<ThemeColor> ForegroundColor { get; set; } = Option<ThemeColor>.None;
        public Option<string> TooltipText { get; set; }

        public Option<Func<string , bool>> Editor { get; set; } = Option<Func<string , bool>>.None;
        public Option<(string, ICommand)[]> ContextCommands { get; set; } = Option<(string, ICommand)[]>.None;

        public Guid ProducerId { get; set; }
        public Guid ConsumerId { get; set; }
    }
}