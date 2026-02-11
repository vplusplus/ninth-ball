
namespace NinthBall.Core
{
    /// <summary>
    /// Immutable two dim matrix represented as a single block of readonly memory
    /// </summary>
    public readonly record struct TwoDMatrix(ReadOnlyMemory<double> Storage, int NumRows, int NumColumns)
    {
        public readonly ReadOnlySpan<double> this[int row] => Storage.Slice(row * NumColumns, NumColumns).Span;

        public readonly double this[int row, int col] => Storage.Span[row * NumColumns + col];
    }

    /// <summary>
    /// Mutable two dim matrix represented as a single block of readonly memory
    /// </summary>
    public readonly record struct XTwoDMatrix(int NumRows, int NumColumns)
    {
        // Mutable: Full data
        public readonly double[] Storage = new double[NumRows * NumColumns];

        // Mutable: One row
        public readonly Span<double> this[int idx] => Storage.AsSpan().Slice(idx * NumColumns, NumColumns);

        // Mutable: One cell
        public double this[int row, int col]
        {
            get => this[row][col];
            set => this[row][col] = value;
        }

        // Returns an immutable version - Zero allocation or zero copy
        public static implicit operator TwoDMatrix(XTwoDMatrix mutable) => new (mutable.Storage, mutable.NumRows, mutable.NumColumns);

        // Returns an immutable version - Zero allocation or zero copy
        public readonly TwoDMatrix ReadOnly => this;
    }

}
