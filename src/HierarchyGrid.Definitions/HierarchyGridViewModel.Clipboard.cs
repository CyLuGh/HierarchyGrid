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
                CopyMode.Flat => CreateClipboardContent( string.Empty , false , RowsDefinitions.Leaves() , ColumnsDefinitions.Leaves() , Theme ),
                CopyMode.Structure => CreateClipboardContent( string.Empty , true , RowsDefinitions.Leaves() , ColumnsDefinitions.Leaves() , Theme ),
                _ => string.Empty
            };

        private string CreateClipboardContent( string title , bool withStructure , IEnumerable<HierarchyDefinition> rows , IEnumerable<HierarchyDefinition> columns , ITheme theme )
        {
            using var mem = new MemoryStream();
            using var writer = new XmlTextWriter( mem , null );
            writer.Formatting = Formatting.Indented;

            // Header
            WriteXMLHeader( writer , theme );

            WriteXMLTitle( title , writer );

            int maxLevel = withStructure ? RowsDefinitions.Select( o => o.Depth( true ) ).Max() : 1;

            var colDefs = columns as HierarchyDefinition[] ?? columns.ToArray();
            WriteXMLColumnsHeaders( writer , maxLevel , colDefs , withStructure );

            // Rows...
            foreach ( var rowDef in rows )
            {
                writer.WriteStartElement( "Row" );
                var paths = rowDef.Path;

                if ( withStructure )
                    foreach ( var def in paths )
                    {
                        writer.WriteStartElement( "Cell" );
                        if ( ( !def.HasChild || !def.IsExpanded ) && maxLevel - def.Level > 1 ) // Because a span of 0 is still taken into account by LibreOffice
                            writer.WriteAttributeString( "ss:MergeAcross" , ( maxLevel - def.Level - 1 ).ToString() );

                        writer.WriteAttributeString( "ss:StyleID" , "headerstyle" );
                        writer.WriteStartElement( "Data" );
                        writer.WriteAttributeString( "ss:Type" , "String" );
                        writer.WriteString( $"{def.Content}" );
                        writer.WriteEndElement(); //data
                        writer.WriteEndElement(); //cell
                    }
                else
                {
                    writer.WriteStartElement( "Cell" );
                    writer.WriteAttributeString( "ss:StyleID" , "headerstyle" );
                    writer.WriteStartElement( "Data" );
                    writer.WriteAttributeString( "ss:Type" , "String" );
                    writer.WriteString( $"{rowDef.Content}" );

                    writer.WriteEndElement(); //data
                    writer.WriteEndElement(); //cell
                }

                WriteXMLData( writer , colDefs , rowDef );

                writer.WriteEndElement(); //Row
            }

            // Footer
            WriteXMLFooter( writer );

            mem.Position = 0;
            using var reader = new StreamReader( mem );
            return reader.ReadToEnd();
        }

        private Option<(Guid, Guid)> Identify( HierarchyDefinition rowDef , HierarchyDefinition colDef )
        {
            if ( rowDef is ProducerDefinition p && colDef is ConsumerDefinition c )
                return Option<(Guid, Guid)>.Some( (p.Guid, c.Guid) );
            else if ( rowDef is ConsumerDefinition cr && colDef is ProducerDefinition pr )
                return Option<(Guid, Guid)>.Some( (pr.Guid, cr.Guid) );

            return Option<(Guid, Guid)>.None;
        }

        private Option<ResultSet> Resolve( HierarchyDefinition rowDef , HierarchyDefinition colDef )
        {
            if ( rowDef is ProducerDefinition p && colDef is ConsumerDefinition c )
                return Option<ResultSet>.Some( HierarchyDefinition.Resolve( p , c ) );
            else if ( rowDef is ConsumerDefinition cr && colDef is ProducerDefinition pr )
                return Option<ResultSet>.Some( HierarchyDefinition.Resolve( pr , cr ) );

            return Option<ResultSet>.None;
        }

        private void WriteXMLHeader( XmlWriter writer , ITheme theme )
        {
            writer.WriteStartDocument( true );
            writer.WriteStartElement( "Workbook" );
            writer.WriteAttributeString( "xmlns" , "urn:schemas-microsoft-com:office:spreadsheet" );
            writer.WriteAttributeString( "xmlns:o" , "urn:schemas-microsoft-com:office:office" );
            writer.WriteAttributeString( "xmlns:x" , "urn:schemas-microsoft-com:office:excel" );
            writer.WriteAttributeString( "xmlns:ss" , "urn:schemas-microsoft-com:office:spreadsheet" );
            writer.WriteAttributeString( "xmlns:html" , "http://www.w3.org/TR/REC-html40" );
            writer.WriteStartElement( "Styles" );

            // Add color management
            string scol;

            //// Custom
            //ProducersCache.Items.AsParallel()
            //    .SelectMany( p => ConsumersCache.Items.Select( c => HierarchyDefinition.Resolve( p , c ) ) )
            //    .Select( o => o.BackgroundColor )
            //    .Where( c => c.IsSome )
            //    .Distinct()
            //    .ForEach( col =>
            //    {
            //        var (a, r, g, b) = col.Some( c => c )
            //            .None( () => (255, 255, 255, 255) );

            //        scol = FormatExcelColor( a , r , g , b );
            //        writer.WriteStartElement( "Style" );
            //        writer.WriteAttributeString( "ss:ID" , string.Concat( "Custom" , scol ) );
            //        writer.WriteStartElement( "Interior" );
            //        writer.WriteAttributeString( "ss:Color" , scol );
            //        writer.WriteAttributeString( "ss:Pattern" , "Solid" );
            //        writer.WriteEndElement();//Interior
            //        writer.WriteEndElement();//Style
            //    } );

            // Computed
            writer.WriteStartElement( "Style" );
            writer.WriteAttributeString( "ss:ID" , "Computed" );
            writer.WriteStartElement( "Interior" );
            scol = FormatExcelColor( theme.ComputedBackgroundColor );
            writer.WriteAttributeString( "ss:Color" , scol );
            writer.WriteAttributeString( "ss:Pattern" , "Solid" );
            writer.WriteEndElement();//Interior
            writer.WriteEndElement();//Style

            // Error
            writer.WriteStartElement( "Style" );
            writer.WriteAttributeString( "ss:ID" , "Error" );
            writer.WriteStartElement( "Interior" );
            scol = FormatExcelColor( theme.ErrorBackgroundColor );
            writer.WriteAttributeString( "ss:Color" , scol );
            writer.WriteAttributeString( "ss:Pattern" , "Solid" );
            writer.WriteEndElement();//Interior
            writer.WriteEndElement();//Style

            // Warning
            writer.WriteStartElement( "Style" );
            writer.WriteAttributeString( "ss:ID" , "Warning" );
            writer.WriteStartElement( "Interior" );
            scol = FormatExcelColor( theme.WarningBackgroundColor );
            writer.WriteAttributeString( "ss:Color" , scol );
            writer.WriteAttributeString( "ss:Pattern" , "Solid" );
            writer.WriteEndElement();//Interior
            writer.WriteEndElement();//Style

            // Remark
            writer.WriteStartElement( "Style" );
            writer.WriteAttributeString( "ss:ID" , "Remark" );
            writer.WriteStartElement( "Interior" );
            scol = FormatExcelColor( theme.RemarkBackgroundColor );
            writer.WriteAttributeString( "ss:Color" , scol );
            writer.WriteAttributeString( "ss:Pattern" , "Solid" );
            writer.WriteEndElement();//Interior
            writer.WriteEndElement();//Style

            writer.WriteStartElement( "Style" );
            writer.WriteAttributeString( "ss:ID" , "headerstyle" );
            writer.WriteStartElement( "Borders" );

            writer.WriteStartElement( "Border" );
            writer.WriteAttributeString( "ss:Position" , "Bottom" );
            writer.WriteAttributeString( "ss:LineStyle" , "Continuous" );
            writer.WriteAttributeString( "ss:Weight" , "1" );
            writer.WriteEndElement();//Border

            writer.WriteStartElement( "Border" );
            writer.WriteAttributeString( "ss:Position" , "Right" );
            writer.WriteAttributeString( "ss:LineStyle" , "Continuous" );
            writer.WriteAttributeString( "ss:Weight" , "1" );
            writer.WriteEndElement();//Border

            writer.WriteEndElement();//Borders
            writer.WriteStartElement( "Interior" );
            writer.WriteAttributeString( "ss:Color" , "#C0C0C0" );
            writer.WriteAttributeString( "ss:Pattern" , "Solid" );
            writer.WriteEndElement();//Interior

            writer.WriteEndElement();//Style

            //percentage format
            writer.WriteEndElement();//Styles

            writer.WriteStartElement( "Worksheet" );
            writer.WriteAttributeString( "ss:Name" , "Sheet1" );
            writer.WriteStartElement( "Table" );
        }

        private void WriteXMLData( XmlWriter writer , HierarchyDefinition[] colDefs , HierarchyDefinition rowDef )
        {
            foreach ( var colDef in colDefs )
            {
                var opt = Resolve( colDef , rowDef );

                opt.Some( resultSet =>
                {
                    var qualification = resultSet.Qualifier;

                    writer.WriteStartElement( "Cell" );

                    switch ( qualification )
                    {
                        case Qualification.Computed:
                            writer.WriteAttributeString( "ss:StyleID" , "Computed" );
                            break;

                        case Qualification.Error:
                            writer.WriteAttributeString( "ss:StyleID" , "Error" );
                            break;

                        case Qualification.Warning:
                            writer.WriteAttributeString( "ss:StyleID" , "Warning" );
                            break;

                        case Qualification.Remark:
                            writer.WriteAttributeString( "ss:StyleID" , "Remark" );
                            break;

                        case Qualification.Custom:
                            resultSet.BackgroundColor
                                .Some( cl =>
                                {
                                    var (a, r, g, b) = cl;
                                    writer.WriteAttributeString( "ss:StyleID" , string.Concat( "Custom" , FormatExcelColor( a , r , g , b ) ) );
                                } );
                            break;
                    }

                    writer.WriteStartElement( "Data" );
                    var str = resultSet.Result;

                    if ( double.TryParse( str , out var val ) )
                    {
                        writer.WriteAttributeString( "ss:Type" , "Number" );
                        str = val.ToString( System.Globalization.NumberFormatInfo.InvariantInfo );
                    }
                    else
                    {
                        writer.WriteAttributeString( "ss:Type" , "String" );
                    }

                    writer.WriteString( str );
                    writer.WriteEndElement(); //data
                    writer.WriteEndElement(); //cell
                } )
                    .None( () => { /* Do nothing if there are no data */ } );
            }
        }

        private void WriteXMLColumnsHeaders( XmlWriter writer , int maxLevel , IEnumerable<HierarchyDefinition> consumers , bool withStructure )
        {
            var hierarchyDefinitions = consumers as HierarchyDefinition[] ?? consumers.ToArray();

            if ( withStructure )
            {
                var roots = hierarchyDefinitions.Roots().ToArray();
                var flatColumns = roots.FlatList( false ).ToArray();

                for ( int level = 0 ; level < roots.TotalDepth( false ) ; level++ )
                {
                    writer.WriteStartElement( "Row" );

                    // Skip as many columns are there are levels in row headers
                    for ( int skip = 0 ; skip < maxLevel ; skip++ )
                    {
                        writer.WriteStartElement( "Cell" );
                        writer.WriteStartElement( "Data" );
                        writer.WriteAttributeString( "ss:Type" , "String" );
                        writer.WriteString( string.Empty );
                        writer.WriteEndElement(); //data
                        writer.WriteEndElement(); //cell
                    }

                    foreach ( var colDef in flatColumns.Where( x => x.Level == level ) )
                    {
                        writer.WriteStartElement( "Cell" );

                        if ( colDef.Count() > 1 )
                            writer.WriteAttributeString( "ss:MergeAcross" , $"{( colDef.Count() - 1 )}" );

                        writer.WriteAttributeString( "ss:StyleID" , "headerstyle" );
                        writer.WriteStartElement( "Data" );
                        writer.WriteAttributeString( "ss:Type" , "String" );
                        writer.WriteString( $"{colDef.Content}" );
                        writer.WriteEndElement(); //data

                        writer.WriteEndElement(); //cell
                    }

                    writer.WriteEndElement(); //row
                }
            }
            else
            {
                writer.WriteStartElement( "Row" );

                // Skip as many columns are there are levels in row headers
                for ( int skip = 0 ; skip < maxLevel ; skip++ )
                {
                    writer.WriteStartElement( "Cell" );
                    writer.WriteStartElement( "Data" );
                    writer.WriteAttributeString( "ss:Type" , "String" );
                    writer.WriteString( string.Empty );
                    writer.WriteEndElement(); //data
                    writer.WriteEndElement(); //cell
                }

                foreach ( var colDef in hierarchyDefinitions )
                {
                    writer.WriteStartElement( "Cell" );

                    if ( colDef.Count() > 1 )
                        writer.WriteAttributeString( "ss:MergeAcross" , ( colDef.Count() - 1 ).ToString() );

                    writer.WriteAttributeString( "ss:StyleID" , "headerstyle" );
                    writer.WriteStartElement( "Data" );
                    writer.WriteAttributeString( "ss:Type" , "String" );
                    writer.WriteString( $"{colDef.Content}" );
                    writer.WriteEndElement(); //data
                    writer.WriteEndElement(); //cell
                }

                writer.WriteEndElement(); //row
            }
        }

        private void WriteXMLTitle( string title , XmlWriter writer )
        {
            if ( !string.IsNullOrWhiteSpace( title ) )
            {
                writer.WriteStartElement( "Row" );
                writer.WriteStartElement( "Cell" );
                writer.WriteStartElement( "Data" );
                writer.WriteAttributeString( "ss:Type" , "String" );
                writer.WriteString( title );
                writer.WriteEndElement(); //data
                writer.WriteEndElement(); //cell
                writer.WriteEndElement(); //row
            }
        }

        private void WriteXMLFooter( XmlWriter writer )
        {
            writer.WriteEndElement(); //table
            writer.WriteEndElement(); //worksheet
            writer.WriteEndElement(); //workbook
            writer.Flush();
        }

        private string FormatExcelColor( byte a , byte r , byte g , byte b )
           => string.Format( "#{0:x}{1:x}{2:x}{3:x}" , a , r , g , b );

        private string FormatExcelColor<T>( T color , Func<T , (byte a, byte r, byte g, byte b)> codeExtractor )
        {
            var (a, r, g, b) = codeExtractor( color );
            return FormatExcelColor( a , r , g , b );
        }

        private string FormatExcelColor( ThemeColor color )
        {
            var (a, r, g, b) = color.ToArgb();
            return FormatExcelColor( a , r , g , b );
        }
    }
}