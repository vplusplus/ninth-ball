
using NinthBall.Core;

namespace NinthBall.Outputs
{
    internal enum ColorHint { None, Success, Warning, Danger, Primary, Muted }

    internal static partial class Suggested
    {
        private delegate ColorHint ColorSelector(SimIteration iteration, in SimYear year);

        /// <summary>
        /// Retrieves the color hint for a specific cell for a given year and column.
        /// Returns ColorHint for the cell, or None if not defined.
        /// </summary>
        internal static ColorHint GetCellColorHint(this SimYear y, CID cid, SimIteration iter) => 
            FxColors.TryGetValue(cid, out var fxColor) && null != fxColor 
                ? fxColor(iter, in y) 
                : ColorHint.None;

        static readonly IReadOnlyDictionary<CID, ColorSelector> FxColors = new Dictionary<CID, ColorSelector>()
        {
            // Anchor information:
            // Neither good or bad on their own.
            // Can reach zero, can't go negative. Single color.
            [CID.JanValue]  = (SimIteration it, in SimYear y) => ColorHint.Primary,
            [CID.DecValue]  = (SimIteration it, in SimYear y) => ColorHint.Primary,

            // Market noise: 
            // Individual asset returns are uncolored to focus the user's attention 
            // on the success of their diversification strategy rather than pure market noise.
            //[CID.ROIStocks] = (it, in y)  => ColorHint.None,
            //[CID.ROIBonds]  = (it, in y)  => ColorHint.None,
            //[CID.ROICash]   = (it, in y)  => ColorHint.None,

            // The "Verdict" (Real ROI Strategy)
            // Instead of nominal zeros, we color based on "Real Growth" (Purchasing Power).
            // Red: Losing ground to inflation.
            // Grey: Maintenance zone (+/- 1.5% around inflation).
            // Green: Growing real wealth.
            [CID.ROI]       = (it, in y) => RealROIRedGreyGreen(y),
            [CID.AnnROI]    = (it, in y) => RealAnnROIRedGreyGreen(it, y),
            [CID.RealCAGR]  = (it, in y) => RealAnnROIRedGreyGreen(it, y),

            // Signals the "Market Era" - remains based on nominal performance for context.
            [CID.LikeYear]  = (it, in y) => EffectiveROIRedGreyGreen(y),


            // Environmental Signal (Terrain)
            // Indicates when the environment is Friendly (Green), Normal (Grey), or Hostile (Red).
            // This provides the context for WHY the ROI might be lagging or succeeding.
            [CID.InflationRate] = (it, in y) => InflationHostilityRedGreyGreen(y.ROI.InflationRate),

            // Cashflow and Portfolio Changes (Polarity Signals)
            // Simple Red/Green banding based on the "Direction" of the money.
            // Helps identify spending patterns and portfolio growth at a glance.
            [CID.LivExp]    = (it, in y) => ColorHint.None,
            [CID.XPreTax]   = (it, in y) => PolarityRedGreen(y.XPreTax),
            [CID.XPostTax]  = (it, in y) => PolarityRedGreen(y.XPostTax),
            [CID.ROIAmount] = (it, in y) => PolarityRedGreen(y.Change.Total()),

        }.AsReadOnly();

        static ColorHint PolarityRedGreen(double value) => value < 0 ? ColorHint.Danger : ColorHint.Success;

        static ColorHint RealROIRedGreyGreen(in SimYear y) 
        {
            if (double.IsNaN(y.EffectiveROI)) return ColorHint.None;

            // Purchasing Power Parity (Standard financial term: Nominal ROI - Inflation = Real Return)
            var realReturn = y.EffectiveROI - y.ROI.InflationRate;  

            const double GreyBand = 0.015;
            return realReturn > GreyBand ? ColorHint.Success : realReturn < -GreyBand ? ColorHint.Danger : ColorHint.Muted;

        }

        static ColorHint RealAnnROIRedGreyGreen(SimIteration it, in SimYear y)
        {
            if (double.IsNaN(y.RunningAnnualizedROI)) return ColorHint.None;

            // Accurate geometric real return: ((1 + Nominal) / (1 + Inflation)) - 1
            var avgInflation = Math.Pow(y.RunningInflationMultiplier, 1.0 / (y.Year + 1)) - 1.0;
            var realReturn = ((1 + y.RunningAnnualizedROI) / (1 + avgInflation)) - 1;

            // Survival Benchmarks (Domain Specific):
            // Red:   < 1.9% (Falling behind the "4% Rule" benchmark).
            // Grey:  1.9% - 3.5% (Meeting benchmark, but stay cautious).
            // Green: > 3.5% (Strong margin of safety, sleep well).
            const double FourPctRuleThreshold = 0.019;  
            const double MarginOfSafety = 0.035; 

            return realReturn < FourPctRuleThreshold ? ColorHint.Danger
                : realReturn > MarginOfSafety ? ColorHint.Success
                : ColorHint.Muted;
        }

        static ColorHint EffectiveROIRedGreyGreen(in SimYear y)
        {
            if (double.IsNaN(y.EffectiveROI)) return ColorHint.None;
            var roi = y.EffectiveROI;
            return roi > 0.04 ? ColorHint.Success : roi < -0.04 ? ColorHint.Danger : ColorHint.Muted;
        }

        static ColorHint InflationHostilityRedGreyGreen(double pctValue) => 
            pctValue <= 0.020 ? ColorHint.Success : // Friendly
            pctValue <= 0.035 ? ColorHint.Muted   : // Normal
                                ColorHint.Danger;  // Hostile
    }
}
