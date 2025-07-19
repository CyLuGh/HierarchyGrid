using System;
using System.Text;
using System.Threading.Tasks;
using LanguageExt;

namespace HierarchyGrid.Definitions
{
    public enum CopyMode
    {
        Flat,
        Structure,
        Selection,
        Highlights,
    }

    public partial class HierarchyGridViewModel
    {
        /// <summary>
        /// Override the clipboard content builder for structured output, use tab separated format if not defined.
        /// Func parameters are rows then columns.
        /// </summary>
        public Option<
            Func<Seq<HierarchyDefinition>, Seq<HierarchyDefinition>, string>
        > CreateClipboardStructuredContentOverride { get; set; }

        /// <summary>
        /// Override the clipboard content builder for flat output, use tab separated format if not defined.
        /// Func parameters are rows then columns.
        /// </summary>
        public Option<
            Func<Seq<HierarchyDefinition>, Seq<HierarchyDefinition>, string>
        > CreateClipboardFlatContentOverride { get; set; }

        /// <summary>
        /// Override the clipboard output for cells output, uses ResultSet.Result if not defined.
        /// </summary>
        public Option<Func<ResultSet, string>> ClipboardFillerOverride { get; set; }

        /// <summary>
        /// Override the clipboard output for column headers, uses HierarchyDefinition.Content if not defined.
        /// </summary>
        public Option<Func<HierarchyDefinition, string>> ClipboardColumnHeaderOverride { get; set; }

        /// <summary>
        /// Override the clipboard output for row headers, uses HierarchyDefinition.Content if not defined.
        /// </summary>
        public Option<Func<HierarchyDefinition, string>> ClipboardRowHeaderOverride { get; set; }

        private string FillClipboardContent(Option<ResultSet> option) =>
            option.Match(
                rs => ClipboardFillerOverride.Match(f => f(rs), rs.Result),
                () => string.Empty
            );

        private string FillClipboardColumnHeaderContent(HierarchyDefinition hdef) =>
            ClipboardColumnHeaderOverride.Match(
                f => f(hdef),
                () => hdef.Content?.ToString() ?? string.Empty
            );

        private string FillClipboardRowHeaderContent(HierarchyDefinition hdef) =>
            ClipboardRowHeaderOverride.Match(
                f => f(hdef),
                () => hdef.Content?.ToString() ?? string.Empty
            );

        private Task<string> CreateClipboardContent(CopyMode mode)
        {
            var rows = GetRows(mode);
            var columns = GetColumns(mode);

            Func<Seq<HierarchyDefinition>, Seq<HierarchyDefinition>, string> builder =
                mode == CopyMode.Structure
                    ? CreateClipboardStructuredContentOverride.Match(
                        f => f,
                        () => CreateClipboardStructuredContent
                    )
                    : CreateClipboardFlatContentOverride.Match(
                        f => f,
                        () => CreateClipboardFlatContent
                    );

            return Task.Run(() => builder(rows, columns));
        }

        private Seq<HierarchyDefinition> GetRows(CopyMode mode)
        {
            switch (mode)
            {
                case CopyMode.Flat:
                    return RowsDefinitions.Leaves();

                case CopyMode.Structure:
                    return RowsDefinitions.FlatList(false);

                case CopyMode.Highlights:
                    var leaves = RowsDefinitions.Leaves();
                    return leaves.Any(l => l.IsHighlighted)
                        ? leaves.Where(l => l.IsHighlighted)
                        : leaves;

                case CopyMode.Selection:
                    var selected = Selections
                        .Select(s =>
                            !IsTransposed
                                ? s.ProducerDefinition as HierarchyDefinition
                                : s.ConsumerDefinition
                        )
                        .Distinct();

                    return selected.Length > 0 ? selected : Seq<HierarchyDefinition>.Empty;

                default:
                    return RowsDefinitions;
            }
        }

        private Seq<HierarchyDefinition> GetColumns(CopyMode mode)
        {
            switch (mode)
            {
                case CopyMode.Flat:
                    return ColumnsDefinitions.Leaves();

                case CopyMode.Structure:
                    return ColumnsDefinitions.FlatList(false);

                case CopyMode.Highlights:
                    var leaves = ColumnsDefinitions.Leaves();
                    return leaves.Any(l => l.IsHighlighted)
                        ? leaves.Where(l => l.IsHighlighted)
                        : leaves;

                case CopyMode.Selection:
                    var selected = Selections
                        .Select(s =>
                            !IsTransposed
                                ? s.ConsumerDefinition as HierarchyDefinition
                                : s.ProducerDefinition
                        )
                        .Distinct();

                    return selected.Length > 0 ? selected : Seq<HierarchyDefinition>.Empty;

                default:
                    return ColumnsDefinitions;
            }
        }

        private string CreateClipboardFlatContent(
            Seq<HierarchyDefinition> rows,
            Seq<HierarchyDefinition> columns
        )
        {
            var sb = new StringBuilder();

            const char separator = '\t';

            // Skip first cell
            sb.Append(separator);

            // Columns titles
            foreach (var column in columns)
                sb.Append(FillClipboardColumnHeaderContent(column)).Append(separator);

            sb.Length--;
            sb.AppendLine();

            foreach (var row in rows)
            {
                sb.Append(FillClipboardRowHeaderContent(row)).Append(separator);

                foreach (var column in columns)
                {
                    sb.Append(FillClipboardContent(Resolve(row, column)));
                    sb.Append(separator);
                }

                sb.Length--;
                sb.AppendLine();
            }

            return sb.ToString();
        }

        private string CreateClipboardStructuredContent(
            Seq<HierarchyDefinition> rows,
            Seq<HierarchyDefinition> columns
        )
        {
            var sb = new StringBuilder();

            const char separator = '\t';

            var rowDepth = rows.TotalDepth(false);
            var colDepth = columns.TotalDepth(false);

            for (int i = 0; i < colDepth; i++)
            {
                var currentLevel = i;

                // Skip cells corresponding to rows depth
                for (int _ = 0; _ < rowDepth; _++)
                    sb.Append(separator);

                var currentLevelColumns = columns.Where(c => c.Level == currentLevel);
                int currentPosition = 0;
                foreach (var column in currentLevelColumns)
                {
                    var columnPosition = columns.GetRelativePosition(column);

                    while (currentPosition < columnPosition)
                    {
                        sb.Append(separator);
                        currentPosition++;
                    }

                    for (int _ = 0; _ < column.Span; _++)
                    {
                        sb.Append(FillClipboardColumnHeaderContent(column)).Append(separator);
                        currentPosition++;
                    }
                }

                sb.Length--;
                sb.AppendLine();
            }

            var columnLeaves = columns.Roots().Leaves().ToArr();

            foreach (var leafRow in rows.Roots().Leaves())
            {
                var path = leafRow.Path;
                int position = 0;

                foreach (var row in path)
                {
                    sb.Append(FillClipboardRowHeaderContent(row)).Append(separator);
                    position++;
                }

                for (int _ = position; _ < rowDepth; _++)
                    sb.Append(separator);

                foreach (var column in columnLeaves)
                {
                    sb.Append(FillClipboardContent(Resolve(leafRow, column)));
                    sb.Append(separator);
                }

                sb.Length--;
                sb.AppendLine();
            }

            return sb.ToString();
        }

        private static Option<ResultSet> Resolve(
            HierarchyDefinition rowDef,
            HierarchyDefinition colDef
        )
        {
            return rowDef switch
            {
                ProducerDefinition p when colDef is ConsumerDefinition c => Option<ResultSet>.Some(
                    HierarchyDefinition.Resolve(p, c)
                ),
                ConsumerDefinition cr when colDef is ProducerDefinition pr =>
                    Option<ResultSet>.Some(HierarchyDefinition.Resolve(pr, cr)),
                _ => Option<ResultSet>.None,
            };
        }
    }
}
