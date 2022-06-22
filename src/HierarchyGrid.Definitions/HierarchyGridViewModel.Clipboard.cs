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
                CopyMode.Flat => CreateClipboardFlatContent( RowsDefinitions.Leaves().ToArr() , ColumnsDefinitions.Leaves().ToArr() ),
                CopyMode.Structure => CreateClipboardStructuredContent( RowsDefinitions.Leaves().ToArr() , ColumnsDefinitions.Leaves().ToArr() ),
                _ => string.Empty
            };

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