
using DocumentFormat.OpenXml.Spreadsheet;

namespace NinthBall.Core
{
    /// <summary>
    /// Style-indices of default cell formats.
    /// </summary>
    public readonly record struct StyleIDs
    (
        uint Date,
        uint Int,
        uint F0, uint F1, uint F2,
        uint C0, uint C1, uint C2,
        uint P0, uint P1, uint P2
    );

    /// <summary>
    /// Utilities for creating and managing Excel Stylesheet with CellFormats.
    /// </summary>
    public static class ExcelStylesheet
    {
        // Custom numbering formats starts from 143 or above.
        const uint FirstCustomNumberFormatId = 143;

        /// <summary>
        /// Provides a 'nothing' Stylesheet with its container nodes pre-created.
        /// </summary>
        public static Stylesheet Empty() => new()
        {
            Fonts = new Fonts(new Font()),
            Fills = new Fills(new Fill()),
            Borders = new Borders(new Border()),
            CellStyleFormats = new CellStyleFormats(new CellFormat() { FormatId = 0, FontId = 0, BorderId = 0, FillId = 0 }),
            CellFormats = new CellFormats(new CellFormat() { FormatId = 0, FontId = 0, BorderId = 0, FillId = 0 }),
            NumberingFormats = new NumberingFormats()
        };

        /// <summary>
        /// Registers opinionated default cell formats.
        /// Provides the style-indices of the cell formats.
        /// </summary>
        public static Stylesheet WithDefaultStyles(this Stylesheet stylesheet, out StyleIDs IDs)
        {
            ArgumentNullException.ThrowIfNull(stylesheet);

            IDs = new
            (
                Int: stylesheet.GetOrAddCellFormat("#,##0"),
                Date: stylesheet.GetOrAddCellFormat("dd-mmm-yyyy"),

                F0: stylesheet.GetOrAddCellFormat("#,##0"),
                F1: stylesheet.GetOrAddCellFormat("#,##0.0"),
                F2: stylesheet.GetOrAddCellFormat("#,##0.00"),

                C0: stylesheet.GetOrAddCellFormat("$#,##0"),
                C1: stylesheet.GetOrAddCellFormat("$#,##0.0"),
                C2: stylesheet.GetOrAddCellFormat("$#,##0.00"),

                P0: stylesheet.GetOrAddCellFormat("#0%"),
                P1: stylesheet.GetOrAddCellFormat("#0.0%"),
                P2: stylesheet.GetOrAddCellFormat("#0.00%")
            );

            return stylesheet;
        }

        /// <summary>
        /// Registers a cell format with given formatCode if not already registered.
        /// Returns index of existing or newly added CellFormat.
        /// </summary>
        public static uint GetOrAddCellFormat(this Stylesheet stylesheet, string formatCode)
        {
            ArgumentNullException.ThrowIfNull(stylesheet);
            ArgumentNullException.ThrowIfNull(formatCode);

            // Find existing CellFormat with given numbering format.
            var cellFormatIndex = stylesheet.TryGetCellFormatIndex(formatCode);
            if (cellFormatIndex.HasValue) return cellFormatIndex.Value;

            // Create a new CellFormat
            CellFormat newCellFormat = new()
            {
                NumberFormatId = stylesheet.GetOrAddNumberingFormat(formatCode),
                ApplyNumberFormat = true
            };

            // Ensure the stylesheet has CellFormats container.
            // And, register a new Cell format with the stylesheet
            CellFormats cellFormats = stylesheet.CellFormats ??= new();
            cellFormats.AppendChild(newCellFormat);

            // Return the index of the cell format
            return (uint)(cellFormats.ChildElements.Count - 1);
        }

        /// <summary>
        /// Returns the index of CellFormat with given numbering format.
        /// All other attribute (such as fonts, borders, etc.) are ignored.
        /// </summary>
        private static uint? TryGetCellFormatIndex(this Stylesheet stylesheet, string formatCode)
        {
            ArgumentNullException.ThrowIfNull(stylesheet);
            ArgumentNullException.ThrowIfNull(formatCode);

            // Find the number format id of the formatCode.
            // If number format not registered, there can't be a CellFormat either.
            uint? numberFormatId = stylesheet.TryGetNumberFormatId(formatCode);
            if (null == numberFormatId) return null;

            // If there is no CellFormats container...
            if (null == stylesheet.CellFormats) return null;

            // Look for the first CellFormat with given numberFormatId.
            uint index = 0;
            foreach (var cellFormat in stylesheet.CellFormats.Elements<CellFormat>())
            {
                // Cell formats we manage sets only NumberFormatId.
                // Look for CellFormat with suggested numberFormatId, with no other styling.
                var found = cellFormat.NumberFormatId != null && cellFormat.NumberFormatId.Value == numberFormatId
                    && null == cellFormat.FormatId
                    && null == cellFormat.BorderId
                    && null == cellFormat.FillId
                    && null == cellFormat.FontId
                    && null == cellFormat.Alignment
                    && null == cellFormat.Protection
                    && null == cellFormat.PivotButton
                    && null == cellFormat.ExtensionList
                    ;

                if (found) return index; else index++;
            }

            // No luck.
            return null;
        }

        /// <summary>
        /// Registers a numbering format with the stylesheet if not already registered.
        /// Returns the numbering format id of the existing or newly added numbering format
        /// </summary>
        private static uint GetOrAddNumberingFormat(this Stylesheet stylesheet, string formatCode)
        {
            ArgumentNullException.ThrowIfNull(stylesheet);
            ArgumentNullException.ThrowIfNull(formatCode);

            // Is this a well-known format or the format already registered?
            uint? id = stylesheet.TryGetNumberFormatId(formatCode);
            if (id.HasValue) return id.Value;

            // Ensure stylesheet has NumberingFormats container
            NumberingFormats formats = stylesheet.NumberingFormats ??= new();

            // This is a new formatCode and is not a well-known format.
            // Coin a number format id, something >= FirstCustomNumberFormatId
            uint? maxId = (uint?)formats.Elements<NumberingFormat>().Max(x => x.NumberFormatId?.Value);
            uint nextNumberFormatId = maxId.HasValue && maxId.Value >= FirstCustomNumberFormatId ? maxId.Value + 1 : FirstCustomNumberFormatId;

            // Create a new numbering format
            NumberingFormat customNumberingFormat = new()
            {
                NumberFormatId = nextNumberFormatId,
                FormatCode = formatCode
            };

            // Register with the stylesheet.
            formats.AppendChild(customNumberingFormat);

            // Return the newly added custom number format id.
            return customNumberingFormat.NumberFormatId;
        }

        /// <summary>
        /// Returns number format id of the given formatCode if its already registered or a well-known format.
        /// </summary>
        private static uint? TryGetNumberFormatId(this Stylesheet stylesheet, string formatCode)
        {
            ArgumentNullException.ThrowIfNull(stylesheet);
            ArgumentNullException.ThrowIfNull(formatCode);

            // Does the format represents one of the predefined numbering format?
            uint? wellKnownNumberFormatId = formatCode switch
            {
                "General" => 0,       // Default format (no specific type)
                "0" => 1,       // Decimal with no decimal places
                "0.00" => 2,       // Decimal with two decimal places
                "#,##0" => 3,       // Thousands separator, no decimal
                "#,##0.00" => 4,       // Thousands separator, two decimals
                "0%" => 9,       // Percentage, no decimal
                "0.00%" => 10,      // Percentage, two decimals
                "m/d/yyyy" => 14,      // Short Date (US style)
                "d-mmm-yy" => 15,      // Date (Day-Month-Year)
                _ => null
            };
            if (wellKnownNumberFormatId.HasValue) return wellKnownNumberFormatId.Value;

            // If the stylesheet doesn't have NumberingFormats container.
            if (null == stylesheet.NumberingFormats) return null;

            // Does the stylesheet contain suggested formatCode?
            return stylesheet
                .NumberingFormats.Elements<NumberingFormat>()
                .Where(x => formatCode.Equals(x.FormatCode))
                .Select(x => x.NumberFormatId?.Value)
                .FirstOrDefault();
        }

        /// <summary>
        /// Update the Count property of all containers (if present)
        /// </summary>
        internal static void UpdateCounts(this Stylesheet stylesheet)
        {
            ArgumentNullException.ThrowIfNull(stylesheet);

            stylesheet.Fonts?.Count = (uint?)stylesheet.Fonts.ChildElements.Count;
            stylesheet.Fills?.Count = (uint?)stylesheet.Fills.ChildElements.Count;
            stylesheet.Borders?.Count = (uint?)stylesheet.Borders.ChildElements.Count;
            stylesheet.CellStyleFormats?.Count = (uint?)stylesheet.CellStyleFormats.ChildElements.Count;
            stylesheet.CellFormats?.Count = (uint?)stylesheet.CellFormats.ChildElements.Count;
            stylesheet.NumberingFormats?.Count = (uint?)stylesheet.NumberingFormats.ChildElements.Count;
        }
    }
}
