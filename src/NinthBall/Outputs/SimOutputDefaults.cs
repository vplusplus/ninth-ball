
namespace NinthBall.Outputs
{
    internal static class SimOutputDefaults
    {
        // Default percentiles presented if user had not configured one.
        public static IReadOnlyList<double> DefaultPercentiles => [0.01, 0.05, 0.10, 0.20, 0.50, 0.80];

        // What is considered as a target to aim for.
        public const double TargetPercentile = 0.2;

        // Default columns presented if user had not configured one.
        public static readonly IReadOnlyList<CID> DefaultColumns =
        [
            CID.Year, CID.Age,
            CID.JanPreTax, CID.JanPostTax,
            CID.SS, CID.Ann,
            CID.Fees, CID.PYTaxes, CID.LivExp,
            CID.XPreTax, CID.XPostTax,
            CID.LikeYear, CID.ROIStocks, CID.ROIBonds, CID.InflationRate,
            CID.AnnROI, CID.DecValue,
        ];
    }
}
