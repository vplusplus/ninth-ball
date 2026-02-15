using NinthBall.Core;
using System.Data;

namespace UnitTests
{
    internal static class DataTableExtensions
    {

        public static DataTable WithColumn(this DataTable dt, string colName, Type? type = null)
        {
            dt.Columns.Add(colName, type ?? typeof(string));
            return dt;
        }

        public static DataTable WithFormat(this DataTable dt, string colName, string format)
        {
            dt.Columns[colName]!.ExtendedProperties["Format"] = format;
            return dt;
        }

        public static DataTable WithAlignment(this DataTable dt, string colName, string alignment)
        {
            dt.Columns[colName]!.ExtendedProperties["Align"] = alignment;
            return dt;
        }

        public static DataTable RegimeTransitionsAsDataTable(this HRegimes regimes)
        {
            var dt = new DataTable();

            // Regime | R1 | R2 | R3
            dt.WithColumn("Regime");
            foreach (var r in regimes.Regimes)
            {
                dt.WithColumn(r.RegimeLabel, typeof(double))
                  .WithFormat(r.RegimeLabel, "P0");
            }

            foreach (var r in regimes.Regimes)
            {
                var tx = r.NextRegimeProbabilities.Span;

                var row = dt.NewRow();
                row[0] = r.RegimeLabel;
                for (int i = 0; i < tx.Length; i++) row[i + 1] = tx[i];

                dt.Rows.Add(row);
            }

            return dt;
        }


        static readonly string DASHES = new string('-', 200);

        public static void PrettyPrint(this DataTable dt, TextWriter writer, int minColWidth = 12)
        {
            var C = dt.Columns;
            var W = DiscoverColumnWidths();

            // THEAD
            PrintHorizontalLine();
            writer.Write(' ');
            for (int i = 0; i < C.Count; i++) PrintCell(C[i], C[i].ColumnName, W[i], i > 0 ? " | " : null);
            writer.WriteLine();
            PrintHorizontalLine();

            // TBODY
            foreach (DataRow R in dt.Rows) 
            {
                writer.Write(' ');
                for (int i=0; i<C.Count; i++) PrintCell(C[i], R[i], W[i], i > 0 ? " | " : null);
                writer.WriteLine();
            }
            PrintHorizontalLine();
            return;

            //................................................

            void PrintHorizontalLine()
            {
                writer.Write('-');
                for (int i = 0; i < C.Count; i++)
                {
                    if (i > 0) writer.Write("-+-");
                    writer.Write( DASHES.AsSpan(0, W[i]));
                }
                writer.Write('-');
                writer.WriteLine();
            }

            void PrintCell(DataColumn col, object? value, int colWidth, string? prefix = null)
            {
                var txt = GetFormattedValue(col, value);
                var isRight = IsRightAligned(col, value);
                var aligned = isRight ? txt.PadLeft(colWidth) : txt.PadRight(colWidth);

                if (null != prefix) writer.Write(prefix);
                writer.Write(aligned);
            }

            int[] DiscoverColumnWidths()
            {
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
        }

        public static void PrintMarkdownTable(this DataTable dt, TextWriter writer, int minColWidth = 12)
        {
            var C = dt.Columns;
            var W = DiscoverColumnWidths();

            // THEAD
            writer.Write("| ");
            for (int i = 0; i < C.Count; i++)
            {
                if (i > 0) writer.Write(" | ");
                PrintCell(C[i], C[i].ColumnName, W[i], false);
            }
            writer.WriteLine(" |");

            // SEPARATOR (with alignment hints)
            writer.Write("|");
            for (int i = 0; i < C.Count; i++)
            {
                var isRight = IsRightAligned(C[i], null);
                var totalWidth = W[i] + 2; // fills the content width + 2 spaces of padding

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
                    PrintCell(C[i], R[i], W[i], IsRightAligned(C[i], R[i]));
                }
                writer.WriteLine(" |");
            }

            return;

            //................................................

            void PrintCell(DataColumn col, object? value, int colWidth, bool isRight)
            {
                var txt = GetFormattedValue(col, value);
                var aligned = isRight ? txt.PadLeft(colWidth) : txt.PadRight(colWidth);
                writer.Write(aligned);
            }

            int[] DiscoverColumnWidths()
            {
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
        }

        static string GetFormattedValue(DataColumn col, object? value)
        {
            if (value == null || value == DBNull.Value) return string.Empty;
            if (col.ExtendedProperties["Format"] is string format && value is IFormattable formattable)
            {
                return formattable.ToString(format, null);
            }
            return value.ToString() ?? string.Empty;
        }

        static bool IsRightAligned(DataColumn col, object? value)
        {
            if (col.ExtendedProperties["Align"] is string align)
                return align.Equals("Right", StringComparison.OrdinalIgnoreCase);

            var type = col.DataType;
            return type == typeof(int) || type == typeof(long) || type == typeof(float) || type == typeof(double) || type == typeof(decimal);
        }

    }
}
