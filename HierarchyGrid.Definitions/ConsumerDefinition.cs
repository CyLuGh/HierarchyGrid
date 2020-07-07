using System;
using System.Collections.Generic;
using System.Text;

namespace HierarchyGrid.Definitions
{
    public class ConsumerDefinition : HierarchyDefinition
    {
        public Func<object, object> Consumer { get; set; } = o => o;
        public Func<object, string> Formatter { get; set; } = o => o.ToString();
        public Func<object, Qualification> Qualify { get; set; } = o => Qualification.Normal;

        public ResultSet Process(InputSet inputSet)
        {
            var resultSet = new ResultSet();

            var data = Consumer != null ? Consumer(inputSet.Input) : inputSet.Input;
            resultSet.Result = Formatter != null ? Formatter(data) : data.ToString();

            if (inputSet.Qualifier != Qualification.Unset)
                resultSet.Qualifier = inputSet.Qualifier;
            else
                resultSet.Qualifier = Qualify != null ? Qualify(data) : Qualification.Normal;

            return resultSet;
        }
    }
}