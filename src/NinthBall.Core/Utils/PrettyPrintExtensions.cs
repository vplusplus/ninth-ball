
namespace NinthBall.Core
{
    internal static partial class PrettyPrintExtensions
    {
        public static void PrintTextTable<T>(this TextWriter writer, IReadOnlyList<T> data, string[] colNames = null!, int minColWidth = 4)
        {
            const string separator = " | ";

            var props = typeof(T).GetProperties();

            // If column names not specified, user property names as column names.
            colNames = colNames ?? props.Select(x => x.Name).ToArray();
            if (colNames.Length != props.Length) throw new Exception("NumProperties != NumColumnNames");

            // Digest the cell values
            var rows = data.Select(r => props.Select(p => p.GetValue(r)?.ToString() ?? "").ToArray()).ToList();

            // Start with suggested min width for each column
            var colWidths = new int[colNames.Length];
            Array.Fill(colWidths, minColWidth);

            // For each column, use max width of the data or the minColWidth.
            colWidths = colNames.Select((name, i) => Math.Max(colWidths[i], Math.Max(name.Length, rows.Count == 0 ? 0 : rows.Max(r => r[i].Length)))).ToArray();


            // Print header
            HR();
            for (int i = 0; i < colNames.Length; i++)
            {
                writer.Write(colNames[i].PadRight(colWidths[i]));
                if (i < colNames.Length - 1) writer.Write(separator);
            }
            writer.WriteLine();
            HR();

            // Print rows
            foreach (var row in rows)
            {
                for (int i = 0; i < row.Length; i++)
                {
                    var text = row[i];
                    var aligned = IsNumeric(props[i].PropertyType) ? text.PadLeft(colWidths[i]) : text.PadRight(colWidths[i]);
                    writer.Write(aligned);
                    if (i < row.Length - 1) writer.Write(separator);
                }
                writer.WriteLine();
            }
            HR();

            // Print line
            void HR()
            {
                for (int i = 0; i < colWidths.Length; i++)
                {
                    writer.Write(new string('-', colWidths[i]));
                    if (i < colWidths.Length - 1) writer.Write("-+-");
                }
                writer.Write("-");
                writer.WriteLine();
            }

            bool IsNumeric(Type t) =>
                t == typeof(int) ||
                t == typeof(long) ||
                t == typeof(float) ||
                t == typeof(double) ||
                t == typeof(decimal);
        }
    }
}
