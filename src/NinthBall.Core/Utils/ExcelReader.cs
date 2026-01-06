﻿
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace NinthBall.Core
{
    // Can read the Worksheets (tabs), rows and cell values from Excel document.
    internal sealed class ExcelReader : IDisposable
    {
        readonly Stream MyFileStream;
        readonly SpreadsheetDocument MyDocument;
        readonly CellReader MyCellReader;

        public ExcelReader(Stream excelFileStream)
        {
            // Open readonly. Allow other processes (Excel) to read or write.
            MyFileStream = excelFileStream ?? throw new ArgumentNullException(nameof(excelFileStream));
            MyDocument = SpreadsheetDocument.Open(MyFileStream, isEditable: false);

            // We need one and only one instance of CellReader.
            MyCellReader = new(this);
        }

        //public ExcelReader(string excelFileName)
        //{
        //    // Open readonly. Allow other processes (Excel) to read or write.
        //    MyFileStream = File.Open(excelFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        //    MyDocument = SpreadsheetDocument.Open(MyFileStream, isEditable: false);

        //    // We need one and only one instance of CellReader.
        //    MyCellReader = new(this);
        //}

        // Returns a sequence of SheetReader, one per worksheet.
        public IEnumerable<SheetReader> GetSheets() => MySheets.Select(sheet => new SheetReader(this, sheet));

        public sealed class SheetReader(ExcelReader MyParent, Sheet MySheet)
        {
            // The sheet name or empty string
            public string SheetName => MySheet.Name?.Value ?? string.Empty;

            // Indicates if the sheet is hidden.
            public bool IsHidden => MySheet.State?.Value == SheetStateValues.Hidden || MySheet.State?.Value == SheetStateValues.VeryHidden;

            // Returns a sequence of RowReader, one per row.
            public IEnumerable<RowReader> GetRows() => MyParent.GetSheetData(MySheet).Elements<Row>().Where(x => null != x).Select(x => new RowReader(MyParent, x));
        }

        public sealed class RowReader(ExcelReader MyParent, Row MyRow)
        {
            // RowIndex, can be null.
            public uint? RowIndex => MyRow.RowIndex?.Value;

            // Returns values of all cells in this row.
            // Retain the order; XL may skip empty cells. Use string.Empty for missing cells.
            public IEnumerable<string> GetCellValues()
            {
                int expectedColIndex = 0;

                foreach (Cell cell in MyRow.Elements<Cell>())
                {
                    if (null == cell) continue;

                    var colIndex = GetColumnIndex(cell);
                    var cellValue = MyParent.MyCellReader.ReadCellValue(cell);

                    while (colIndex.HasValue && colIndex > expectedColIndex)
                    {
                        yield return string.Empty;
                        expectedColIndex++;
                    }

                    yield return cellValue;
                    expectedColIndex++;
                }
            }
        }

        // Can read Inline-String, Shared-String, Numeric, Boolean and Date values.
        // Date is inferred using FormatId (if present) from the Stylesheet.
        // Doesn't support macros or formulas.
        private sealed class CellReader(ExcelReader parent)
        {
            // ReadFromYamlFile and cache NumberFormatIds and SharedStrings once. PRESERVE the order.
            readonly uint[] MyNumberFormatIds = parent.MyCellFormats.Select(x => x?.NumberFormatId?.Value ?? 0).ToArray();
            readonly string[] MySharedStrings = parent.MySharedStringTable.Elements<SharedStringItem>().Select(x => x?.Text?.Text ?? x?.InnerText ?? string.Empty).ToArray();

            public string ReadCellValue(Cell cell)
            {
                ArgumentNullException.ThrowIfNull(cell);

                var dataType = cell.DataType?.Value;
                var styleIndex = cell.StyleIndex?.Value ?? uint.MinValue;
                var cellText = cell.CellValue?.Text ?? cell.InnerText ?? string.Empty;

                if (cellText.Length == 0)
                {
                    return string.Empty;
                }
                else if (dataType == CellValues.Boolean)
                {
                    return "0" == cellText ? "FALSE" : "1" == cellText ? "TRUE" : cellText;
                }
                else if (dataType == CellValues.SharedString)
                {
                    return int.TryParse(cellText, out var sstIndex) && sstIndex >= 0 && sstIndex < MySharedStrings.Length
                        ? MySharedStrings[sstIndex] ?? string.Empty
                        : cellText;
                }
                else if (dataType is null)
                {
                    return styleIndex >= 0 && styleIndex < MyNumberFormatIds.Length
                        && MyNumberFormatIds[styleIndex] >= 14 && MyNumberFormatIds[styleIndex] <= 22
                        && double.TryParse(cellText, out var doubleDate)
                            ? DateTime.FromOADate(doubleDate).ToString("O")
                            : cellText;
                }
                else
                {
                    return cellText;
                }
            }
        }

        int disposing = 0;

        void DisposeOnce()
        {
            if (0 == Interlocked.Exchange(ref disposing, 1))
            {
                try { MyDocument.Dispose(); } catch { }
                try { MyFileStream.Dispose(); } catch { }
            }
        }

        void IDisposable.Dispose()
        {
            DisposeOnce();
            GC.SuppressFinalize(this);
        }

        ~ExcelReader()
        {
            DisposeOnce();
        }

        //......................................................................
        // Boilerplate  utils for navigating OpenXML components
        //......................................................................
        Stylesheet MyStylesheet => MyDocument.WorkbookPart?.WorkbookStylesPart?.Stylesheet ?? throw new Exception("Stylesheet was null.");

        SharedStringTable MySharedStringTable => MyDocument.WorkbookPart?.SharedStringTablePart?.SharedStringTable ?? throw new Exception("SharedStringTable was null.");

        IEnumerable<CellFormat> MyCellFormats => MyStylesheet.CellFormats?.OfType<CellFormat>() ?? throw new Exception("CellFormats was null.");

        IEnumerable<Sheet> MySheets => MyDocument.WorkbookPart?.Workbook?.Sheets?.OfType<Sheet>() ?? throw new Exception("Sheets was null.");

        SheetData GetSheetData(Sheet someSheet)
        {
            ArgumentNullException.ThrowIfNull(someSheet);

            var id = someSheet.Id?.Value ?? throw new Exception("Sheet.Id was null");
            var worksheetPart = MyDocument.WorkbookPart?.GetPartById(id) as WorksheetPart ?? throw new Exception($"WorksheetPart not found | Id: {id}");
            var worksheet = worksheetPart.Worksheet ?? throw new Exception($"Worksheet was null | Id: {id}");
            return worksheet.GetFirstChild<SheetData>() ?? throw new Exception($"Worksheet missing SheetData | | Id: {id}");
        }

        static int? GetColumnIndex(Cell cell)
        {
            string? cellReference = cell?.CellReference;
            if (string.IsNullOrEmpty(cellReference)) return null;

            int index = 0;
            foreach (char c in cellReference)
            {
                if (!char.IsLetter(c)) break;
                index = (index * 26) + (c - 'A' + 1);
            }
            return index - 1; // zero-based
        }
    }
}