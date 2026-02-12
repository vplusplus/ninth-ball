
using NinthBall.Core;

namespace UnitTests
{
    internal static class Print
    {
        static TextWriter LeftAligned(this TextWriter writer, string text, int width) 
        {
            writer.Write(text.PadRight(width));
            return writer;
        }

        public static void PrettyPrintTransitionMatrix(this TextWriter writer, HRegimes regimes)
        {
            var matrix = regimes.GetRegimeTransitionMatrix();
            var labels = regimes.Regimes.AsEnumerable().Select(x => x.RegimeLabel).ToArray();
            PrettyPrintTransitionMatrix(writer, matrix, labels);
        }

        public static void PrettyPrintTransitionMatrix(this TextWriter writer, TwoDMatrix matrix, string[] labels)
        {
            string dashes = new string('-', 80);

            string GetLabel(int rid) => rid < labels.Length ? labels[rid] : $"Regime {rid}";

            int labelWidth = 4;
            for (int i = 0; i < matrix.NumColumns; i++)
            {
                var label = GetLabel(i);    
                labelWidth = Math.Max(labelWidth, label.Length + 2);
            }

            // Header
            writer.WriteLine(dashes);
            writer.LeftAligned(string.Empty, labelWidth);
            writer.Write(" | ");
            for(int r=0; r<matrix.NumColumns; r++)
            {
                writer.LeftAligned(GetLabel(r) , labelWidth);
                writer.Write(" | ");
            }
            writer.WriteLine();
            writer.WriteLine(dashes);

            for (int row = 0; row < matrix.NumColumns; row++)
            {
                writer.LeftAligned(GetLabel(row), labelWidth);
                writer.Write(" | ");
                for (int col = 0; col < matrix.NumColumns; col++)
                {
                    writer.LeftAligned($"{matrix[row, col]:P1}", labelWidth);
                    writer.Write(" | ");
                }

                writer.WriteLine();
            }

            writer.WriteLine(dashes);
        }

    }
}
