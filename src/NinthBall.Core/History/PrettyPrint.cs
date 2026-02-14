

namespace NinthBall.Core
{
    internal static partial class PrettyPrintExtensions
    {
        public static void Print(this HRegimes regimes, TextWriter writer)
        {
            var R = regimes.Regimes;
            var P = regimes.StandardizationParams;

            // writer.WriteLine($" {R.Count} regimes | Silhouette: {Q.SilhouetteScore:F2} | Inertia: {Q.TotalInertia}");

            // NumRegimes | TotalInertia | SilhouetteScore | DBI | CH | Dunn


            var aboutRegimes = regimes.Regimes.Select((r, idx) => new
            {
                Label = r.RegimeLabel,
                Members = 1,
                //Silhouette = Math.Round(regimes.Quality.ClusterSilhouette.Span[idx], 2),
                //Inertia = Math.Round( regimes.Quality.ClusterInertia.Span[idx], 2 )
            })
            .ToList();

            writer.PrintTextTable(aboutRegimes, minColWidth: 8);

        }

        public static void PrettyPrint(this TextWriter writer, KMean.Result kResult)
        {
            var Q = kResult.Quality;

            //var summary = new
            //{
            //    Clusters = kResult.NumClusters,
            //    Features = kResult.NumFeatures,
            //    Silhouette = Math.Round( Q.Silhouette, 2),
            //    Inertia = Math.Round(Q.Inertia, 0),
            //    DBI = Math.Round( Q.DBI, 2),
            //    CH = Math.Round( Q.CH, 2),
            //    Dunn = Math.Round(Q.Dunn, 2),
            //};

            //writer.WriteLine();
            //writer.PrintTextSingleRowTable(summary, minColWidth: 10);

            var byCluster = Enumerable.Range(0, kResult.NumClusters).Select(i => new
            {
                Cluster = $"#{i}",
                Members = Q.ClusterMembersCount.Span[i],
                Silhouette = Math.Round( Q.ClusterSilhouette.Span[i], 2),
                Inertia = Math.Round( Q.ClusterInertia.Span[i] ),
            })
            .ToList();

            byCluster.Add(new
            {
                Cluster     = "Total",
                Members     = kResult.Assignments.Length,
                Silhouette  = Math.Round(Q.Silhouette, 2),
                Inertia     = Math.Round(Q.Inertia, 0),
            });

            writer.WriteLine();
            writer.WriteLine($"Clusters: {kResult.NumClusters} | Features: {kResult.NumFeatures} | DBI: {Q.DBI:F2} | CH: {Q.CH:F2} | Dunn: {Q.Dunn:F2}");
            writer.PrintTextTable(byCluster, minColWidth: 10);
            
        }

        //static void PrintTextSingleRowTable<T>(this TextWriter writer, T row, string[] colNames = null!, int minColWidth = 4)
        //{

        //    writer.PrintTextTable([row], colNames, minColWidth);
        //}

        static void PrintTextTable<T>(this TextWriter writer, IReadOnlyList<T> data, string[] colNames = null!, int minColWidth = 4)
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

/*              
 
             Col1    Col2    Col3
 Label1      Cell1   Cell2   Cell3
 Label2      Cell1   Cell2   Cell3
 Label3      Cell1   Cell2   Cell3

 
        static void PrintTextTable<T>(this TextWriter writer, IReadOnlyList<T> data, int minColWidth)
        {
            const string separator = " | ";

            var props = typeof(T).GetProperties();
            var names = props.Select(x => x.Name).ToArray();
            var rows  = data.Select(r => props.Select(p => p.GetValue(r)?.ToString() ?? "").ToArray()).ToList();

            var widths = names
                .Select((name, i) => Math.Max(minColWidth, Math.Max( name.Length, rows.Count == 0 ? 0 : rows.Max(r => r[i].Length))))
                .ToArray();

            // Print header
            HR();
            for (int i = 0; i < names.Length; i++)
            {
                writer.Write(names[i].PadRight(widths[i]));
                if (i < names.Length - 1) writer.Write(separator);
            }
            writer.WriteLine();
            HR();

            // Print rows
            foreach (var row in rows)
            {
                for (int i = 0; i < row.Length; i++)
                {
                    var text = row[i];
                    var aligned = IsNumeric(props[i].PropertyType) ? text.PadLeft(widths[i]) : text.PadRight(widths[i]);
                    writer.Write(aligned);
                    if (i < row.Length - 1) writer.Write(separator);
                }
                writer.WriteLine();
            }
            HR();

            // Print line
            void HR()
            {
                for (int i = 0; i < widths.Length; i++)
                {
                    writer.Write(new string('-', widths[i]));

                    if (i < widths.Length - 1)
                        writer.Write("-+-");
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

 */