using LanguageExt;
using System;

namespace HierarchyGrid.Definitions
{
    public class ProducerDefinition : HierarchyDefinition
    {
        public Func<object> Producer { get; set; }
        public Func<Qualification> Qualify { get; set; } = () => Qualification.Unset;

        /// <summary>
        /// Indicates that entire row shouldn't be editable.
        /// </summary>
        public bool IsLocked { get; set; }

        public Option<InputSet> Produce() => Producer != null ?
        Option<InputSet>.Some(new InputSet
        {
            Input = Producer(),
            ProducerPosition = Position,
            Qualifier = Qualify != null ? Qualify() : Qualification.Unset,
            IsLocked = IsLocked
        })
        : Option<InputSet>.None;
    }
}