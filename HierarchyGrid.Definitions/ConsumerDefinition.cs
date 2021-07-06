using LanguageExt;
using ReactiveUI;
using System;
using System.Linq;
using System.Windows.Input;

namespace HierarchyGrid.Definitions
{
    public class ConsumerDefinition : HierarchyDefinition
    {
        public Func<object , object> Consumer { get; set; } = o => o;
        public Func<object , string> Formatter { get; set; } = o => o.ToString();
        public Func<object , Qualification> Qualify { get; set; } = _ => Qualification.Normal;
        public Func<object , ((byte a, byte r, byte g, byte b), (byte a, byte r, byte g, byte b))> Colorize { get; set; }

        /// <summary>
        /// Func that will be called from editing textbox, input being string from textbox and bool being the success state of the update.
        /// </summary>
        public Func<object , object , string , bool> Editor { get; set; }

        /// <summary>
        /// Indicates that the cell can't be edited. First parameter is raw data from producer and second is the result from the consumer.
        /// </summary>
        public Func<object , object , bool> IsLocked { get; set; }

        public Func<object , (string description, Action<ResultSet> action)[]> ContextItems { get; set; }

        public ResultSet Process( InputSet inputSet )
        {
            var resultSet = new ResultSet
            {
                ProducerPosition = inputSet.ProducerPosition ,
                ConsumerPosition = Position
            };

            var data = Consumer != null ? Consumer( inputSet.Input ) : inputSet.Input;
            resultSet.Result = Formatter != null ? Formatter( data ) : data.ToString();

            if ( inputSet.Qualifier != Qualification.Unset )
                resultSet.Qualifier = inputSet.Qualifier;
            else
                resultSet.Qualifier = Qualify != null ? Qualify( data ) : Qualification.Normal;

            inputSet.CustomColors.Match( c =>
                {
                    var (back, fore) = c;
                    resultSet.BackgroundColor = Option<(byte a, byte r, byte g, byte b)>.Some( back );
                    resultSet.ForegroundColor = Option<(byte a, byte r, byte g, byte b)>.Some( fore );
                } ,
                () =>
                {
                    if ( Colorize != null )
                    {
                        var (background, foreground) = Colorize( data );
                        resultSet.BackgroundColor = Option<(byte a, byte r, byte g, byte b)>.Some( background );
                        resultSet.ForegroundColor = Option<(byte a, byte r, byte g, byte b)>.Some( foreground );
                    }
                    else
                    {
                        resultSet.BackgroundColor = Option<(byte a, byte r, byte g, byte b)>.None;
                        resultSet.ForegroundColor = Option<(byte a, byte r, byte g, byte b)>.None;
                    }
                } );

            var locked = inputSet.IsLocked || ( IsLocked != null && IsLocked( inputSet.Input , data ) );

            if ( Editor != null && !locked )
            {
                Func<string , bool> edit = ( string input ) => Editor( inputSet.Input , data , input );
                resultSet.Editor = Option<Func<string , bool>>.Some( edit );
            }
            else
                resultSet.Editor = Option<Func<string , bool>>.None;

            if ( ContextItems != null )
            {
                var cis = ContextItems( inputSet.Input );
                if ( cis.Length > 0 )
                {
                    resultSet.ContextCommands = Option<(string, ICommand)[]>
                        .Some( cis.Select( ci => (ci.description, (ICommand) ReactiveCommand.Create( () => ci.action( resultSet ) )) )
                                          .ToArray() );
                }
                else
                    resultSet.ContextCommands = Option<(string, ICommand)[]>.None;
            }
            else
                resultSet.ContextCommands = Option<(string, ICommand)[]>.None;

            return resultSet;
        }
    }
}