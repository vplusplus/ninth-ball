
namespace NinthBall.Core
{
    internal static class HistoryExtensions
    {
        public static HBlock.F ComputeBlockFeatures(this ReadOnlyMemory<HROI> block)
        {
            if (0 == block.Length) throw new ArgumentException("Invalid HBlock | Block was empty.");

            return new
            (
                RealCAGRStocks:     block.RealCAGR(b => b.StocksROI),
                RealCAGRBonds:      block.RealCAGR(b => b.BondsROI),
                NominalCAGRStocks:  block.NominalCAGR(b => b.StocksROI),
                NominalCAGRBonds:   block.NominalCAGR(b => b.BondsROI),
                MaxDrawdownStocks:  block.NominalMaxDrawdown(b => b.StocksROI),
                MaxDrawdownBonds:   block.NominalMaxDrawdown(b => b.BondsROI),
                GMeanInflationRate: block.GeometricMean(b => b.InflationRate),
                RealCAGR6040:       block.RealCAGR6040()
            );
        }

        static double GeometricMean(this ReadOnlyMemory<HROI> window, Func<HROI, double> valueSelector)
        {
            ArgumentNullException.ThrowIfNull(valueSelector);
            if (0 == window.Length) throw new Exception("Invalid zero length block");

            double cumulativeMultiplier = 1.0;

            foreach(var hroi in window.Span)
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

/*
         static double Mean(this ReadOnlyMemory<HROI> window, Func<HROI, double> valueSelector)
        {
            if (0 == window.Length) return 0;

            double sum = 0.0;
            foreach(var roi in window.Span) sum += valueSelector(roi);
            return sum / window.Length;
        }
*/