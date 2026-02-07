
namespace NinthBall.Core
{
    internal static class VectorExtensions
    {
        // [target] += [source]
        public static void Sum(this Span<double> target, ReadOnlySpan<double> source)
        {
            if (target.Length != source.Length) throw new InvalidOperationException($"Vector lengths are not same | [{target.Length}] and [{source.Length}]");

            for (int i = 0; i < target.Length; i++) target[i] += source[i];
        }

        // [target] /= denominator
        public static void Divide(this Span<double> target, double denominator)
        {
            for (int i = 0; i < target.Length; i++) target[i] /= denominator;
        }

        // Copy operations
        public static void CopyFrom(this Span<double> target, ReadOnlySpan<double> source)
        {
            if (target.Length != source.Length) throw new InvalidOperationException($"Vector lengths are not same | [{target.Length}] and [{source.Length}]");
            source.CopyTo(target);
        }

        // [targetSumSq] += ([sourceRow] - [meanVector])^2
        public static void SumSquaredDiff(this Span<double> targetSumSq, ReadOnlySpan<double> sourceRow, ReadOnlySpan<double> meanVector)
        {
            if (targetSumSq.Length != sourceRow.Length || targetSumSq.Length != meanVector.Length) throw new InvalidOperationException("Vector lengths mismatch");
            for (int i = 0; i < targetSumSq.Length; i++)
            {
                double diff = sourceRow[i] - meanVector[i];
                targetSumSq[i] += diff * diff;
            }
        }

        // [target] = sqrt([target])
        public static void Sqrt(this Span<double> target)
        {
            for (int i = 0; i < target.Length; i++) target[i] = Math.Sqrt(target[i]);
        }

        // [targetRow] = ([sourceRow] - [meanVector]) / [stdDevVector]
        public static void ZNormalize(this Span<double> targetRow, ReadOnlySpan<double> sourceRow, ReadOnlySpan<double> meanVector, ReadOnlySpan<double> stdDevVector)
        {
            if (targetRow.Length != sourceRow.Length || targetRow.Length != meanVector.Length || targetRow.Length != stdDevVector.Length) throw new InvalidOperationException("Vector lengths mismatch");
            for (int i = 0; i < targetRow.Length; i++)
            {
                targetRow[i] = stdDevVector[i] > 0
                    ? (sourceRow[i] - meanVector[i]) / stdDevVector[i]
                    : 0.0;
            }
        }
    }
}
