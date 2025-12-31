
//using DocumentFormat.OpenXml;
//using DocumentFormat.OpenXml.Packaging;
//using DocumentFormat.OpenXml.Spreadsheet;

//namespace NinthBall.Core
//{
//    /// <summary>
//    /// Creates Excel workbook with single worksheet and auto-filter.
//    /// Supports text, integer, decimal and date cells with pre-defined formats.
//    /// Uses in-memory DOM model, not suitable for very large worksheets.
//    /// Not thread-safe.
//    /// </summary>
//    internal sealed class ExcelWriter : IDisposable
//    {
//        readonly SpreadsheetDocument MyDocument;
//        readonly SharedStrings MySharedStrings;
//        readonly CellFormatIds MyCellFormats;
//        readonly Worksheet MyWorksheet;
//        readonly SheetData MySheetData;

//        public ExcelWriter(string xlsxFileName, string sheetName, int[] columnWidths)
//        {
//            ArgumentNullException.ThrowIfNull(xlsxFileName);
//            ArgumentNullException.ThrowIfNull(sheetName);
//            ArgumentNullException.ThrowIfNull(columnWidths);

//            MyDocument = SpreadsheetDocument.Create(xlsxFileName, SpreadsheetDocumentType.Workbook);

//            var workbookPart = MyDocument.AddWorkbookPart();
//            workbookPart.Workbook = new Workbook();

//            var workbookStylesPart = workbookPart.AddNewPart<WorkbookStylesPart>();
//            workbookStylesPart.Stylesheet = CreateDefaultStyles(out MyCellFormats);

//            var sharedStringTablePart = workbookPart.AddNewPart<SharedStringTablePart>();
//            sharedStringTablePart.SharedStringTable = CreateSharedStringCache(out MySharedStrings);

//            var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
//            worksheetPart.Worksheet = MyWorksheet = CreateOneWorksheetWithAutoFilter(columnWidths, out MySheetData);

//            // Associate the workbook and worksheet.
//            // SheetId is hard coded to 1, which is ok. This is the one-and-only-one worksheet supported.
//            var sheets = workbookPart.Workbook.AppendChild(new Sheets());
//            sheets.Append(new Sheet()
//            {
//                Id = workbookPart.GetIdOfPart(worksheetPart),
//                SheetId = 1,
//                Name = sheetName,
//            });
//        }

//        public RowBuilder NewRow()
//        {
//            ThrowIfDisposed();
//            return new RowBuilder(this);
//        }

//        public void Close()
//        {
//            FinalizeOnce();
//        }

//        record CellFormatIds(uint CellFormatIdInteger, uint CellFormatIdDecimal, uint CellFormatIdDate);

//        // Create a Stylesheet with default choice for the Integer, Decimal and Date formats.
//        // Return the index of the respective CellFormat and the Stylesheeet.
//        static Stylesheet CreateDefaultStyles(out CellFormatIds styleIds)
//        {
//            // The undocumented, but well-known built-in number formats
//            const uint XLNumberFormatIdInteger = 3;     // #,##0
//            const uint XLNumberFormatIdDecimal = 4;     // #,##0.00
//            const uint XLNumberFormatIdDate = 14;    // mm-dd-yy

//            // Pre-determined cell format ids for numeric, decimal and date columns.
//            CellFormat defaultFormat = new();
//            CellFormat integerFormat = new() { NumberFormatId = XLNumberFormatIdInteger, ApplyNumberFormat = true };
//            CellFormat decimalFormat = new() { NumberFormatId = XLNumberFormatIdDecimal, ApplyNumberFormat = true };
//            CellFormat dateFormat = new() { NumberFormatId = XLNumberFormatIdDate, ApplyNumberFormat = true };
//            CellFormats cellFormats = new(defaultFormat, integerFormat, decimalFormat, dateFormat);

//            // Capture the index of respective cell format ids.
//            var byIndex = cellFormats.ToList();
//            styleIds = new(
//                CellFormatIdInteger: (uint)byIndex.IndexOf(integerFormat),
//                CellFormatIdDecimal: (uint)byIndex.IndexOf(decimalFormat),
//                CellFormatIdDate: (uint)byIndex.IndexOf(dateFormat)
//            );

//            return new()
//            {
//                Fonts = new Fonts(new Font()),
//                Fills = new Fills(new Fill()),
//                Borders = new Borders(new Border()),
//                CellStyleFormats = new CellStyleFormats(new CellFormat()),
//                CellFormats = cellFormats
//            };
//        }

//        static SharedStringTable CreateSharedStringCache(out SharedStrings sharedStrings)
//        {
//            sharedStrings = new();
//            return sharedStrings.SharedStringTable;
//        }

//        static Worksheet CreateOneWorksheetWithAutoFilter(int[] columnWidths, out SheetData sheetData)
//        {
//            sheetData = new SheetData();
//            var worksheet = new Worksheet(sheetData);

//            if (null != columnWidths && columnWidths.Length > 0)
//            {
//                // Columns goes first before SheetData
//                var columns = CreateColumnsWithSuggestedWidths(columnWidths);
//                worksheet.InsertAt(CreateColumnsWithSuggestedWidths(columnWidths), 0);

//                // AUtoFilter goes last.
//                var autoFilter = CreateAutoFilter(columnWidths.Length);
//                worksheet.Append(autoFilter);
//            }

//            return worksheet;
//        }

//        static Columns CreateColumnsWithSuggestedWidths(int[] columnWidths)
//        {
//            Columns columns = new();
//            uint colIndex = 1;

//            for (int i = 0; i < columnWidths.Length; i++)
//            {
//                columns.Append(new Column() { Min = colIndex, Max = colIndex, Width = Math.Max(columnWidths[i], 0), CustomWidth = true });
//                colIndex++;
//            }

//            return columns;
//        }

//        static AutoFilter CreateAutoFilter(int colCount)
//        {
//            string lastColumnName = colCount <= 0 ? "B" : GetColumnName(colCount);
//            return new AutoFilter() { Reference = $"A1:{lastColumnName}1" };
//        }

//        static void UpdateAutoFilter(Worksheet? worksheet)
//        {
//            // Find the auto filter
//            var autoFilter = worksheet?.GetFirstChild<AutoFilter>();
//            if (null == autoFilter) return;

//            // Discover no of columns and rows.
//            int colCount = worksheet?.GetFirstChild<Columns>()?.Count() ?? 0;
//            int rowCount = worksheet?.GetFirstChild<SheetData>()?.Elements<Row>()?.Count() ?? 0;
//            if (0 == colCount || 0 == rowCount) return;

//            // Update AutoFilter range
//            autoFilter.Reference = $"A1:{GetColumnName(colCount)}{rowCount}";
//        }

//        public static string GetColumnName(int columnIndex)
//        {
//            if (columnIndex <= 0) throw new ArgumentException("columnIndex must be 1 or above");

//            string name = "";
//            while (columnIndex > 0)
//            {
//                columnIndex--;
//                name = (char)('A' + (columnIndex % 26)) + name;
//                columnIndex /= 26;
//            }
//            return name;
//        }

//        int disposing = 0;

//        void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(disposing > 0, typeof(ExcelWriter));

//        void FinalizeOnce()
//        {
//            if (0 == Interlocked.Exchange(ref disposing, 1))
//            {
//                UpdateAutoFilter(MyWorksheet);
//                MyDocument.Save();
//                MyDocument.Dispose();
//            }
//        }

//        void IDisposable.Dispose()
//        {
//            FinalizeOnce();
//            GC.SuppressFinalize(this);
//        }

//        ~ExcelWriter()
//        {
//            FinalizeOnce();
//        }

//        /// <summary>
//        /// Fluent API to append cells to current row.
//        /// Commits the row when dispposed.
//        /// </summary>
//        public sealed class RowBuilder(ExcelWriter parent) : IDisposable
//        {
//            readonly ExcelWriter Parent = parent ?? throw new ArgumentNullException(nameof(parent));
//            readonly Row MyRow = new();

//            public RowBuilder Append(bool value)
//            {
//                ThrowIfDisposed();
//                MyRow.Append(new Cell { DataType = CellValues.Boolean, CellValue = new CellValue(value ? 1 : 0) });
//                return this;
//            }

//            public RowBuilder Append(int value)
//            {
//                ThrowIfDisposed();
//                MyRow.Append(new Cell { StyleIndex = Parent.MyCellFormats.CellFormatIdInteger, CellValue = new CellValue(value) });
//                return this;
//            }

//            public RowBuilder Append(double value)
//            {
//                ThrowIfDisposed();
//                MyRow.Append(new Cell { StyleIndex = Parent.MyCellFormats.CellFormatIdDecimal, CellValue = new CellValue(value) });
//                return this;
//            }

//            public RowBuilder Append(DateTime value)
//            {
//                ThrowIfDisposed();
//                MyRow.Append(new Cell { StyleIndex = Parent.MyCellFormats.CellFormatIdDate, CellValue = new CellValue(value.ToOADate()) });
//                return this;
//            }

//            public RowBuilder Append(string value)
//            {
//                ThrowIfDisposed();
//                MyRow.Append(new Cell { DataType = CellValues.SharedString, CellValue = new CellValue(Parent.MySharedStrings.GetOrAdd(value)) });
//                return this;
//            }

//            public RowBuilder Append(IEnumerable<string> values)
//            {
//                ThrowIfDisposed();

//                foreach (string value in values) Append(value);
//                return this;
//            }


//            public void EndRow()
//            {
//                FinalizeOnce();
//            }

//            int disposing = 0;

//            void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(disposing > 0, typeof(RowBuilder));

//            void FinalizeOnce()
//            {
//                if (0 == Interlocked.Exchange(ref disposing, 1))
//                {
//                    Parent.MySheetData.Append(MyRow);
//                }
//            }

//            void IDisposable.Dispose()
//            {
//                FinalizeOnce();
//                GC.SuppressFinalize(this);
//            }

//            ~RowBuilder()
//            {
//                FinalizeOnce();
//            }
//        }

//        /// <summary>
//        /// OpenXML SharedStringTable with fast lookup of existing indexes. 
//        /// </summary>
//        sealed class SharedStrings
//        {
//            public readonly SharedStringTable SharedStringTable = new();
//            private readonly Dictionary<string, int> SharedStringTableIndex = [];

//            public SharedStrings() { }

//            public int GetOrAdd(string something)
//            {
//                something ??= string.Empty;

//                if (SharedStringTableIndex.TryGetValue(something, out var index)) return index;

//                // Below lines are not thread-safe; OpenXML is not thread-safe. 
//                // Thread safety is a not a requirement for this utility.
//                SharedStringTable.AppendChild(new SharedStringItem(new Text(something)));
//                return SharedStringTableIndex[something] = SharedStringTable.Count() - 1;
//            }
//        }
//    }
//}