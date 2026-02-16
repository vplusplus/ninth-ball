using System.Collections;
using System.Data;
using System.Reflection;

namespace NinthBall.Core.PrettyPrint
{
    internal static class PrettyPrintMarkdownExtensions
    {
        static readonly string DASHES = new string('-', 200);

        //......................................................................
        #region Markdown Titles & Sections
        //......................................................................

        public static TextWriter PrintMarkdownPageTitle(this TextWriter writer, string text) => PrintHeading(writer, "###", text);

        public static TextWriter PrintMarkdownSectionTitle(this TextWriter writer, string text) => PrintHeading(writer, "####", text);

        static TextWriter PrintHeading(this TextWriter writer, string hashes, string text)
        {
            writer.WriteLine();         // Why: Blank line before - Mandatory
            writer.Write(hashes);
            writer.Write(" ");          // Why: Always include a single space after hash
            writer.Write(text);
            writer.WriteLine();         // Why: End the title line.      
            writer.WriteLine();         // Why: Blank line after - optional, highly Recommended
            return writer;
        }

        #endregion

        //......................................................................
        #region Record Rendering - One Line
        //......................................................................

        public static TextWriter PrintMarkdownRecordOneLine(this TextWriter writer, object record)
            => PrintOneLine(writer, GetSimplePropertyNamesAndValues(record));

        public static TextWriter PrintMarkdownRecordOneLine(this TextWriter writer, IDictionary record)
            => PrintOneLine(writer, GetDictionaryKeysAndValues(record));

        static TextWriter PrintOneLine(this TextWriter writer, IEnumerable<(string Name, object? Value)> pairs)
        {
            bool first = true;
            foreach (var pair in pairs)
            {
                if (!first) writer.Write(" | ");
                writer.Write(pair.Name);
                writer.Write(" : ");
                writer.Write(GetFormattedValue(value: pair.Value, optionalCustomFormat: null));
                first = false;
            }
            writer.WriteLine();
            return writer;
        }

        #endregion

        //......................................................................
        #region Record Rendering - Wide & Tall
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
        #region Render Markdown Table
        //......................................................................

        public static TextWriter PrintMarkdownTable(this TextWriter writer, DataTable dt, int minColWidth = 12)
        {
            ArgumentNullException.ThrowIfNull(dt);
            ArgumentNullException.ThrowIfNull(writer);

            var C = dt.Columns;
            var numRows = dt.Rows.Count;
            var numCols = C.Count;

            // Column widths. Start with suggested min widths
            var W = new int[numCols];
            Array.Fill(W, minColWidth);

            // Consult column name to adjust width
            for (int j = 0; j < numCols; j++) W[j] = Math.Max(W[j], C[j].ColumnName.Length);

            // Collect formatted content of all cells.
            // Along side, adjust column width based on content.
            // When the loop ends, we will have formatted content and adjusted column widths.
            var buffer = new string[numRows * numCols];
            for (int r = 0; r < numRows; r++)
            {
                var row = dt.Rows[r];
                for (int c = 0; c < numCols; c++)
                {
                    var formatted = GetFormattedValue(value: row[c], optionalCustomFormat: C[c].TextFormat);
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

            // Alignment is defined and is True | Alignment not specified but its numeric value
            static bool IsRightAligned(DataColumn? col, object? value) => (null != col?.IsRightAligned && true == col?.IsRightAligned) || true == value?.GetType()?.IsNumeric();
        }

        public static TextWriter PrintMarkdownTable<T>(this TextWriter writer, IEnumerable<T> collection, int minColWidth = 12)
        {
            return writer.PrintMarkdownTable(collection.ToDataTable(), minColWidth);
        }

        public static TextWriter PrintMarkdownTable(this TextWriter writer, IEnumerable<IDictionary> collection, int minColWidth = 12)
        {
            return writer.PrintMarkdownTable(collection.ToDataTable(), minColWidth);
        }

        #endregion

        //......................................................................
        #region Helpers
        //......................................................................

        private static IEnumerable<(string Name, object? Value)> GetSimplePropertyNamesAndValues(object? record)
        {
            if (record == null) return [];

            return record.GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && IsSimpleType(p.PropertyType))
                .Select(p => (p.Name, p.GetValue(record)));

            static bool IsSimpleType(Type type)
            {
                type = Nullable.GetUnderlyingType(type) ?? type;
                return type.IsEnum || Type.GetTypeCode(type) != TypeCode.Object;
            }
        }

        private static IEnumerable<(string Name, object? Value)> GetDictionaryKeysAndValues(IDictionary record)
        {
            if (record == null) yield break;

            foreach (DictionaryEntry entry in record)
                yield return (entry.Key?.ToString() ?? "Unknown", entry.Value);
        }

        private static string GetFormattedValue(object? value, string? optionalCustomFormat)
        {
            if (value == null || value == DBNull.Value)
            {
                return string.Empty;
            }
            else if (value is IFormattable formattable)
            {
                string format = optionalCustomFormat ?? DefaultFormat(value);

                return
                    string.IsNullOrWhiteSpace(format) ? value.ToString() ?? string.Empty :
                    format.Contains('{') ? string.Format(format, value) :
                    formattable.ToString(format, null);
            }
            else
            {
                return value.ToString() ?? string.Empty;
            }

            static string DefaultFormat(object? value)
            {
                if (null == value) return string.Empty;

                var type = Nullable.GetUnderlyingType(value.GetType()) ?? value.GetType();

                return Type.GetTypeCode(type) switch
                {
                    TypeCode.Byte or TypeCode.SByte or TypeCode.Int16 or TypeCode.UInt16 or TypeCode.Int32 or TypeCode.UInt32 or TypeCode.Int64 or TypeCode.UInt64 => "{0:N0}",
                    TypeCode.Single or TypeCode.Double or TypeCode.Decimal => "N2",
                    _ => string.Empty
                };
            }
        }

        static bool IsNumeric(this Type? type)
        {
            if (null == type) return false;

            type = Nullable.GetUnderlyingType(type) ?? type;

            return Type.GetTypeCode(type) switch
            {
                TypeCode.Byte or TypeCode.SByte or 
                TypeCode.Int16 or TypeCode.UInt16 or TypeCode.Int32 or TypeCode.UInt32 or TypeCode.Int64 or TypeCode.UInt64 or
                TypeCode.Single or TypeCode.Double or TypeCode.Decimal => true,
                _ => false
            };
        }

        #endregion
    }
}
