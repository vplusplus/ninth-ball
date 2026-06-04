
using NinthBall.Core;

namespace NinthBall.Reports
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
            [CID.LikeYear]  = (it, in y) => BadYearIsRed(y),

            [CID.Infl] = (it, in y) => InflationRedGreyGreen(y.ROI.InflationRate),

            [CID.ROIStocks] = (it, in y) => ROIVsInflation(y.ROI.StocksROI, inflation: y.ROI.InflationRate),
            [CID.ROIBonds]  = (it, in y) => ROIVsInflation(y.ROI.BondsROI, inflation: y.ROI.InflationRate),
            [CID.ROI]       = (it, in y) => ROIVsInflation(y.RunningGrowth.PortfolioReturn, inflation: y.ROI.InflationRate), 

            [CID.LivExp]    = (it, in y) => ColorHint.None,
            [CID.XPreTax]   = (it, in y) => RedGreen(y.XPreTax),
            [CID.XPostTax]  = (it, in y) => RedGreen(y.XPostTax),

            [CID.ChangePreTax]  = (it, in y) => RedGreen(y.Change.PreTax),
            [CID.ChangePostTax] = (it, in y) => RedGreen(y.Change.PostTax),
            [CID.Change]        = (it, in y) => RedGreen(y.Change.Total),

            [CID.CAGRReal] = (it, in y) => RealAnnROIRedGreyGreen(it, y),
            [CID.DecReal] = (it, in y) => ColorHint.Primary,

        }.AsReadOnly();

        static ColorHint RedGreen(double value) => value < 0 ? ColorHint.Danger : ColorHint.Success;

        static ColorHint RealROIRedGreyGreen(in SimYear y) 
        {
            if (double.IsNaN(y.RunningGrowth.PortfolioReturn)) return ColorHint.None; 

            // Purchasing Power Parity (Standard financial term: Nominal ROI - Inflation = Real Return)
            var realReturn = y.RunningGrowth.PortfolioReturn - y.ROI.InflationRate;  

            const double GreyBand = 0.015;
            return realReturn > GreyBand ? ColorHint.Success : realReturn < -GreyBand ? ColorHint.Danger : ColorHint.Muted;
        }

        static ColorHint BadYearIsRed(in SimYear y)
        {
            return
                y.RunningGrowth.PortfolioReturn < 0 || y.ROI.InflationRate > 0.05 ? ColorHint.Danger :
                y.RunningGrowth.PortfolioReturn - y.ROI.InflationRate > 0.0 ? ColorHint.Success :
                ColorHint.Muted;

            //if (double.IsNaN(y.RunningGrowth.PortfolioReturn)) return ColorHint.None;

            //// Purchasing Power Parity (Standard financial term: Nominal ROI - Inflation = Real Return)
            //var realReturn = y.RunningGrowth.PortfolioReturn - y.ROI.InflationRate;

            //const double GreyBand = 0.015;
            //return realReturn > GreyBand ? ColorHint.Success : realReturn < -GreyBand ? ColorHint.Danger : ColorHint.Muted;
        }


        static ColorHint RealAnnROIRedGreyGreen(SimIteration it, in SimYear y)
        {
            if (double.IsNaN(y.RunningGrowth.RealAnnualizedReturn)) return ColorHint.None;

            var realReturn = y.RunningGrowth.RealAnnualizedReturn;

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
            if (double.IsNaN(y.RunningGrowth.PortfolioReturn)) return ColorHint.None;
            var roi = y.RunningGrowth.PortfolioReturn;
            return roi > 0.04 ? ColorHint.Success : roi < -0.04 ? ColorHint.Danger : ColorHint.Muted;
        }

        static ColorHint InflationRedGreyGreen(double pctValue) => pctValue <= 0.030 ? ColorHint.Success : pctValue <= 0.05 ? ColorHint.Muted : ColorHint.Danger;


        static ColorHint ROIVsInflation(double roi, double inflation) =>
            roi < 0.0 ? ColorHint.Danger :
            roi < inflation ? ColorHint.Muted :
            ColorHint.Success;

    }
}
