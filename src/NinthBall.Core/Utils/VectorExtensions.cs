
namespace NinthBall.Core
{
    internal static class VectorExtensions
    {
        // int Random.Next(0, weightes.Length) - biased by suggested weighted 
        public static int NextWeightedIndex(this Random R, ReadOnlySpan<double> weights)
        {
            if (0 == weights.Length) throw new ArgumentException("Empty or NULl weights.");

            double totalWeights = weights.Sum();
            double randomValue = R.NextDouble() * totalWeights;

            double cumulative = 0.0;
            for (int i = 0; i < weights.Length; i++)
            {
                cumulative += weights[i];
                if (randomValue <= cumulative) return i;
            }

            return weights.Length - 1;
        }


        // [target] += [source]
        public static void Add(this Span<double> target, ReadOnlySpan<double> source)
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

        public static void SumSquaredDiff(this Span<double> targetSumSq, ReadOnlySpan<double> sourceRow, ReadOnlySpan<double> meanVector)
        {
            if (targetSumSq.Length != sourceRow.Length || targetSumSq.Length != meanVector.Length) throw new InvalidOperationException("Vector lengths mismatch");

            for (int i = 0; i < targetSumSq.Length; i++)
            {
                double diff = sourceRow[i] - meanVector[i];
                targetSumSq[i] += diff * diff;
            }
        }

        public static void Sqrt(this Span<double> values)
        {
            for (int i = 0; i < values.Length; i++) values[i] = Math.Sqrt(values[i]);
        }

        // [targetRow] = ([sourceRow] - [meanVector]) / [stdDevVector]
        public static void ZNormalize(this ReadOnlySpan<double> sourceRow, ReadOnlySpan<double> meanVector, ReadOnlySpan<double> stdDevVector, Span<double> targetRow)
        {
            if (targetRow.Length != sourceRow.Length || targetRow.Length != meanVector.Length || targetRow.Length != stdDevVector.Length) throw new InvalidOperationException("Vector lengths mismatch");
            for (int i = 0; i < targetRow.Length; i++)
            {
                targetRow[i] = stdDevVector[i] > 0
                    ? (sourceRow[i] - meanVector[i]) / stdDevVector[i]
                    : 0.0;
            }
        }

        // Squared straight-line distance (Euclidean) in a multi-dimensional-space.
        public static double EuclideanDistanceSquared(this ReadOnlySpan<double> a, ReadOnlySpan<double> b)
        {
            if (a.Length != b.Length) throw new InvalidOperationException($"Vector lengths are not same | [{a.Length}] and [{b.Length}]");

            double sum = 0.0;
            for (int i = 0; i < a.Length; i++)
            {
                double diff = a[i] - b[i];
                sum += diff * diff;
            }
            return sum;
        }

        public static void ToProbabilityDistribution(this Span<double> values)
        {
            // Edge cases: Zero items or only one item
            if (0 == values.Length)
            {
                throw new ArgumentException(nameof(values), "List of values was empty.");
            }
            else if (1 == values.Length) {
                values[0] = 1.0;
                return;
            }

            // Sum of given values
            double total = values.Sum();

            // Another edge case: All values are zero
            if (0 == total)
            {
                for (int i = 0; i < values.Length; i++) values[i] = 1.0 / values.Length;
                return;
            }

            // Compute probabilty distribution
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = values[i] / total;
            }

            // Drop double precision dust. sum(values) should be 1.0
            values[values.Length - 1] += 1.0 - values.Sum();
        }

        public static double Sum(this ReadOnlySpan<double> values)
        {
            double sum = 0.0;
            for (int i = 0; i < values.Length; i++) sum += values[i];
            return sum;
        }

        public static double Mean(this ReadOnlySpan<double> values)
        {
            return values.IsEmpty ? 0 : values.Sum() / values.Length;
        }

        public static double Variance(this ReadOnlySpan<double> values, double? mean = null)
        {
            if (values.Length < 2) return 0;
            double m = mean ?? values.Mean();
            double sumSqDiff = 0;
            for (int i = 0; i < values.Length; i++)
            {
                double diff = values[i] - m;
                sumSqDiff += diff * diff;
            }
            return sumSqDiff / (values.Length - 1);
        }

        public static double StdDev(this ReadOnlySpan<double> values, double? mean = null) => Math.Sqrt(values.Variance(mean));

        public static double Skewness(this ReadOnlySpan<double> values, double? mean = null, double? stdDev = null)
        {
            int n = values.Length;
            if (n < 3) return 0;
            double m = mean ?? values.Mean();
            double s = stdDev ?? values.StdDev(m);
            if (s <= 1e-10) return 0;

            double sumCubedDiff = 0;
            for (int i = 0; i < n; i++)
            {
                double z = (values[i] - m) / s;
                sumCubedDiff += z * z * z;
            }

            return (double)n / ((n - 1) * (n - 2)) * sumCubedDiff;
        }

        public static double Kurtosis(this ReadOnlySpan<double> values, double? mean = null, double? stdDev = null)
        {
            int n = values.Length;
            if (n < 4) return 0;
            double m = mean ?? values.Mean();
            double s = stdDev ?? values.StdDev(m);
            if (s <= 1e-10) return 0;

            double sumFourthDiff = 0;
            for (int i = 0; i < n; i++)
            {
                double z = (values[i] - m) / s;
                sumFourthDiff += z * z * z * z;
            }

            double term1 = ((double)n * (n + 1)) / ((n - 1) * (n - 2) * (n - 3));
            double term2 = (3.0 * (n - 1) * (n - 1)) / ((n - 2) * (n - 3));
            return term1 * sumFourthDiff - term2;
        }

        public static double Correlation(this ReadOnlySpan<double> x, ReadOnlySpan<double> y)
        {
            if (x.Length != y.Length || x.Length < 2) return 0;
            double mx = x.Mean();
            double my = y.Mean();
            double sx = x.StdDev(mx);
            double sy = y.StdDev(my);
            if (sx <= 1e-10 || sy <= 1e-10) return 0;

            double sumProduct = 0;
            for (int i = 0; i < x.Length; i++)
            {
                sumProduct += (x[i] - mx) * (y[i] - my);
            }

            return sumProduct / ((x.Length - 1) * sx * sy);
        }

        public static double AutoCorrelation(this ReadOnlySpan<double> values, int lag = 1)
        {
            if (values.Length <= lag + 1) return 0;
            return values.Slice(lag).Correlation(values.Slice(0, values.Length - lag));
        }
    }
}
