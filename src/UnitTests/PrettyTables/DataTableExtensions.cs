using NinthBall.Core;
using System.Data;

namespace UnitTests
{
    internal static class DataTableExtensions
    {

        public static DataTable WithColumn(this DataTable dt, string colName)
        {
            dt.Columns.Add(colName);
            return dt;
        }


        public static DataTable RegimeTransitionsAsDataTable(this HRegimes regimes)
        {
            var numRegimes = regimes.Regimes.Count;

            var dt = new DataTable();

            // Regime | R1 | R2 | R3
            dt.Columns.Add("Regime");
            foreach(var r in regimes.Regimes) dt.Columns.Add(r.RegimeLabel);

            foreach(var r in regimes.Regimes)
            {
                var tx = r.NextRegimeProbabilities.Span;

                var row = dt.NewRow();
                row[0] = r.RegimeLabel;
                for (int i = 0; i < tx.Length; i++) row[i + 1] = $"{tx[i],4:P0}";

                dt.Rows.Add(row);
            }

            return dt;
        }


        public static void PrettyPrint(this DataTable dt, TextWriter writer, int minColWidth = 12)
        {
            var dashes = new string('-', 80);

            var colWidths = DiscoverColumnWidths();

            // Header
            PrintHorizontalLine();
            writer.Write(' ');
            for (int i = 0; i < dt.Columns.Count; i++)
            {
                if (i > 0) writer.Write(" | ");
                PrintCell(dt.Columns[i].ColumnName, colWidths[i]);
            }
            writer.WriteLine();
            PrintHorizontalLine();

            foreach (DataRow row in dt.Rows) 
            {
                writer.Write(' ');
                for (int i=0; i<dt.Columns.Count; i++)
                {
                    if (i > 0) writer.Write(" | ");

                    PrintCell(row[i], colWidths[i]);

                    //var txt = $"{row[i]}".PadLeft(colWidths[i]);
                    //writer.Write(txt);
                }
                writer.WriteLine();
            }

            PrintHorizontalLine();


            void PrintCell(object value, int colWidth)
            {
                var t = value?.GetType() ?? typeof(string);

                var isNumeric = 
                    t == typeof(int) ||
                    t == typeof(long) ||
                    t == typeof(float) ||
                    t == typeof(double) ||
                    t == typeof(decimal);

                var txt = value.ToString() ?? string.Empty;
                var aligned = isNumeric ? txt.PadLeft(colWidth) : txt.PadRight(colWidth);
                writer.Write(aligned);
            }

            void PrintHorizontalLine()
            {
                writer.Write('-');
                for (int i = 0; i < dt.Columns.Count; i++)
                {
                    if (i > 0) writer.Write("-+-");

                    var txt = new string('-', colWidths[i]);
                    writer.Write(txt);
                }
                writer.Write('-');
                writer.WriteLine();
            }


            int[] DiscoverColumnWidths()
            {
                int[] colWidths = new int[dt.Columns.Count];

                Array.Fill(colWidths, minColWidth);

                for (int i = 0; i < dt.Columns.Count; i++)
                    colWidths[i] = Math.Max(colWidths[i], dt.Columns[i].ColumnName.Length);

                foreach (DataRow row in dt.Rows)
                    for (int i = 0; i < dt.Columns.Count; i++)
                        colWidths[i] = Math.Max(colWidths[i], $"{row[i]}".Length);

                return colWidths;
            }
        }

    }
}
