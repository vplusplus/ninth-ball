
using DocumentFormat.OpenXml;

namespace NinthBall.Core
{
    // Represents a small window into the historical returns.
    readonly record struct HBlock
    (
        ReadOnlyMemory<HROI> Slice,     // Small slice of the history.
        HBlock.F Features               // Computed block-level Microeconomics characteristics.
    )
    {
        // Features of this block
        public readonly record struct F
        (
            // Nominal values
            double NominalCAGRStocks,
            double NominalCAGRBonds,
            double MaxDrawdownStocks,
            double MaxDrawdownBonds,
            double GMeanInflationRate,

            // Real values
            double RealCAGRStocks,
            double RealCAGRBonds,
            double RealCAGR6040
        );

        public readonly int StartYear => Slice.Span[0].Year;
        public readonly int EndYear   => Slice.Span[^1].Year;

        public static bool Overlaps(in HBlock prevBlock, in HBlock nextBlock) => nextBlock.StartYear <= prevBlock.EndYear && nextBlock.EndYear >= prevBlock.StartYear;
    }

    internal sealed class HistoricalBlocks(HistoricalReturns History, BootstrapOptions Options)
    {
        // Prepare blocks of suggested lengths once.
        readonly Lazy<IReadOnlyList<HBlock>> LazyBlocks = new(() => History.Returns.ReadBlocks(Options.BlockSizes).ToList().AsReadOnly());

        public IReadOnlyList<HBlock> Blocks => LazyBlocks.Value;

        public int MinYear => LazyBlocks.Value[0].StartYear;

        public int MaxYear => LazyBlocks.Value[^1].EndYear;

    }

    internal static class HistoricalBlocksExtensions
    {
        public static IEnumerable<HBlock> ReadBlocks(this ReadOnlyMemory<HROI> history, IReadOnlyList<int> blockSizes)
        {
            if (null == blockSizes || 0 == blockSizes.Count || blockSizes.Any(x => x <= 0)) throw new ArgumentException("Invalid block sizes.");

            // We depend on chronology, ensure the input data is sorted by year
            if (!IsSortedByYear(history.Span)) throw new Exception("Invalid history | Data is not sorted by year.");

            // History is sorted. Ensure block-sizes are also sorted.
            blockSizes = blockSizes.OrderBy(n => n).ToArray();

            // Yield overlapping blocks of requested sizes
            foreach (var blockLength in blockSizes)
            {
                var maxBlocks = history.Length - blockLength + 1;
                for (int startIndex = 0; startIndex < maxBlocks; startIndex++)
                {
                    var slice    = history.Slice(startIndex, blockLength);
                    var features = slice.ComputeBlockFeatures();

                    yield return new HBlock(slice, features);
                }
            }

            static bool IsSortedByYear(ReadOnlySpan<HROI> history)
            {
                for (int i = 1; i < history.Length; i++) if (history[i - 1].Year > history[i].Year) return false;
                return true;
            }
        }

        public static HBlock.F ComputeBlockFeatures(this ReadOnlyMemory<HROI> block)
        {
            if (0 == block.Length) throw new ArgumentException("Invalid HBlock | Block was empty.");

            return new
            (
                // Nominal values
                NominalCAGRStocks:  block.NominalCAGR(b => b.StocksROI),
                NominalCAGRBonds:   block.NominalCAGR(b => b.BondsROI),
                MaxDrawdownStocks:  block.NominalMaxDrawdown(b => b.StocksROI),
                MaxDrawdownBonds:   block.NominalMaxDrawdown(b => b.BondsROI),
                GMeanInflationRate: block.GeometricMean(b => b.InflationRate),

                // Real values
                RealCAGRStocks:     block.RealCAGR(b => b.StocksROI),
                RealCAGRBonds:      block.RealCAGR(b => b.BondsROI),
                RealCAGR6040:       block.RealCAGR6040()
            );
        }

        static double RealCAGR(this ReadOnlyMemory<HROI> block, Func<HROI, double> valueSelector)
        {
            double nominalCAGR      = block.GeometricMean(valueSelector);
            double inflationGeoMean = block.GeometricMean(x => x.InflationRate);

            return ((1.0 + nominalCAGR) / (1.0 + inflationGeoMean)) - 1.0;
        }

        static double NominalCAGR(this ReadOnlyMemory<HROI> window, Func<HROI, double> valueSelector)
        {
            return window.GeometricMean(valueSelector);
        }

        static double NominalMaxDrawdown(this ReadOnlyMemory<HROI> window, Func<HROI, double> valueSelector)
        {
            ArgumentNullException.ThrowIfNull(valueSelector);

            double peak = 1.0;
            double currentValue = 1.0;
            double maxDrawdown = 0.0;

            foreach (var hroi in window.Span)
            {
                var roi = valueSelector(hroi);
                currentValue *= (1 + roi);

                // Track new peak
                if (currentValue > peak) peak = currentValue;

                // Track worst drawdown from the peak
                double drawdown = (peak - currentValue) / peak;
                if (drawdown > maxDrawdown) maxDrawdown = drawdown;
            }

            // Less is bad.
            return -maxDrawdown;
        }

        static double RealCAGR6040(this ReadOnlyMemory<HROI> window)
        {
            // Real CAGR (inflation adjusted) for an imaginary 60/40 portfolio.
            const double SixtyPCT  = 0.6;
            const double FourtyPCT = 1 - SixtyPCT;

            return window.RealCAGR(x => x.StocksROI * SixtyPCT + x.BondsROI * FourtyPCT);
        }

        static double GeometricMean(this ReadOnlyMemory<HROI> block, Func<HROI, double> valueSelector)
        {
            ArgumentNullException.ThrowIfNull(valueSelector);
            if (0 == block.Length) throw new Exception("Invalid zero length block");

            double cumulativeMultiplier = 1.0;

            foreach (var hroi in block.Span)
            {
                // Accumulate
                double value = valueSelector(hroi);
                cumulativeMultiplier *= (1 + value);
            }

            // Annualize
            return Math.Pow(cumulativeMultiplier, 1.0 / block.Length) - 1.0;
        }

    }
}
