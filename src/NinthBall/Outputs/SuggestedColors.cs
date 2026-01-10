
using Microsoft.VisualBasic;
using NinthBall.Core;


namespace NinthBall.Outputs
{
    internal enum ColorHint { None, Success, Warning, Danger, Primary, Muted }

    internal static partial class ColumnDefinitions
    {
        internal static ColorHint GetCellColorHint(this SimYear y, CID cid, SimIteration iter) => FxColors.TryGetValue(cid, out var fxColor) && null != fxColor ? fxColor(iter, y) : ColorHint.None;


        delegate ColorHint ColorSelector(SimIteration iteration, SimYear year);

        static readonly IReadOnlyDictionary<CID, ColorSelector> FxColors = new Dictionary<CID, ColorSelector>()
        {
            [CID.JanValue] = (it, y) => ColorHint.Primary,
            [CID.DecValue] = (it, y) => ColorHint.Primary,

            [CID.LikeYear]  = ROIRedGreyGreen,
            [CID.ROI]       = ROIRedGreyGreen,
            [CID.ROIStocks] = (it, y) => ROIRedGreyGreen(y.ROI.StocksROI),
            [CID.ROIBonds]  = (it, y) => ROIRedGreyGreen(y.ROI.BondsROI),
            [CID.ROICash]   = (it, y) => ROIRedGreyGreen(y.ROI.CashROI),
            [CID.CYExp]     = (it, y) => WarnOnStepDown(it, y),
            [CID.XPreTax]   = (it, y) => RedGreen(y.XPreTax),
            [CID.XPostTax]  = (it, y) => RedGreen(y.XPostTax),

        }.AsReadOnly();

        static ColorHint RedGreen(double value) => value < 0 ? ColorHint.Danger : ColorHint.Success;

        static ColorHint ROIRedGreyGreen(SimIteration simIteration, SimYear simYear) => ROIRedGreyGreen(simYear.EffectiveROI);
        static ColorHint ROIRedGreyGreen(double pctValue) => pctValue >= -0.04 && pctValue <= +0.04 ? ColorHint.Muted : pctValue <= 0 ? ColorHint.Danger : ColorHint.Success;

        static ColorHint WarnOnStepDown(SimIteration simIteration, SimYear simYear)
        {
            var cyExp = simYear.Expenses.CYExp;
            var pyExp = simYear.Year > 1 ? simIteration.ByYear.Span[simYear.Year - 1].Expenses.CYExp : double.MinValue;
            var expReduction = cyExp < pyExp;

            return expReduction ? ColorHint.Warning : ColorHint.None;
        }

    }
}
