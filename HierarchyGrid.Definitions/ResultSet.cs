using LanguageExt;
using System;

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
        public Option<(byte a, byte r, byte g, byte b)> CustomColor { get; set; } = Option<(byte a, byte r, byte g, byte b)>.None;

        public bool IsLocked { get; set; }
    }

    public class ResultSet
    {
        public string Result { get; set; }
        public Qualification Qualifier { get; set; }
        public Option<(byte a, byte r, byte g, byte b)> CustomColor { get; set; } = Option<(byte a, byte r, byte g, byte b)>.None;

        public Option<Func<string, bool>> Editor { get; set; } = Option<Func<string, bool>>.None;
    }
}