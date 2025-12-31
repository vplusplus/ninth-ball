using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using System.Globalization;
using System.Text.RegularExpressions;

namespace NinthBall.Core
{
    /// <summary>
    /// SAX based forward-only Workbook writer with low memory usage and negligible stack allocation.
    /// DOESN'T support SharedStringTable. All strings are written inline.
    /// </summary>
    public sealed class ExcelWriter : IDisposable
    {
        static readonly Regex RxInvalidSheetName = new(@"[\\\\[\\]\\*\\?:\\/]", RegexOptions.Compiled);

        readonly string FilePath;
        readonly string TempFilePath;

        readonly SpreadsheetDocument Document;
        readonly WorkbookPart WorkbookPart;
        readonly Stylesheet Stylesheet;

        public ExcelWriter(string outputFilePath, Stylesheet stylesheet)
        {
            ArgumentNullException.ThrowIfNull(outputFilePath);
            ArgumentNullException.ThrowIfNull(stylesheet);

            FilePath = outputFilePath;
            Stylesheet = stylesheet;

            // Content written to a temp file, deleted on Save() or Dispose()
            TempFilePath = Path.GetTempFileName();

            // Create the SpreadsheetDocument and core structure
            Document = SpreadsheetDocument.Create(TempFilePath, SpreadsheetDocumentType.Workbook);
            WorkbookPart = Document.AddWorkbookPart();
            WorkbookPart.Workbook = new Workbook();
            WorkbookPart.Workbook.AppendChild(new Sheets());
        }

        public WorksheetWriter BeginSheet(string sheetName)
        {
            ThrowIfDisposed();

            sheetName = ThrowIfInvalidSheetName(sheetName);

            // Create WorksheetPart and xml-Writer for the WorksheetPart
            var worksheetPart = WorkbookPart.AddNewPart<WorksheetPart>();
            var sheetXmlWriter = OpenXmlWriter.Create(worksheetPart);

            // Link Sheet to the Workbook
            var sheets = WorkbookPart.Workbook.GetFirstChild<Sheets>() ?? throw new Exception("Unexpected. Sheets was null.");
            var nextSheetId = 1 + sheets.Elements<Sheet>().Max(x => x?.SheetId?.Value).GetValueOrDefault(0);
            sheets.Append(new Sheet()
            {
                Id = WorkbookPart.GetIdOfPart(worksheetPart),
                SheetId = nextSheetId,
                Name = sheetName
            });

            // Provide a writer chain. 
            return new WorksheetWriter(sheetXmlWriter).BeginWorksheet();

            static string ThrowIfInvalidSheetName(string sheetName)
            {
                if (string.IsNullOrWhiteSpace(sheetName)) throw new FatalWarning("Invalid Excel sheet name | Was NULL or Empty");

                sheetName = sheetName.Trim('\'');
                if (sheetName.Length > 31) throw new FatalWarning("Invalid Excel sheet name | Must be <= 31 chars.");
                if ("History".Equals(sheetName, StringComparison.OrdinalIgnoreCase)) throw new FatalWarning("Invalid Excel sheet name | 'History' is a reerve name.");
                if (RxInvalidSheetName.IsMatch(sheetName)) throw new FatalWarning("Invalid Excel sheet name | Has invalid chars.");

                return sheetName;
            }
        }

        public void Save()
        {
            ThrowIfDisposed();

            // Flush WorkbookPart content.
            WorkbookPart.Workbook.Save();

            // Write Styles at the end.
            UpdateCounts(Stylesheet);
            WorkbookPart.AddNewPart<WorkbookStylesPart>().Stylesheet = Stylesheet;
            Stylesheet.Save();

            // Save and close XML Document (flushes to disk)
            Document.Save();
            Document.Dispose();

            // Ensure target directory.
            var dir = Path.GetDirectoryName(FilePath) ?? "./";
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            // Move the temp file content to the target.
            // This should also delete the temp file.
            File.Move(TempFilePath, FilePath, overwrite: true);
            return;

            // Update the Count property of the container nodes if present.
            static void UpdateCounts(Stylesheet stylesheet)
            {
                ArgumentNullException.ThrowIfNull(stylesheet);

                stylesheet.Fonts?.Count             = (uint?)stylesheet.Fonts.ChildElements.Count;
                stylesheet.Fills?.Count             = (uint?)stylesheet.Fills.ChildElements.Count;
                stylesheet.Borders?.Count           = (uint?)stylesheet.Borders.ChildElements.Count;
                stylesheet.CellStyleFormats?.Count  = (uint?)stylesheet.CellStyleFormats.ChildElements.Count;
                stylesheet.CellFormats?.Count       = (uint?)stylesheet.CellFormats.ChildElements.Count;
                stylesheet.NumberingFormats?.Count  = (uint?)stylesheet.NumberingFormats.ChildElements.Count;
            }
        }

        //.............................................................
        #region Tiered fluent APIs for SAX-writing Workbook and its sub-components
        //.............................................................
        public readonly ref struct WorksheetWriter(OpenXmlWriter Writer)
        {
            readonly Worksheet OneWorksheet = new();
            readonly Columns OneColumns = new();
            readonly Column OneColumn = new();

            readonly SheetDataWriter OneSheetDataWriter = new(Writer);

            internal WorksheetWriter BeginWorksheet()
            {
                Writer.WriteStartElement(OneWorksheet);
                return this;
            }

            public readonly void EndWorksheet()
            {
                Writer.WriteEndElement();   // End worksheet
                Writer.Dispose();           // IMPORTANT: Writer is no longer valid
            }

            /// <summary>
            /// One-shot helper to set optional column widths.
            /// Set column widths once and before calling BeginSheetData().
            /// </summary>
            public WorksheetWriter WriteColumns(params ReadOnlySpan<double?> columnWidths)
            {
                // <cols>
                Writer.WriteStartElement(OneColumns);

                for (int i = 0; i < columnWidths.Length; i++)
                {
                    var colIndex = (uint)(i + 1);
                    var colWidth = columnWidths[i];

                    // Null implies, do not explicitly set width for this column.
                    if (colWidth.HasValue)
                    {
                        // <col />
                        OneColumn.Min = OneColumn.Max = colIndex;
                        OneColumn.Width = colWidth;
                        OneColumn.CustomWidth = true;
                        Writer.WriteElement(OneColumn);
                    }
                }

                // </cols>
                Writer.WriteEndElement();

                return this;
            }

            /// <summary>
            /// Start writing rows and cells.
            /// Dispose me to close the SheetData.
            /// </summary>
            public SheetDataWriter BeginSheetData() => OneSheetDataWriter.BeginSheetData();

            public readonly void Dispose() => EndWorksheet();
        }

        public readonly ref struct SheetDataWriter(OpenXmlWriter Writer)
        {
            readonly SheetData OneSheetData = new();
            readonly RowWriter OneRowWriter = new(Writer);

            // <sheetData>
            internal SheetDataWriter BeginSheetData()                     
            {
                Writer.WriteStartElement(OneSheetData);
                return this;
            }

            // </sheetData>
            public void EndSheetData() => Writer.WriteEndElement(); 

            public readonly RowWriter BeginRow() => OneRowWriter.BeginRow();

            public void Dispose() => EndSheetData();
        }

        public readonly ref struct RowWriter(OpenXmlWriter Writer)
        {
            readonly Row OneRow = new();

            readonly InlineStringCell OneStringCell = new();
            readonly NumberCell OneNumberCell = new();
            readonly BooleanCell OneBooleanCell = new();
            readonly DateCell OneDateCell = new();

            internal RowWriter BeginRow()
            {
                Writer.WriteStartElement(OneRow);
                return this;
            }

            public readonly void EndRow() => Writer.WriteEndElement();

            public readonly RowWriter Append(bool value, uint? styleIndex = null)
            {
                OneBooleanCell.WriteCell(Writer, value, styleIndex);
                return this;
            }

            public readonly RowWriter Append(int value, uint? styleIndex = null)
            {
                OneNumberCell.WriteCell(Writer, value, styleIndex);
                return this;
            }

            public readonly RowWriter Append(double value, uint? styleIndex = null)
            {
                OneNumberCell.WriteCell(Writer, value, styleIndex);
                return this;
            }

            public readonly RowWriter Append(DateTime value, uint styleIndex)   // Must provide style for date
            {
                OneDateCell.WriteCell(Writer, value, styleIndex);
                return this;
            }

            public readonly RowWriter Append(string value, uint? styleIndex = null)
            {
                OneStringCell.WriteCell(Writer, value, styleIndex);
                return this;
            }

            public readonly void Dispose() => EndRow();
        }

        #endregion

        //......................................................................
        #region Dispose() and dtor()
        //......................................................................
        int disposing = 0;

        bool ThrowIfDisposed() => 0 == disposing ? true : throw new ObjectDisposedException(nameof(ExcelWriter));

        void DisposeOnce()
        {
            // If already disposed...
            if (Interlocked.Exchange(ref disposing, 1) > 0) return;

            // Consumer is responsible for saving before disposing the instance.
            // We are responsible only for deleting the temp file.
            // Dispose the SpreadsheetDocument since we will delete the temp file. 
            // If document is saved, below line will throw, which is ok.
            try { Document.Dispose(); } catch { }

            // Delete the temp file if still exists
            try { if (File.Exists(TempFilePath)) File.Delete(TempFilePath); } catch { }
        }

        void IDisposable.Dispose()
        {
            DisposeOnce();
            GC.SuppressFinalize(this);
        }

        ~ExcelWriter() => DisposeOnce();

        #endregion

        //......................................................................
        #region Cell templates
        //......................................................................
        sealed class InlineStringCell
        {
            readonly Cell Cell;
            readonly Text CellText;

            public InlineStringCell()
            {
                CellText = new();
                Cell = new Cell(new InlineString(CellText)) { DataType = CellValues.InlineString };
            }

            public void WriteCell(OpenXmlWriter writer, string text, uint? styleIndex)
            {
                CellText.Text = text ?? "";
                Cell.StyleIndex = styleIndex;
                writer.WriteElement(Cell);
            }
        }

        sealed class NumberCell
        {
            readonly Cell Cell;
            readonly CellValue CellValue;

            public NumberCell()
            {
                CellValue = new();
                Cell = new Cell(CellValue) { DataType = CellValues.Number };
            }

            public void WriteCell(OpenXmlWriter writer, int value, uint? styleIndex)
            {
                CellValue.Text = value.ToString(CultureInfo.InvariantCulture);
                Cell.StyleIndex = styleIndex;
                writer.WriteElement(Cell);
            }

            public void WriteCell(OpenXmlWriter writer, double value, uint? styleIndex)
            {
                CellValue.Text = value.ToString(CultureInfo.InvariantCulture);
                Cell.StyleIndex = styleIndex;
                writer.WriteElement(Cell);
            }
        }

        sealed class BooleanCell
        {
            readonly Cell Cell;
            readonly CellValue CellValue;

            public BooleanCell()
            {
                CellValue = new();
                Cell = new Cell(CellValue) { DataType = CellValues.Boolean };
            }

            public void WriteCell(OpenXmlWriter writer, bool value, uint? styleIndex)
            {
                CellValue.Text = value ? "1" : "0";
                Cell.StyleIndex = styleIndex;
                writer.WriteElement(Cell);
            }
        }

        sealed class DateCell
        {
            readonly Cell Cell;
            readonly CellValue CellValue;

            public DateCell()
            {
                CellValue = new();
                Cell = new Cell(CellValue) { DataType = CellValues.Number };
            }

            public void WriteCell(OpenXmlWriter writer, DateTime value, uint styleIndex)
            {
                // Date values are renered as OADate (numeric) - Must provide a styleIndex.
                CellValue.Text = value.ToOADate().ToString(CultureInfo.InvariantCulture);
                Cell.StyleIndex = styleIndex;
                writer.WriteElement(Cell);
            }
        }

        #endregion

    }
}

