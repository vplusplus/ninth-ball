
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
    }
}
