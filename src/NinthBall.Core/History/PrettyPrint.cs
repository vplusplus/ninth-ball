using DocumentFormat.OpenXml.Office2019.Excel.RichData2;
using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;

namespace NinthBall.Core
{
    internal static partial class PrettyPrint
    {
        public static void Print(this HRegimes regimes, TextWriter writer)
        {
            var R = regimes.Regimes;
            var Q = regimes.Quality;
            var P = regimes.StandardizationParams;

            writer.WriteLine($" {R.Count} regimes | Silhouette: {Q.SilhouetteScore:F2} | Inertia: {Q.TotalInertia}");

            // NumRegimes | TotalInertia | SilhouetteScore | DBI | CH | Dunn


            var aboutRegimes = regimes.Regimes.Select((r, idx) => new
            {
                Label = r.RegimeLabel,
                Members = 1,
                Silhouette = Math.Round(regimes.Quality.ClusterSilhouette.Span[idx], 2),
                Inertia = Math.Round( regimes.Quality.ClusterInertia.Span[idx], 2 )
            })
            .ToList();

            writer.PrintTextTable(aboutRegimes, 8);

        }

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
    }
}
