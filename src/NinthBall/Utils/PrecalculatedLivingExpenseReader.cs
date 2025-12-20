
namespace NinthBall
{
    internal static class PrecalculatedLivingExpenseReader
    {
        public static IReadOnlyList<double> ReadPrecalculatedLivingExpenses(string xlFileName, string sheetName)
        {
            var sequence = new List<double>();

            using (var xlReader = new ExcelReader(xlFileName))
            {
                var sheet = xlReader.GetSheets().Where(s => sheetName.Equals(s.SheetName, StringComparison.OrdinalIgnoreCase)).SingleOrDefault()
                    ?? throw new FatalWarning($"Sheet not found | File: {Path.GetFileName(xlFileName)} | Sheet: '{sheetName}'");

                foreach (var row in sheet.GetRows())
                {
                    if (null == row) continue;

                    // Skip first (header) row.  Do not use IEnumerable.Skip(1) option; Use Rowindex.
                    var isFirstRow = null != row.RowIndex && 1 == row.RowIndex.Value;
                    if (isFirstRow) continue;

                    var cells = row.GetCellValues().ToArray();

                    if (
                        null != cells
                        && cells.Length >= 1
                        && double.TryParse(cells[0], out var amount)
                    )
                    {
                        sequence.Add(amount);
                    }
                }
            }

            return sequence.AsReadOnly();
        }
    }
}
