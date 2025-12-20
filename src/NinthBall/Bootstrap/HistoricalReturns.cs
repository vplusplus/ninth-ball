
namespace NinthBall
{
    // Historical returns:
    // REF: https://pages.stern.nyu.edu/~adamodar/New_Home_Page/datafile/histretSP.html?utm_source=chatgpt.com

    public record struct HROI(int Year, double StocksROI, double BondROI); 

    internal sealed class HistoricalReturns(ROIHistory options)
    {
        public IReadOnlyList<HROI> AllYears => field ??= ReadHistoryOnce();

        IReadOnlyList<HROI> ReadHistoryOnce()
        {
            var xlFileName = options.XLFileName ?? throw new ArgumentException(nameof(options.XLFileName));
            var sheetName  = options.XLSheetName?? throw new ArgumentException(nameof(options.XLSheetName));

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
