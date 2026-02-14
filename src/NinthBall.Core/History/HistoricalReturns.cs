
using System.Globalization;
using NinthBall.Utils;

// Historical data source:
// https://pages.stern.nyu.edu/~adamodar/New_Home_Page/datafile/histretSP.html

namespace NinthBall.Core
{
    /// <summary>
    /// Represents historical stocks/bonds ROI and Inflation rates.
    /// </summary>
    internal sealed class HistoricalReturns
    {
        public ReadOnlyMemory<HROI> Returns => LazySortedHistory.Value;
        public int FromYear => LazySortedHistory.Value.Span[0].Year;
        public int ToYear   => LazySortedHistory.Value.Span[^1].Year;

        //......................................................................
        // Historical returns, read-once and chronologically ordered.
        //......................................................................
        private static readonly Lazy<ReadOnlyMemory<HROI>> LazySortedHistory = new(ReadHistory);

        // Read, parse, sort and validate historical data from embedded resource. 
        public static ReadOnlyMemory<HROI> ReadHistory()
        {
            const string ResNameEndsWith = "ROI-History.xlsx";
            const string SheetName = "DATA";

            var resAssembly = typeof(HistoricalReturns).Assembly;
            var resName = resAssembly.GetManifestResourceNames().Single(x => x.EndsWith(ResNameEndsWith, StringComparison.OrdinalIgnoreCase));
            using var resStream = resAssembly.GetManifestResourceStream(resName) ?? throw new Exception($"Unexpected: Resource stream on {resName} was null.");

            var history = new List<HROI>(200);

            using (var xlReader = new ExcelReader(resStream))
            {
                var sheet = xlReader
                    .GetSheets()
                    .Where(s => SheetName.Equals(s.SheetName, StringComparison.OrdinalIgnoreCase))
                    .SingleOrDefault()
                        ?? throw new Exception($"Sheet not found | Resource: {resName} | Sheet: '{SheetName}'");

                foreach (var row in sheet.GetRows())
                {
                    if (null == row) continue;

                    // Skip first (header) row.  Do not use IEnumerable.Skip(1). Check RowIndex.
                    var isFirstRow = null != row.RowIndex && 1 == row.RowIndex.Value;
                    if (isFirstRow) continue;

                    var cells = row.GetCellValues().ToArray();

                    int cellIndex = 0;
                    if (
                        null != cells
                        && cells.Length >= 4
                        && int.TryParse(cells[cellIndex++], out var year)
                        && double.TryParse(cells[cellIndex++], NumberStyles.Float, CultureInfo.InvariantCulture, out var stocksROI)
                        && double.TryParse(cells[cellIndex++], NumberStyles.Float, CultureInfo.InvariantCulture, out var bondsROI)
                        && double.TryParse(cells[cellIndex++], NumberStyles.Float, CultureInfo.InvariantCulture, out var inflationRate)
                    )
                    {
                        history.Add(new(year, StocksROI: stocksROI, BondsROI: bondsROI, InflationRate: inflationRate));
                    }
                }
            }

            // Repeatable (sort by year) and read-only (to memory)
            var sortedHistory = history.OrderBy(x => x.Year).ToArray().AsMemory();

            // Check data quality
            return (0 == sortedHistory.Length || sortedHistory.Length != sortedHistory.Span[^1].Year - sortedHistory.Span[0].Year + 1)
                ? throw new FatalWarning($"Invalid historical ROI data | Either it is empty or counts do not agree.")
                : sortedHistory;
        }
    }
}
