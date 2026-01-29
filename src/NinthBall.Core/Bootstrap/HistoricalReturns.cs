
using System.Globalization;
using NinthBall.Utils;

namespace NinthBall.Core
{
    // Historical returns:
    // REF: https://pages.stern.nyu.edu/~adamodar/New_Home_Page/datafile/histretSP.html?utm_source=chatgpt.com

    /// <summary>
    /// Represents historical stocks and bonds ROI imported from Excel file.
    /// </summary>
    internal sealed class HistoricalReturns
    {
        private static readonly Lazy<(ReadOnlyMemory<HROI> Data, int MinYear, int MaxYear)> LazyHistory = new(ReadHistoryOnce);

        public ReadOnlyMemory<HROI> History => LazyHistory.Value.Data;
        public int MinYear => LazyHistory.Value.MinYear;
        public int MaxYear => LazyHistory.Value.MaxYear;

        static (ReadOnlyMemory<HROI> history, int minYear, int maxYear) ReadHistoryOnce()
        {
            const string ResNameEndsWith = "ROI-History.xlsx";
            const string SheetName = "DATA";

            // Look for exactly one ROI-History.xlsx embedded resource.
            // Open resource stream
            var resAssembly = typeof(HistoricalReturns).Assembly;
            var resName = resAssembly.GetManifestResourceNames().Single(x => x.EndsWith(ResNameEndsWith, StringComparison.OrdinalIgnoreCase));
            using var resStream = resAssembly.GetManifestResourceStream(resName) ?? throw new Exception("Unexpected: ManifestResourceStream was null.");

            var history = new List<HROI>(200);
            var minYear = int.MaxValue;
            var maxYear = int.MinValue;

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
                        if (year < minYear) minYear = year;
                        if (year > maxYear) maxYear = year;
                    }
                }
            }

            // Check and inform...
            if (0 == history.Count || history.Count != maxYear - minYear + 1) throw new FatalWarning($"Invalid historical ROI data. Check for data integrity.");
            Console.WriteLine($" Read {history.Count} years of historical ROI data from {minYear} to {maxYear}.");

            // Repeatable (sort by year) and read-only (to memory)
            var sortedReadonlyHistory = history.OrderBy(x => x.Year).ToArray().AsMemory();

            return (sortedReadonlyHistory, minYear, maxYear);
        }
    }
}
