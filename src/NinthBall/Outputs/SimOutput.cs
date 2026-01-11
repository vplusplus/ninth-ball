
namespace NinthBall.Outputs
{
    public readonly record struct Percentile(double Pctl, string FriendlyName)
    {
        public const double Target = 0.20;
        public string PctlName => $"{Pctl * 100:0}th";
    }

    /// <summary>
    /// User preferences for report outputs.
    /// Loaded from SimOutput.yaml
    /// </summary>
    public record SimOutput
    (
        IReadOnlyList<Percentile>? Percentiles,
        IReadOnlyDictionary<string, IReadOnlyList<CID>>? Views,
        string? HtmlView,
        string? ExcelView
    );


    internal static class SimOutputExtensions
    {
        extension(SimOutput simOutput)
        {
            public IReadOnlyList<Percentile> GetPercentilesOrDefault() => simOutput?.Percentiles ?? DefaultPercentiles;
            public IReadOnlyList<CID> GetHtmlColumnsOrDefaults() => simOutput.GetUsderDefinedColumnsOrDefaults(simOutput?.HtmlView ?? "",  DefaultHtmlColumns);
            public IReadOnlyList<CID> GetExcelColumnsOrDefaults() => simOutput.GetUsderDefinedColumnsOrDefaults(simOutput?.ExcelView?? "", DefaultExcelColumns);

            IReadOnlyList<CID> GetUsderDefinedColumnsOrDefaults(string viewName, IReadOnlyList<CID> defaultColumns)
            {
                IReadOnlyList<CID>? userDefinedColumns = null;

                var isDefined = simOutput?.Views?.TryGetValue(viewName, out userDefinedColumns);

                return true == isDefined && null != userDefinedColumns && userDefinedColumns.Count > 0
                    ? userDefinedColumns
                    : defaultColumns;
            }
        }

        // TODO: Its public for now for Excel reports. Make it private once excel reports are refactored.
        public static readonly IReadOnlyList<Percentile> DefaultPercentiles =
        [
            new(0.00, "Worst-case"),
            new(0.05, "Unlucky"),
            new(0.10, "Unfortunate"),
            new(0.20, "Target"),
            new(0.50, "Coin-flip"),
            new(0.80, "Fortunate"),
            new(0.90, "Lucky"),
        ];

        // Default columns presented in html output if not configured.
        private static readonly IReadOnlyList<CID> DefaultHtmlColumns =
        [
            CID.Year, CID.Age,
            CID.JanPreTax, CID.JanPostTax, CID.JanValue,

            CID.Fees,
            CID.PYTaxes,
            CID.LivExp,

            CID.SS,
            CID.Ann,

            CID.XPreTax, CID.XPostTax,
            CID.ROIAmount,
            CID.DecValue,

            CID.LikeYear, CID.ROI, CID.AnnROI, CID.ROIStocks, CID.ROIBonds
        ];


        // Default columns presented in Excel output if not configured.
        private static readonly IReadOnlyList<CID> DefaultExcelColumns =
        [
            CID.Year, CID.Age,
            CID.JanPreTax, CID.JanPostTax, CID.JanValue,
            CID.DecValue,
            CID.LikeYear, CID.ROI, CID.ROIStocks, CID.ROIBonds
        ];
    }
}
