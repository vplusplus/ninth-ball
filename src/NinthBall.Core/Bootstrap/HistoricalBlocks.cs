
using DocumentFormat.OpenXml;

namespace NinthBall.Core
{
    // Represents a small window into the historical returns.
    // ARRScore is a ranking-score, an 'indicator' of good/bad windows.
    readonly record struct HBlock(ReadOnlyMemory<HROI> Slice, HBlock.F Features)
    {
        // Features of this block
        public readonly record struct F
        (
            double RealCAGRStocks,
            double RealCAGRBonds,
            double NominalCAGRStocks,
            double NominalCAGRBonds,
            double MaxDrawdownStocks,
            double MaxDrawdownBonds,
            double GMeanInflationRate,
            double RealCAGR6040
        );

        public readonly int StartYear => Slice.Span[0].Year;
        public readonly int EndYear   => Slice.Span[^1].Year;
        public static bool Overlaps(in HBlock prevBlock, in HBlock nextBlock) => nextBlock.StartYear <= prevBlock.EndYear && nextBlock.EndYear >= prevBlock.StartYear;
    }

    internal sealed class HistoricalBlocks(HistoricalReturns History, MovingBlockBootstrapOptions Options)
    {
        public IReadOnlyList<HBlock> Blocks => LazyBlocks.Value.Blocks;
        public int MinYear => LazyBlocks.Value.MinYear;
        public int MaxYear => LazyBlocks.Value.MaxYear;

        // Prepare all available blocks once.
        readonly Lazy<(IReadOnlyList<HBlock> Blocks, int MinYear, int MaxYear)> LazyBlocks = new(() =>
        {
            var availableBlocks = History.History.ReadBlocks(Options.BlockSizes).ToList().AsReadOnly();
            var minYear = availableBlocks.Min(x => x.StartYear);
            var maxYear = availableBlocks.Max(x => x.EndYear);

            return
            (
                Blocks:  availableBlocks,
                MinYear: minYear,
                MaxYear: maxYear
            );
        });
    }

    internal static class HistoricalBlockExtensions
    {
        public static IEnumerable<HBlock> ReadBlocks(this ReadOnlyMemory<HROI> history, IReadOnlyList<int> blockSizes)
        {
            if (null == blockSizes || 0 == blockSizes.Count || blockSizes.Any(x => x <= 0)) throw new ArgumentException("Invalid block sizes.");

            // We depend on chronology, ensure the input data is sorted by year
            if (!IsSortedByYear(history.Span)) throw new Exception("Invalid history | Data is not sorted by year.");

            // Defensive sort.
            blockSizes = blockSizes.OrderBy(n => n).ToArray();

            // Yield overlapping blocks of requested sizes
            foreach (var blockLength in blockSizes)
            {
                var maxBlocks = history.Length - blockLength + 1;
                for (int startIndex = 0; startIndex < maxBlocks; startIndex++)
                {
                    ReadOnlyMemory<HROI> slice = history.Slice(startIndex, blockLength);

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
                RealCAGRStocks: block.RealCAGR(b => b.StocksROI),
                RealCAGRBonds: block.RealCAGR(b => b.BondsROI),
                NominalCAGRStocks: block.NominalCAGR(b => b.StocksROI),
                NominalCAGRBonds: block.NominalCAGR(b => b.BondsROI),
                MaxDrawdownStocks: block.NominalMaxDrawdown(b => b.StocksROI),
                MaxDrawdownBonds: block.NominalMaxDrawdown(b => b.BondsROI),
                GMeanInflationRate: block.GeometricMean(b => b.InflationRate),
                RealCAGR6040: block.RealCAGR6040()
            );
        }

        static double GeometricMean(this ReadOnlyMemory<HROI> window, Func<HROI, double> valueSelector)
        {
            ArgumentNullException.ThrowIfNull(valueSelector);
            if (0 == window.Length) throw new Exception("Invalid zero length block");

            double cumulativeMultiplier = 1.0;

            foreach (var hroi in window.Span)
            {
                // Accumulate
                double value = valueSelector(hroi);
                cumulativeMultiplier *= (1 + value);
            }

            // Annualize
            return Math.Pow(cumulativeMultiplier, 1.0 / window.Length) - 1.0;
        }

        static double RealCAGR(this ReadOnlyMemory<HROI> window, Func<HROI, double> valueSelector)
        {
            ArgumentNullException.ThrowIfNull(valueSelector);
            if (0 == window.Length) throw new Exception("Invalid zero length block");

            double nominalCAGR = window.GeometricMean(valueSelector);
            double inflationGeoMean = window.GeometricMean(x => x.InflationRate);

            // (1 + nominal) / (1 + inflation) - 1
            return ((1.0 + nominalCAGR) / (1.0 + inflationGeoMean)) - 1.0;
        }

        static double NominalCAGR(this ReadOnlyMemory<HROI> window, Func<HROI, double> valueSelector)
        {
            ArgumentNullException.ThrowIfNull(valueSelector);
            if (0 == window.Length) throw new Exception("Invalid zero length block");

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

                // Calculate drawown from peak
                double drawdown = (peak - currentValue) / peak;

                // Track worst drawdown
                if (drawdown > maxDrawdown) maxDrawdown = drawdown;
            }

            return maxDrawdown;
        }

        static double RealCAGR6040(this ReadOnlyMemory<HROI> window)
        {
            // Real CAGR (inflation adjusted) for an imaginary 60/40 portfolio.
            const double SixtyPCT = 0.6;
            const double FourtyPCT = 1 - SixtyPCT;

            return window.RealCAGR(x => x.StocksROI * SixtyPCT + x.BondsROI * FourtyPCT);
        }

    }
}
