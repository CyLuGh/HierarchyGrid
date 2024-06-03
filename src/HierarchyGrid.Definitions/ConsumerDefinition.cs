using System;
using System.Linq;
using System.Windows.Input;
using LanguageExt;
using ReactiveUI;

namespace HierarchyGrid.Definitions;

public class ConsumerDefinition : HierarchyDefinition
{
    public ConsumerDefinition( Guid? id = null ) : base( id )
    {
    }

    public Func<object , object>? Consumer { get; set; } = o => o;
    public Func<object , string>? Formatter { get; set; } = o => o?.ToString() ?? string.Empty;
    public Func<object , Qualification>? Qualify { get; set; } = _ => Qualification.Normal;
    public Func<object , (ThemeColor , ThemeColor)>? Colorize { get; set; }
    public Func<object , object , string>? TooltipCreator { get; set; }

    /// <summary>
    /// Func that will be called from editing textbox, input being string from textbox and bool being the success state of the update.
    /// </summary>
    public Func<object , object , string , bool>? Editor { get; set; }

    /// <summary>
    /// Indicates that the cell can't be edited. First parameter is raw data from producer and second is the result from the consumer.
    /// </summary>
    public Func<object , object , bool>? IsLocked { get; set; }

    public Func<object , (string description , Action<ResultSet> action)[]>? ContextItems { get; set; }

    private Qualification GetQualification( InputSet inputSet , object data ) =>
        inputSet.Qualifier != Qualification.Unset
            ? inputSet.Qualifier
            : Qualify?.Invoke( data ) ?? Qualification.Normal;

    public ResultSet Process( InputSet inputSet )
    {
        var data = Consumer != null ? Consumer( inputSet.Input ) : inputSet.Input;
        var (background , foreground) = inputSet.CustomColors.Match( c => c ,
            () => Colorize?.Invoke( data ) ?? ( Option<ThemeColor>.None , Option<ThemeColor>.None ) );
        var locked = inputSet.IsLocked || ( IsLocked != null && IsLocked( inputSet.Input , data ) );
        var editor = Option<Func<string , bool>>.None;
        if ( Editor != null && !locked )
        {
            bool Edit( string input ) => Editor( inputSet.Input , data , input );
            editor = Option<Func<string , bool>>.Some( Edit );
        }
        
        var tooltipText = TooltipCreator != null ? TooltipCreator( inputSet.Input , data ) : string.Empty;
        var tt = !string.IsNullOrEmpty( tooltipText )
            ? Option<string>.Some( tooltipText )
            : Option<string>.None;

        var contextCommands = Option<(string , ReactiveCommand<ResultSet,System.Reactive.Unit>)[]>.None;
        if ( ContextItems != null )
        {
            var cis = ContextItems( inputSet.Input );
            if ( cis.Length > 0 )

            contextCommands= cis.Select( ci =>
                    ( ci.description , ReactiveCommand.Create( (ResultSet rs) => ci.action( rs ) ) ) )
                .ToArray() ;
        }

        var resultSet = new ResultSet
        {
            ProducerId = inputSet.ProducerId ,
            ConsumerId = Guid ,
            Qualifier = GetQualification( inputSet , data ) ,
            Result = ( Formatter != null ? Formatter( data ) : data.ToString() ) ?? string.Empty ,
            BackgroundColor = background ,
            ForegroundColor = foreground ,
            Editor = editor, 
            TooltipText = tt,
            ContextCommands = contextCommands
        };
        
        return resultSet;
    }
}