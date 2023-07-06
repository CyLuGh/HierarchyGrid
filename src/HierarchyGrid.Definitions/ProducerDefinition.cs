using LanguageExt;
using System;

namespace HierarchyGrid.Definitions
{
    public class ProducerDefinition : HierarchyDefinition, IComparable<ProducerDefinition>
    {
        public ProducerDefinition( Guid? id = null ) : base( id ) { }

        public Func<object> Producer { get; set; }
        public Func<Qualification> Qualify { get; set; } = () => Qualification.Unset;

        /// <summary>
        /// Indicates that entire row shouldn't be editable.
        /// </summary>
        public bool IsLocked { get; set; }

        public int CompareTo( ProducerDefinition other )
        {
            return Content.ToString().CompareTo( other.Content.ToString() );
        }

        public Option<InputSet> Produce() => Producer != null ?
        Option<InputSet>.Some( new InputSet
        {
            Input = Producer() ,
            ProducerId = Guid ,
            Qualifier = Qualify?.Invoke() ?? Qualification.Unset ,
            IsLocked = IsLocked
        } )
        : Option<InputSet>.None;
    }
}