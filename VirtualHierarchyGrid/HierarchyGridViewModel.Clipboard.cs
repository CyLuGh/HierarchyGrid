using HierarchyGrid.Definitions;
using LanguageExt;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Xml;

namespace VirtualHierarchyGrid
{
    partial class HierarchyGridViewModel
    {
        private DataObject CopyToClipboard( string title , bool withStructure , IEnumerable<HierarchyDefinition> rows , IEnumerable<HierarchyDefinition> columns )
        {
            var mem = new MemoryStream();
            var writer = new XmlTextWriter( mem , null );
            writer.Formatting = Formatting.Indented;

            // Header
            WriteXMLHeader( writer );

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

            return new DataObject( "XML Spreadsheet" , mem );
        }

        private Option<(int, int)> Identify( HierarchyDefinition rowDef , HierarchyDefinition colDef )
        {
            if ( rowDef is ProducerDefinition p && colDef is ConsumerDefinition c )
                return Option<(int, int)>.Some( (p.Position, c.Position) );
            else if ( rowDef is ConsumerDefinition cr && colDef is ProducerDefinition pr )
                return Option<(int, int)>.Some( (pr.Position, cr.Position) );

            return Option<(int, int)>.None;
        }

        private void WriteXMLData( XmlTextWriter writer , HierarchyDefinition[] colDefs , HierarchyDefinition rowDef )
        {
            foreach ( var colDef in colDefs )
            {
                var idd = Identify( colDef , rowDef );

                idd.Some( key =>
                {
                    if ( ResultSets.TryGetValue( key , out var resultSet ) )
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
                                resultSet.CustomColor
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
                    }
                } )
                    .None( () => { /* Do nothing if there are no data */ } );
            }
        }

        private void WriteXMLColumnsHeaders( XmlTextWriter writer , int maxLevel , IEnumerable<HierarchyDefinition> consumers , bool withStructure )
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

        private void WriteXMLTitle( string title , XmlTextWriter writer )
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

        private void WriteXMLHeader( XmlTextWriter writer )
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

            // Custom
            foreach ( var col in ResultSets.Values.Select( o => o.CustomColor )
                                                 .Where( c => c.IsSome )
                                                 .Select( c => c )
                                                 .Distinct() )
            {
                var (a, r, g, b) = col.Some( c => c )
                                      .None( () => (255, 255, 255, 255) );

                scol = FormatExcelColor( a , r , g , b );
                writer.WriteStartElement( "Style" );
                writer.WriteAttributeString( "ss:ID" , string.Concat( "Custom" , scol ) );
                writer.WriteStartElement( "Interior" );
                writer.WriteAttributeString( "ss:Color" , scol );
                writer.WriteAttributeString( "ss:Pattern" , "Solid" );
                writer.WriteEndElement();//Interior
                writer.WriteEndElement();//Style
            }

            // Computed
            writer.WriteStartElement( "Style" );
            writer.WriteAttributeString( "ss:ID" , "Computed" );
            writer.WriteStartElement( "Interior" );
            scol = FormatExcelColor( ( (SolidColorBrush) HierarchyGridCell.CellReadOnlyBackground ).Color );
            writer.WriteAttributeString( "ss:Color" , scol );
            writer.WriteAttributeString( "ss:Pattern" , "Solid" );
            writer.WriteEndElement();//Interior
            writer.WriteEndElement();//Style

            // Error
            writer.WriteStartElement( "Style" );
            writer.WriteAttributeString( "ss:ID" , "Error" );
            writer.WriteStartElement( "Interior" );
            scol = FormatExcelColor( ( (SolidColorBrush) HierarchyGridCell.CellErrorBackground ).Color );
            writer.WriteAttributeString( "ss:Color" , scol );
            writer.WriteAttributeString( "ss:Pattern" , "Solid" );
            writer.WriteEndElement();//Interior
            writer.WriteEndElement();//Style

            // Warning
            writer.WriteStartElement( "Style" );
            writer.WriteAttributeString( "ss:ID" , "Warning" );
            writer.WriteStartElement( "Interior" );
            scol = FormatExcelColor( ( (SolidColorBrush) HierarchyGridCell.CellWarningBackground ).Color );
            writer.WriteAttributeString( "ss:Color" , scol );
            writer.WriteAttributeString( "ss:Pattern" , "Solid" );
            writer.WriteEndElement();//Interior
            writer.WriteEndElement();//Style

            // Remark
            writer.WriteStartElement( "Style" );
            writer.WriteAttributeString( "ss:ID" , "Remark" );
            writer.WriteStartElement( "Interior" );
            scol = FormatExcelColor( ( (SolidColorBrush) HierarchyGridCell.CellRemarkBackground ).Color );
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

        private void WriteXMLFooter( XmlTextWriter writer )
        {
            writer.WriteEndElement(); //table
            writer.WriteEndElement(); //worksheet
            writer.WriteEndElement(); //workbook
            writer.Flush();
        }

        private string FormatExcelColor( byte a , byte r , byte g , byte b )
            => string.Format( "#{0:x}{1:x}{2:x}{3:x}" , a , r , g , b );

        private string FormatExcelColor( Color col )
        {
            string R = col.R.ToString( "x" ).Length == 1 ? "0" + col.R.ToString( "x" ) : col.R.ToString( "x" );
            string G = col.G.ToString( "x" ).Length == 1 ? "0" + col.G.ToString( "x" ) : col.G.ToString( "x" );
            string B = col.B.ToString( "x" ).Length == 1 ? "0" + col.B.ToString( "x" ) : col.B.ToString( "x" );

            string scol = "#" + R + G + B;

            return scol;
        }
    }
}