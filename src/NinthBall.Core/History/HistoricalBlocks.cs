
using DocumentFormat.OpenXml;

namespace NinthBall.Core
{
    //..........................................................................
    #region Models - HBlock and HBlockFeatures
    //..........................................................................
    // Represents a small window into the historical returns.
    public readonly record struct HBlock
    (
        ReadOnlyMemory<HROI> Slice,     // Small slice of the history.
        HBlockFeatures       Features   // Computed block-level Micro-economics characteristics.
    )
    {
        public readonly int StartYear => Slice.Span[0].Year;
        public readonly int EndYear => Slice.Span[^1].Year;
    }

    // Micro-economics characteristics of a small sequence of historical returns
    public readonly record struct HBlockFeatures
    (
        // Nominal values
        double NominalCAGRStocks,
        double NominalCAGRBonds,
        double MaxDrawdownStocks,
        double MaxDrawdownBonds,
        double GMeanInflationRate
    );

    #endregion

    internal sealed class HistoricalBlocks(HistoricalReturns History, BootstrapOptions Options)
    {
        readonly Lazy<IReadOnlyList<HBlock>> LazyBlocks = new( History.Returns.ReadBlocks(Options.BlockSizes) );

        public IReadOnlyList<HBlock> Blocks => LazyBlocks.Value;

        public int MinYear => LazyBlocks.Value[0].StartYear;

        public int MaxYear => LazyBlocks.Value[^1].EndYear;

    }

    internal static class HistoricalBlocksExtensions
    {
        public static IReadOnlyList<HBlock> ReadBlocks(this ReadOnlyMemory<HROI> history, IReadOnlyList<int> blockSizes)
        {
            if (null == blockSizes || 0 == blockSizes.Count || blockSizes.Any(x => x <= 0 || x > history.Length)) throw new ArgumentException("Invalid block sizes.");

            // We depend on chronology, ensure the historical data is sorted by year
            if (!IsSortedByYear(history.Span)) throw new Exception("Invalid history | Data is not sorted by year.");

            // History is sorted. Ensure block-sizes are also sorted.
            blockSizes = blockSizes.OrderBy(n => n).ToArray();

            var blocks = new List<HBlock>();

            // Collect overlapping blocks of requested sizes
            foreach (var blockLength in blockSizes)
            {
                var maxBlocks = history.Length - blockLength + 1;

                for (int startIndex = 0; startIndex < maxBlocks; startIndex++)
                {
                    blocks.Add( new HBlock(
                        history.Slice(startIndex, blockLength),
                        history.Slice(startIndex, blockLength).ComputeBlockFeatures()
                    ));
                }
            }

            // Order of blocks should not matter since block selection is uniform.
            // Repeatable (order-by year & length) and immutable (AsReadOnly). 
            return blocks
                .OrderBy(x => x.StartYear)
                .ThenBy(x => x.Slice.Length)
                .ToList()
                .AsReadOnly();

            static bool IsSortedByYear(ReadOnlySpan<HROI> history)
            {
                for (int i = 1; i < history.Length; i++) if (history[i - 1].Year > history[i].Year) return false;
                return true;
            }
        }

        static HBlockFeatures ComputeBlockFeatures(this ReadOnlyMemory<HROI> block)
        {
            if (0 == block.Length) throw new ArgumentException("Invalid HBlock | Block was empty.");

            return new
            (
                // Nominal values
                NominalCAGRStocks:  block.NominalCAGR(b => b.StocksROI),
                NominalCAGRBonds:   block.NominalCAGR(b => b.BondsROI),
                MaxDrawdownStocks:  block.NominalMaxDrawdown(b => b.StocksROI),
                MaxDrawdownBonds:   block.NominalMaxDrawdown(b => b.BondsROI),
                GMeanInflationRate: block.GeometricMean(b => b.InflationRate)

                // Real values
                // RealCAGRStocks:     block.RealCAGR(b => b.StocksROI),
                // RealCAGRBonds:      block.RealCAGR(b => b.BondsROI)
                // RealCAGR6040:       block.RealCAGR(x => x.StocksROI * SixtyPCT + x.BondsROI * FortyPCT)
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
