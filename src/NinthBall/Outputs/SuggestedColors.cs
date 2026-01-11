
using NinthBall.Core;

namespace NinthBall.Outputs
{
    internal enum ColorHint { None, Success, Warning, Danger, Primary, Muted }

    internal static partial class ColumnDefinitions
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
            [CID.JanValue]  = (SimIteration it, in SimYear y) => ColorHint.Primary,
            [CID.DecValue]  = (SimIteration it, in SimYear y) => ColorHint.Primary,

            [CID.LikeYear]  = (SimIteration it, in SimYear y) => ROIRedGreyGreen(it, in y),
            [CID.ROI]       = (SimIteration it, in SimYear y) => ROIRedGreyGreen(it, in y),
            [CID.ROIStocks] = (SimIteration it, in SimYear y) => ROIRedGreyGreen(y.ROI.StocksROI),
            [CID.ROIBonds]  = (SimIteration it, in SimYear y) => ROIRedGreyGreen(y.ROI.BondsROI),
            [CID.ROICash]   = (SimIteration it, in SimYear y) => ROIRedGreyGreen(y.ROI.CashROI),
            [CID.CYExp]     = (SimIteration it, in SimYear y) => WarnOnStepDown(it, in y),
            [CID.XPreTax]   = (SimIteration it, in SimYear y) => RedGreen(y.XPreTax),
            [CID.XPostTax]  = (SimIteration it, in SimYear y) => RedGreen(y.XPostTax),
            [CID.Change]    = (SimIteration it, in SimYear y) => RedGreen(y.Change.Total()),

        }.AsReadOnly();

        static ColorHint RedGreen(double value) => value < 0 ? ColorHint.Danger : ColorHint.Success;

        static ColorHint ROIRedGreyGreen(SimIteration simIteration, in SimYear simYear) => ROIRedGreyGreen(simYear.EffectiveROI);

        static ColorHint ROIRedGreyGreen(double pctValue) => pctValue >= -0.04 && pctValue <= +0.04 ? ColorHint.Muted : pctValue <= 0 ? ColorHint.Danger : ColorHint.Success;

        static ColorHint WarnOnStepDown(SimIteration simIteration, in SimYear simYear)
        {
            var cyExp = simYear.Expenses.CYExp;
            var pyExp = simYear.Year > 1 ? simIteration.ByYear.Span[simYear.Year - 1].Expenses.CYExp : double.MinValue;
            var expReduction = cyExp < pyExp;

            return expReduction ? ColorHint.Warning : ColorHint.None;
        }
    }
}
