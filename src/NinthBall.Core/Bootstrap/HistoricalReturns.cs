
namespace NinthBall.Core
{
    // Historical returns:
    // REF: https://pages.stern.nyu.edu/~adamodar/New_Home_Page/datafile/histretSP.html?utm_source=chatgpt.com

    /// <summary>
    /// Represents stocks and bonds ROI on a specific year.
    /// </summary>
    internal readonly record struct HROI(int Year, double StocksROI, double BondROI);

    /// <summary>
    /// Represents historical stocks and bonds ROI imported from Excel file.
    /// </summary>
    internal sealed class HistoricalReturns
    {
        private static readonly Lazy<IReadOnlyList<HROI>> LazyHistory = new(ReadHistoryOnce);

        public IReadOnlyList<HROI> History => LazyHistory.Value;

        private static IReadOnlyList<HROI> ReadHistoryOnce()
        {
            const string ROIHistoryResEndsWith = "ROI-History.xlsx";
            const string ROIHistorySheetName   = "DATA";

            var resAssembly = typeof(HistoricalReturns).Assembly;

            // Look for exactly one ROI-History.xlsx embedded resource.
            var roiHistoryResourceName = resAssembly
                .GetManifestResourceNames()
                .Where(x => x.EndsWith(ROIHistoryResEndsWith, StringComparison.OrdinalIgnoreCase))
                .Single();

            Console.WriteLine($" Reading resource | '{roiHistoryResourceName}'");

            // Open resource stream
            using var roiHistoryResourceStream = resAssembly
                .GetManifestResourceStream(roiHistoryResourceName)
                ?? throw new Exception("Unexpected: GetManifestResourceStream() returned null.");

            List<HROI> history = [];

            using (var xlReader = new ExcelReader(roiHistoryResourceStream))
            {
                var sheet = xlReader
                    .GetSheets()
                    .Where(s => ROIHistorySheetName.Equals(s.SheetName, StringComparison.OrdinalIgnoreCase))
                    .SingleOrDefault()
                    ?? throw new Exception($"Sheet not found | Resource: {roiHistoryResourceName} | Sheet: '{ROIHistorySheetName}'");

                foreach (var row in sheet.GetRows())
                {
                    if (null == row) continue;

                    // Skip first (header) row.  Do not use IEnumerable.Skip(1). Check Rowindex.
                    var isFirstRow = null != row.RowIndex && 1 == row.RowIndex.Value;
                    if (isFirstRow) continue;

                    var cells = row.GetCellValues().ToArray();

                    if (
                        null != cells
                        && cells.Length >= 3
                        && int.TryParse(cells[0], out var year)
                        && double.TryParse(cells[1], out var stocksROI)
                        && double.TryParse(cells[2], out var bondROI)
                    )
                    {
                        history.Add(new(year, stocksROI, bondROI));
                    }
                }
            }

            return history.AsReadOnly();
        }
    }
}
