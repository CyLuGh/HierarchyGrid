﻿using LanguageExt;
using System;

namespace HierarchyGrid.Definitions
{
    public class ConsumerDefinition : HierarchyDefinition
    {
        public Func<object, object> Consumer { get; set; } = o => o;
        public Func<object, string> Formatter { get; set; } = o => o.ToString();
        public Func<object, Qualification> Qualify { get; set; } = _ => Qualification.Normal;
        public Func<object, (byte a, byte r, byte g, byte b)> Colorize { get; set; }

        /// <summary>
        /// Func that will be called from editing textbox, input being string from textbox and bool being the success state of the update.
        /// </summary>
        public Func<object, object, string, bool> Editor { get; set; }

        /// <summary>
        /// Indicates that the cell can't be edited. First parameter is raw data from producer and second is the result from the consumer.
        /// </summary>
        public Func<object, object, bool> IsLocked { get; set; }

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

            var locked = inputSet.IsLocked || (IsLocked != null && IsLocked(inputSet.Input, data));

            if (Editor != null && !locked)
            {
                Func<string, bool> edit = (string input) => Editor(inputSet.Input, data, input);
                resultSet.Editor = Option<Func<string, bool>>.Some(edit);
            }
            else
                resultSet.Editor = Option<Func<string, bool>>.None;

            return resultSet;
        }
    }
}