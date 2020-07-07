using LanguageExt;
using System;
using System.Collections.Generic;
using System.Text;

namespace HierarchyGrid.Definitions
{
    public class ProducerDefinition : HierarchyDefinition
    {
        public Func<object> Producer { get; set; }
        public Func<Qualification> Qualify {get;set;} = () => Qualification.Unset;

        public Option<InputSet> Produce() => Producer != null ? 
        Option<InputSet>.Some( new InputSet {
            Input = Producer()
        }) 
        : Option<InputSet>.None;
    }
}