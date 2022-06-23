using LanguageExt;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace HierarchyGrid.Definitions
{
    public enum CopyMode
    {
        Flat, Structure, Selection, Highlights
    }

    public partial class HierarchyGridViewModel
    {
        private string CreateClipboardContent( CopyMode mode )
            => mode switch
            {
                CopyMode.Flat => CreateClipboardFlatContent( GetRows( mode ) , GetColumns( mode ) ),
                CopyMode.Structure => CreateClipboardStructuredContent( GetRows( mode ) , GetColumns( mode ) ),
                CopyMode.Highlights => CreateClipboardFlatContent( GetRows( mode ) , GetColumns( mode ) ),
                CopyMode.Selection => CreateClipboardFlatContent( GetRows( mode ) , GetColumns( mode ) ),
                _ => string.Empty
            };

        private Arr<HierarchyDefinition> GetRows( CopyMode mode )
        {
            switch ( mode )
            {
                case CopyMode.Flat:
                    return RowsDefinitions.Leaves().ToArr();

                case CopyMode.Structure:
                    return RowsDefinitions.FlatList( false ).ToArr();

                case CopyMode.Highlights:
                    var leaves = RowsDefinitions.Leaves().ToArr();
                    if ( leaves.Any( l => l.IsHighlighted ) )
                        return leaves.Where( l => l.IsHighlighted );
                    return leaves;

                case CopyMode.Selection:
                    var selected = Selections.Select( s => !IsTransposed ? s.ProducerDefinition as HierarchyDefinition : s.ConsumerDefinition )
                        .Distinct().ToArr();

                    if ( selected.Length > 0 )
                        return selected;

                    return Arr<HierarchyDefinition>.Empty;

                default:
                    return RowsDefinitions.ToArr();
            }
        }

        private Arr<HierarchyDefinition> GetColumns( CopyMode mode )
        {
            switch ( mode )
            {
                case CopyMode.Flat:
                    return ColumnsDefinitions.Leaves().ToArr();

                case CopyMode.Structure:
                    return ColumnsDefinitions.FlatList( false ).ToArr();

                case CopyMode.Highlights:
                    var leaves = ColumnsDefinitions.Leaves().ToArr();
                    if ( leaves.Any( l => l.IsHighlighted ) )
                        return leaves.Where( l => l.IsHighlighted );
                    return leaves;

                case CopyMode.Selection:
                    var selected = Selections.Select( s => !IsTransposed ? s.ConsumerDefinition as HierarchyDefinition : s.ProducerDefinition )
                        .Distinct().ToArr();

                    if ( selected.Length > 0 )
                        return selected;

                    return Arr<HierarchyDefinition>.Empty;

                default:
                    return ColumnsDefinitions.ToArr();
            }
        }

        private static string CreateClipboardFlatContent( Arr<HierarchyDefinition> rows , Arr<HierarchyDefinition> columns )
        {
            var sb = new StringBuilder();

            const char separator = '\t';

            // Skip first cell
            sb.Append( separator );

            // Columns titles
            foreach ( var column in columns )
                sb.Append( column.Content ).Append( separator );

            sb.Length--;
            sb.AppendLine();

            foreach ( var row in rows )
            {
                sb.Append( row.Content ).Append( separator );

                foreach ( var column in columns )
                {
                    sb.Append( Resolve( row , column )
                        .Some( rs => rs.Result )
                        .None( () => string.Empty ) );
                    sb.Append( separator );
                }

                sb.Length--;
                sb.AppendLine();
            }

            return sb.ToString();
        }

        private static string CreateClipboardStructuredContent( Arr<HierarchyDefinition> rows , Arr<HierarchyDefinition> columns )
        {
            var sb = new StringBuilder();

            const char separator = '\t';

            var rowDepth = rows.TotalDepth( false );
            var colDepth = columns.TotalDepth( false );

            for ( int i = 0 ; i < colDepth ; i++ )
            {
                var currentLevel = i;

                // Skip cells corresponding to rows depth
                for ( int _ = 0 ; _ < rowDepth ; _++ )
                    sb.Append( separator );

                var currentLevelColumns = columns.Where( c => c.Level == currentLevel );
                foreach ( var column in currentLevelColumns )
                    for ( int _ = 0 ; _ < column.Span ; _++ )
                        sb.Append( column.Content ).Append( separator );

                sb.Length--;
                sb.AppendLine();
            }

            var columnLeaves = columns.Roots().Leaves().ToArr();

            foreach ( var leafRow in rows.Roots().Leaves() )
            {
                var path = leafRow.Path;
                int position = 0;

                foreach ( var row in path )
                {
                    sb.Append( row.Content ).Append( separator );
                    position++;
                }

                for ( int _ = position ; _ < rowDepth ; _++ )
                    sb.Append( separator );

                foreach ( var column in columnLeaves )
                {
                    sb.Append( Resolve( leafRow , column )
                        .Some( rs => rs.Result )
                        .None( () => string.Empty ) );
                    sb.Append( separator );
                }

                sb.Length--;
                sb.AppendLine();
            }

            return sb.ToString();
        }

        private static Option<(Guid, Guid)> Identify( HierarchyDefinition rowDef , HierarchyDefinition colDef )
        {
            if ( rowDef is ProducerDefinition p && colDef is ConsumerDefinition c )
                return Option<(Guid, Guid)>.Some( (p.Guid, c.Guid) );
            else if ( rowDef is ConsumerDefinition cr && colDef is ProducerDefinition pr )
                return Option<(Guid, Guid)>.Some( (pr.Guid, cr.Guid) );

            return Option<(Guid, Guid)>.None;
        }

        private static Option<ResultSet> Resolve( HierarchyDefinition rowDef , HierarchyDefinition colDef )
        {
            if ( rowDef is ProducerDefinition p && colDef is ConsumerDefinition c )
                return Option<ResultSet>.Some( HierarchyDefinition.Resolve( p , c ) );
            else if ( rowDef is ConsumerDefinition cr && colDef is ProducerDefinition pr )
                return Option<ResultSet>.Some( HierarchyDefinition.Resolve( pr , cr ) );

            return Option<ResultSet>.None;
        }
    }
}