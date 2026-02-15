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
            writer.PrintMarkdownTable(record.ToWideTable());
            return writer;
        }

        public static TextWriter PrintMarkdownRecordWide(this TextWriter writer, IDictionary record)
        {
            writer.PrintMarkdownTable(record.ToWideTable());
            return writer;
        }

        public static TextWriter PrintMarkdownRecordTall(this TextWriter writer, object record)
        {
            writer.PrintMarkdownTable(record.ToTallTable());
            return writer;
        }

        public static TextWriter PrintMarkdownRecordTall(this TextWriter writer, IDictionary record)
        {
            writer.PrintMarkdownTable(record.ToTallTable());
            return writer;
        }

        #endregion

        //......................................................................
        #region Markdown Table Grid
        //......................................................................

        // THE CORE ENGINE: Prints any DataTable as a Markdown table.
        // No title/section here; use Fluent Title API on the writer instead.
        public static TextWriter PrintMarkdownTable(this TextWriter writer, DataTable dt, int minColWidth = 12)
        {
            ArgumentNullException.ThrowIfNull(dt);
            ArgumentNullException.ThrowIfNull(writer);

            var C = dt.Columns;
            var numRows = dt.Rows.Count;
            var numCols = C.Count;

            // PASS 1: Measure and Buffer (Single Pass Formatting)
            var W = new int[numCols];
            Array.Fill(W, minColWidth);
            for (int j = 0; j < numCols; j++) W[j] = Math.Max(W[j], C[j].ColumnName.Length);

            var buffer = new string[numRows * numCols];
            for (int r = 0; r < numRows; r++)
            {
                var row = dt.Rows[r];
                for (int c = 0; c < numCols; c++)
                {
                    var formatted = GetFormattedValue(C[c], row[c]);
                    buffer[r * numCols + c] = formatted;
                    W[c] = Math.Max(W[c], formatted.Length);
                }
            }

            // THEAD
            writer.Write("| ");
            for (int i = 0; i < numCols; i++)
            {
                if (i > 0) writer.Write(" | ");
                writer.Write(C[i].ColumnName.PadRight(W[i]));
            }
            writer.WriteLine(" |");

            // SEPARATOR
            writer.Write("|");
            for (int i = 0; i < numCols; i++)
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
            for (int r = 0; r < numRows; r++)
            {
                writer.Write("| ");
                for (int c = 0; c < numCols; c++)
                {
                    if (c > 0) writer.Write(" | ");
                    
                    var txt = buffer[r * numCols + c];
                    var isRight = IsRightAligned(C[c], dt.Rows[r][c]);
                    var aligned = isRight ? txt.PadLeft(W[c]) : txt.PadRight(W[c]);
                    
                    writer.Write(aligned);
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
