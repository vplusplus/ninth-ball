using System.Collections;
using System.Data;
using System.Reflection;

namespace UnitTests.PrettyTables
{
    internal static class PrettyPrintExtensions
    {
        static readonly string DASHES = new string('-', 200);

        //......................................................................
        #region Markdown Headings 
        //......................................................................

        public static TextWriter PrintMarkdownPageTitle(this TextWriter writer, string text) => PrintHeading(writer, "#", text);

        public static TextWriter PrintMarkdownSectionTitle(this TextWriter writer, string text) => PrintHeading(writer, "##", text);

        static TextWriter PrintHeading(this TextWriter writer, string hashes, string text)
        {
            writer.WriteLine();
            writer.Write(hashes);
            writer.Write(" ");
            writer.Write(text);
            writer.WriteLine();
            writer.WriteLine();
            return writer;
        }

        #endregion

        //......................................................................
        #region Markdown record - One line
        //......................................................................

        public static TextWriter PrintMarkdownRecordOneLine(this TextWriter writer, object record)
            => PrintOneLine(writer, GetSimplePropertyPairs(record));

        public static TextWriter PrintMarkdownRecordOneLine(this TextWriter writer, IDictionary record)
            => PrintOneLine(writer, GetDictionaryPairs(record));

        static TextWriter PrintOneLine(this TextWriter writer, IEnumerable<(string Name, object? Value)> pairs)
        {
            bool first = true;
            foreach (var pair in pairs)
            {
                if (!first) writer.Write(" | ");
                writer.Write(pair.Name);
                writer.Write(" : ");
                writer.Write(FormatDiagnosticValue(pair.Value));
                first = false;
            }
            return writer;
        }

        #endregion

        //......................................................................
        #region Markdown record - Wide and Tall formats
        //......................................................................

        public static TextWriter PrintMarkdownRecordWide(this TextWriter writer, object record)
        {
            record.ToWideTable().PrintMarkdownTable(writer);
            return writer;
        }

        public static TextWriter PrintMarkdownRecordWide(this TextWriter writer, IDictionary record)
        {
            record.ToWideTable().PrintMarkdownTable(writer);
            return writer;
        }

        public static TextWriter PrintMarkdownRecordTall(this TextWriter writer, object record)
        {
            record.ToTallTable().PrintMarkdownTable(writer);
            return writer;
        }

        public static TextWriter PrintMarkdownRecordTall(this TextWriter writer, IDictionary record)
        {
            record.ToTallTable().PrintMarkdownTable(writer);
            return writer;
        }

        #endregion

        //......................................................................
        #region Markdown Table Grid
        //......................................................................

        // THE CORE ENGINE: Prints any DataTable as a Markdown table.
        // No title/section here; use Fluent Title API on the writer instead.
        public static TextWriter PrintMarkdownTable(this DataTable dt, TextWriter writer, int minColWidth = 12)
        {
            ArgumentNullException.ThrowIfNull(dt);
            ArgumentNullException.ThrowIfNull(writer);

            var C = dt.Columns;
            var W = DiscoverColumnWidths(dt, minColWidth);

            // THEAD
            writer.Write("| ");
            for (int i = 0; i < C.Count; i++)
            {
                if (i > 0) writer.Write(" | ");
                PrintCellInternalSimplified(writer, C[i], C[i].ColumnName, W[i], false);
            }
            writer.WriteLine(" |");

            // SEPARATOR
            writer.Write("|");
            for (int i = 0; i < C.Count; i++)
            {
                var isRight = IsRightAligned(C[i], null);
                var totalWidth = W[i] + 2; 

                if (isRight)
                {
                    writer.Write(DASHES.AsSpan(0, totalWidth - 1));
                    writer.Write(":");
                }
                else
                {
                    writer.Write(":");
                    writer.Write(DASHES.AsSpan(0, totalWidth - 1));
                }
                writer.Write("|");
            }
            writer.WriteLine();

            // TBODY
            foreach (DataRow R in dt.Rows)
            {
                writer.Write("| ");
                for (int i = 0; i < C.Count; i++)
                {
                    if (i > 0) writer.Write(" | ");
                    PrintCellInternalSimplified(writer, C[i], R[i], W[i], IsRightAligned(C[i], R[i]));
                }
                writer.WriteLine(" |");
            }
            return writer;
        }

        #endregion

        //......................................................................
        #region Helpers
        //......................................................................

        private static IEnumerable<(string Name, object? Value)> GetSimplePropertyPairs(object? record)
        {
            if (record == null) return [];
            return record.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && (p.PropertyType.IsPrimitive || p.PropertyType == typeof(string) || p.PropertyType == typeof(decimal) || p.PropertyType == typeof(DateTime) || p.PropertyType == typeof(TimeSpan)))
                .Select(p => (p.Name, p.GetValue(record)));
        }

        private static IEnumerable<(string Name, object? Value)> GetDictionaryPairs(IDictionary record)
        {
            if (record == null) yield break;
            foreach (DictionaryEntry entry in record)
                yield return (entry.Key?.ToString() ?? "Unknown", entry.Value);
        }

        private static string FormatDiagnosticValue(object? value)
        {
            if (value == null || value == DBNull.Value) return "";
            if (value is double d) return d.ToString("N4");
            if (value is float f) return f.ToString("N3");
            if (value is decimal m) return m.ToString("N2");
            return value.ToString() ?? "";
        }

        private static void PrintCellInternalSimplified(TextWriter writer, DataColumn col, object? value, int colWidth, bool isRight)
        {
            var txt = GetFormattedValue(col, value);
            var aligned = isRight ? txt.PadLeft(colWidth) : txt.PadRight(colWidth);
            writer.Write(aligned);
        }

        private static int[] DiscoverColumnWidths(DataTable dt, int minColWidth)
        {
            var C = dt.Columns;
            int[] colWidths = new int[C.Count];
            Array.Fill(colWidths, minColWidth);

            for (int i = 0; i < C.Count; i++)
                colWidths[i] = Math.Max(colWidths[i], C[i].ColumnName.Length);

            foreach (DataRow row in dt.Rows)
            {
                for (int i = 0; i < C.Count; i++)
                {
                    var cellValue = GetFormattedValue(C[i], row[i]);
                    colWidths[i] = Math.Max(colWidths[i], cellValue.Length);
                }
            }
            return colWidths;
        }

        private static string GetFormattedValue(DataColumn col, object? value)
        {
            if (value == null || value == DBNull.Value) return string.Empty;
            if (col.ExtendedProperties["Format"] is string format && value is IFormattable formattable)
            {
                return formattable.ToString(format, null);
            }
            // Fallback to diagnostic formatting if no specific format hint is present
            return FormatDiagnosticValue(value);
        }

        private static bool IsRightAligned(DataColumn col, object? value)
        {
            if (col.ExtendedProperties["Align"] is string align)
                return align.Equals("Right", StringComparison.OrdinalIgnoreCase);

            var type = (value != null && value != DBNull.Value) ? value.GetType() : col.DataType;
            return type == typeof(int) || type == typeof(long) || type == typeof(float) || type == typeof(double) || type == typeof(decimal);
        }

        #endregion
    }
}
