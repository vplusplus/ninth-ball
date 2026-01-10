
using NinthBall.Core;


namespace NinthBall.Outputs
{
    internal enum ColorHint { None, Success, Warning, Danger, Muted }

    internal static partial class ColumnDefinitions
    {
        internal static ColorHint GetCellColorHint(this SimYear y, CID cid, SimIteration iter) => FxColors.TryGetValue(cid, out var fxColor) && null != fxColor ? fxColor(iter, y) : ColorHint.None;


        delegate ColorHint ColorSelector(SimIteration iteration, SimYear year);

        static readonly IReadOnlyDictionary<CID, ColorSelector> FxColors = new Dictionary<CID, ColorSelector>()
        {
            [CID.LikeYear]  = (it, y) => ROIRedGreyGreen(y),
            [CID.ROI]       = (it, y) => ROIRedGreyGreen(y),
            [CID.ROIStocks] = (it, y) => ROIRedGreyGreen(y.ROI.StocksROI),
            [CID.ROIBonds]  = (it, y) => ROIRedGreyGreen(y.ROI.BondsROI),
            [CID.ROICash]   = (it, y) => ROIRedGreyGreen(y.ROI.CashROI),

        }.AsReadOnly();

        static ColorHint ROIRedGreyGreen(SimYear simYear) => ROIRedGreyGreen(simYear.EffectiveROI);
        static ColorHint ROIRedGreyGreen(double pctValue) => pctValue >= -0.04 && pctValue <= +0.04 ? ColorHint.Muted : pctValue <= 0 ? ColorHint.Danger : ColorHint.Success;

    }
}
