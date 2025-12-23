
namespace NinthBall.Core
{
    // Historical returns:
    // Credits: https://pages.stern.nyu.edu/~adamodar/New_Home_Page/datafile/histretSP.html?utm_source=chatgpt.com

    /// <summary>
    /// Represents stocks and bonds ROI on a specific year.
    /// </summary>
    internal readonly record struct HROI(int Year, double StocksROI, double BondROI);

    /// <summary>
    /// Represents historical stocks and bonds ROI imported from Excel file.
    /// </summary>
    internal sealed class HistoricalReturns(ROIHistory options)
    {
        public IReadOnlyList<HROI> History => field ??= ReadHistoryOnce();

        IReadOnlyList<HROI> ReadHistoryOnce()
        {
            var xlFileName = options.XLFileName  ?? throw new ArgumentNullException(nameof(options.XLFileName));
            var sheetName  = options.XLSheetName ?? throw new ArgumentNullException(nameof(options.XLSheetName));

            Console.WriteLine($" Reading ROI-history from {Path.GetFileName(xlFileName)}[{sheetName}]");

            List<HROI> history = [];

            using (var xlReader = new ExcelReader(xlFileName))
            {
                var sheet = xlReader.GetSheets().Where(s => sheetName.Equals(s.SheetName, StringComparison.OrdinalIgnoreCase)).SingleOrDefault()
                    ?? throw new Exception($"Sheet not found | File: {Path.GetFileName(xlFileName)} | Sheet: '{sheetName}'");

                foreach (var row in sheet.GetRows())
                {
                    if (null == row) continue;

                    // Skip first (header) row.  Do not use IEnumerable.Skip(1) option; Use Rowindex.
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
