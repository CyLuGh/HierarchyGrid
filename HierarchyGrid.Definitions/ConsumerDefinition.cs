using LanguageExt;
using System;

namespace HierarchyGrid.Definitions
{
    public class ConsumerDefinition : HierarchyDefinition
    {
        public Func<object, object> Consumer { get; set; } = o => o;
        public Func<object, string> Formatter { get; set; } = o => o.ToString();
        public Func<object, Qualification> Qualify { get; set; } = _ => Qualification.Normal;
        public Func<object, (byte a, byte r, byte g, byte b)> Colorize { get; set; }

        public ResultSet Process(InputSet inputSet)
        {
            var resultSet = new ResultSet();

            var data = Consumer != null ? Consumer(inputSet.Input) : inputSet.Input;
            resultSet.Result = Formatter != null ? Formatter(data) : data.ToString();

            if (inputSet.Qualifier != Qualification.Unset)
                resultSet.Qualifier = inputSet.Qualifier;
            else
                resultSet.Qualifier = Qualify != null ? Qualify(data) : Qualification.Normal;

            inputSet.CustomColor.Match(c => resultSet.CustomColor = Option<(byte a, byte r, byte g, byte b)>.Some(c),
                () =>
                {
                    if (Colorize != null)
                        resultSet.CustomColor = Option<(byte a, byte r, byte g, byte b)>.Some(Colorize(data));
                    else
                        resultSet.CustomColor = Option<(byte a, byte r, byte g, byte b)>.None;
                });

            return resultSet;
        }
    }
}