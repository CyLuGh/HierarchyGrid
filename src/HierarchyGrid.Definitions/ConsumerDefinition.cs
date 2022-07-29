using LanguageExt;
using ReactiveUI;
using System;
using System.Linq;
using System.Windows.Input;

namespace HierarchyGrid.Definitions
{
    public class ConsumerDefinition : HierarchyDefinition
    {
        public ConsumerDefinition( Guid? id = null ) : base( id ) {}

        public Func<object , object> Consumer { get; set; } = o => o;
        public Func<object , string> Formatter { get; set; } = o => o.ToString();
        public Func<object , Qualification> Qualify { get; set; } = _ => Qualification.Normal;
        public Func<object , (ThemeColor, ThemeColor)> Colorize { get; set; }
        public Func<object , object , string> TooltipCreator { get; set; }

        /// <summary>
        /// Func that will be called from editing textbox, input being string from textbox and bool being the success state of the update.
        /// </summary>
        public Func<object , object , string , bool> Editor { get; set; }

        /// <summary>
        /// Indicates that the cell can't be edited. First parameter is raw data from producer and second is the result from the consumer.
        /// </summary>
        public Func<object , object , bool> IsLocked { get; set; }

        public Func<object , (string description, Action<ResultSet> action)[]> ContextItems { get; set; }

        private Qualification GetQualification( InputSet inputSet , object data )
            => inputSet.Qualifier != Qualification.Unset
                ? inputSet.Qualifier
                : Qualify != null ? Qualify( data ) : Qualification.Normal;

        public ResultSet Process( InputSet inputSet )
        {
            var data = Consumer != null ? Consumer( inputSet.Input ) : inputSet.Input;

            var resultSet = new ResultSet
            {
                ProducerId = inputSet.ProducerId ,
                ConsumerId = Guid ,
                Qualifier = GetQualification( inputSet , data ) ,
                Result = Formatter != null ? Formatter( data ) : data.ToString()
            };

            inputSet.CustomColors.Match( c =>
                {
                    var (back, fore) = c;
                    resultSet.BackgroundColor = back;
                    resultSet.ForegroundColor = fore;
                } ,
                () =>
                {
                    if ( Colorize != null )
                    {
                        var (background, foreground) = Colorize( data );
                        resultSet.BackgroundColor = background;
                        resultSet.ForegroundColor = foreground;
                    }
                    else
                    {
                        resultSet.BackgroundColor = Option<ThemeColor>.None;
                        resultSet.ForegroundColor = Option<ThemeColor>.None;
                    }
                } );

            var locked = inputSet.IsLocked || ( IsLocked != null && IsLocked( inputSet.Input , data ) );

            if ( Editor != null && !locked )
            {
                Func<string , bool> edit = ( string input ) => Editor( inputSet.Input , data , input );
                resultSet.Editor = Option<Func<string , bool>>.Some( edit );
            }
            else
            {
                resultSet.Editor = Option<Func<string , bool>>.None;
            }

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
                {
                    resultSet.ContextCommands = Option<(string, ICommand)[]>.None;
                }
            }
            else
            {
                resultSet.ContextCommands = Option<(string, ICommand)[]>.None;
            }

            var tooltipText = TooltipCreator != null ? TooltipCreator( inputSet.Input , data ) : string.Empty;
            resultSet.TooltipText = !string.IsNullOrEmpty(tooltipText) ?
                Option<string>.Some( tooltipText ) : Option<string>.None;

            return resultSet;
        }
    }
}