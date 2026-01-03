using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Spreadsheet;

namespace NinthBall.Core
{
    public enum HAlign { Left, Center, Right }
    
    public enum VAlign { Top, Middle, Bottom }

    public readonly record struct XLStyle
    (
        string Format = "General",
        string FName  = "Aptos Narrow",
        uint   FSize  = 11,
        uint   FColor = 0xFF000000,
        bool   IsBold = false,
        HAlign HAlign = HAlign.Left,
        VAlign VAlign = VAlign.Top
    );

    public sealed class ExcelStylesheetBuilder
    {
        //......................................................................
        #region ctor(), RegisterStyle() and Build()
        //......................................................................
        Stylesheet __SS = DefaultStyles();
        Stylesheet MyStylesheet => __SS ?? throw new InvalidOperationException("Stylesheet is already built and discarded.");

        static Stylesheet DefaultStyles()
        {
            static Font FontZero() => new Font(new FontSize { Val = 11 }, new Color { Rgb = "FF000000" }, new FontName { Val = "Aptos Narrow" }, new FontFamilyNumbering { Val = 2 }, new FontScheme { Val = FontSchemeValues.Minor });
            static Fill FillNone() => new Fill(new PatternFill { PatternType = PatternValues.None });
            static Fill FillGray125() => new Fill(new PatternFill { PatternType = PatternValues.Gray125 });
            static Border BorderZero() => new Border(new LeftBorder(), new RightBorder(), new TopBorder(), new BottomBorder(), new DiagonalBorder());
            static CellFormat CellStyleFormatZero() => new CellFormat { NumberFormatId = 0, FontId = 0, FillId = 0, BorderId = 0 };
            static CellFormat CellFormatZero() => new CellFormat { FormatId = 0, NumberFormatId = 0, FontId = 0, FillId = 0, BorderId = 0 };
            static CellStyle CellStyleZero() => new CellStyle { Name = "Normal", FormatId = 0, BuiltinId = 0 };

            // A canonically correct, minimal OpenXML stylesheet
            return new Stylesheet
            {
                Fonts = new(FontZero()),
                Fills = new(FillNone(), FillGray125()),
                Borders = new(BorderZero()),
                CellStyleFormats = new(CellStyleFormatZero()),
                CellFormats = new(CellFormatZero()),
                CellStyles = new(CellStyleZero()),
                NumberingFormats = new()
            };
        }

        /// <summary>
        /// Registers the style (and its components) if not present.
        /// Returns the CellFormatId a.k.a. StyleIndex (used as cell.StyleIndex)
        /// </summary>
        public uint RegisterStyle(XLStyle spec) => GetOrAddCellFormat
        (
            numberFormatId: GetOrAddNumberFormat(spec.Format),
            fontId: GetOrAddFont(spec.FName, spec.FSize, spec.FColor, spec.IsBold),
            spec.HAlign,
            spec.VAlign
        );

        /// <summary>
        /// Finalize and return the Stylesheet. This instance will be discarded upon Build()
        /// </summary>
        public Stylesheet Build()
        {
            // Once Stylesheet leaves our control, our internal caches are not guaranteed to be current.
            var ss = __SS ?? throw new InvalidOperationException("Stylesheet is already built and discarded.");
            __SS = null!;

            // Update the counts
            ss.NumberingFormats?.Count = (uint?)ss.NumberingFormats.ChildElements.Count;
            ss.Fonts?.Count = (uint?)ss.Fonts.ChildElements.Count;
            ss.Fills?.Count = (uint?)ss.Fills.ChildElements.Count;
            ss.Borders?.Count = (uint?)ss.Borders.ChildElements.Count;
            ss.CellStyleFormats?.Count = (uint?)ss.CellStyleFormats.ChildElements.Count;
            ss.CellFormats?.Count = (uint?)ss.CellFormats.ChildElements.Count;
            ss.CellStyles?.Count = (uint?)ss.CellStyles.ChildElements.Count;

            return ss;
        }

        #endregion

        //......................................................................
        #region GetOrAddNumberFormat()
        //......................................................................
        const uint FirstCustomNumberFormatId = 143;

        uint NextNumberFormatId = FirstCustomNumberFormatId;

        readonly Dictionary<string, uint> NFIdCache = new()
        {
            ["General"] = 0,
            ["0"] = 1,
            ["0.00"] = 2,
            ["#,##0"] = 3,
            ["#,##0.00"] = 4,
            ["0%"] = 9,
            ["0.00%"] = 10,
            ["m/d/yyyy"] = 14,
            ["d-mmm-yy"] = 15
        };

        uint GetOrAddNumberFormat(string formatCode)
        {
            if (string.IsNullOrWhiteSpace(formatCode)) throw new ArgumentException("Format code cannot be empty", nameof(formatCode));

            // Check cache
            if (NFIdCache.TryGetValue(formatCode, out var existingId)) return existingId;

            // Prepare new instance
            var nextId = NextNumberFormatId++;
            var newFormat = new NumberingFormat() { NumberFormatId = nextId, FormatCode = formatCode };

            // Append, and cache index
            MyStylesheet.NumberingFormats!.Append(newFormat);
            return NFIdCache[formatCode] = nextId;
        }

        #endregion

        //......................................................................
        #region GetOrAddFont()
        //......................................................................
        readonly record struct FontKey(string FontName, uint FontSize, uint FontColorHex, bool IsBold);

        readonly Dictionary<FontKey, uint> FontIdCache = new();

        uint GetOrAddFont(string fontName, uint fontSize = 14, uint fontColorHex = 0xFF000000, bool isBold = false)
        {
            if (string.IsNullOrWhiteSpace(fontName)) throw new ArgumentException("Font name cannot be empty", nameof(fontName));

            // Check cache
            FontKey key = new(fontName, fontSize, fontColorHex, isBold);
            if (FontIdCache.TryGetValue(key, out var existing)) return existing;

            // Prepare new instance
            Font newFont = new
            (
                new FontName { Val = fontName },
                new FontSize { Val = fontSize },
                new Color { Rgb = fontColorHex.ToString("X8") },
                new FontFamilyNumbering { Val = 2 },
                new FontScheme { Val = FontSchemeValues.Minor }
            );
            if (isBold) newFont.AppendChild(new Bold());

            // Append and cache index.
            return FontIdCache[key] = AppendN(MyStylesheet.Fonts!, newFont);
        }

        #endregion

        //......................................................................
        #region GetOrAddFill(), GetOrAddBorder()
        //......................................................................
        // Fill and Border not yet exposed.
        // Can be extended using same pattern.
        #endregion

        //......................................................................
        #region GetOrAddCellFormat()
        //......................................................................
        // FillID and BorderId are stubbed to zero - Exists for future enhancements
        private readonly record struct CFKey(uint NumberFormatId, uint FontId, uint FillId, uint BorderId, HAlign HAlign, VAlign VAlign);

        private readonly Dictionary<CFKey, uint> CellFormatIdCache = new();

        uint GetOrAddCellFormat(uint numberFormatId, uint fontId, HAlign hAlign, VAlign vAlign)
        {
            // Check cache.
            CFKey key = new(numberFormatId, fontId, FillId: 0, BorderId: 0, hAlign, vAlign);
            if (CellFormatIdCache.TryGetValue(key, out var existing)) return existing;

            // Prepare new instance
            var newCellFormat = new CellFormat
            {
                FormatId = 0,
                NumberFormatId = (uint)numberFormatId,
                ApplyNumberFormat = 0 != numberFormatId,
                FontId = fontId,
                ApplyFont = 0 != fontId,
                FillId = 0,
                ApplyFill = false,
                BorderId = 0,
                ApplyBorder = false,
                Alignment = ToOpenXmlAlignment(hAlign, vAlign),
                ApplyAlignment = HasAlignment(hAlign, vAlign),
            };

            // Append and cache index.
            return CellFormatIdCache[key] = AppendN(MyStylesheet.CellFormats!, newCellFormat);


            static HorizontalAlignmentValues ToHAV(HAlign a) => a == HAlign.Left ? HorizontalAlignmentValues.Left : a == HAlign.Right ? HorizontalAlignmentValues.Right : HorizontalAlignmentValues.Center;
            static VerticalAlignmentValues ToVAV(VAlign a)   => a == VAlign.Top ? VerticalAlignmentValues.Top : a == VAlign.Bottom ? VerticalAlignmentValues.Bottom : VerticalAlignmentValues.Center;
            static bool HasAlignment(HAlign ha, VAlign va)   => ha != HAlign.Left || va != VAlign.Top;
            static Alignment? ToOpenXmlAlignment(HAlign ha, VAlign va) => HasAlignment(ha, va) ? new Alignment()
            {
                Vertical   = va == VAlign.Top  ? null : ToVAV(va),
                Horizontal = ha == HAlign.Left ? null : ToHAV(ha)
            } : null;
        }

        #endregion

        //......................................................................
        #region Utils - AppendN()
        //......................................................................
        private static uint AppendN(OpenXmlElement parent, OpenXmlElement oneChild)
        {
            ArgumentNullException.ThrowIfNull(parent);
            ArgumentNullException.ThrowIfNull(oneChild);

            parent.AppendChild(oneChild);
            return (uint)parent.ChildElements.Count - 1;
        }

        #endregion

        //......................................................................
        #region Common number formats
        //......................................................................
        /// <summary>
        /// Common number formats using C# ToString() format specifier naming.
        /// P = Percent, N = Number, C = Currency, F = Fixed-point, D = Date
        /// </summary>
        public static class NumberFormats
        {
            // General
            public const string General = "General";

            // Number formats (N)
            public const string N0 = "#,##0;-#,##0;;";          // Integer with thousands separator
            public const string N1 = "#,##0.0;-#,##0.0;;";      // 1 decimal place
            public const string N2 = "#,##0.00;-#,##0.00;;";    // 2 decimal places

            // Currency formats (C) - US locale
            public const string C0 = "$#,##0;-$#,##0;;";        // Currency, no decimals
            public const string C1 = "$#,##0.0;-$#,##0.0;;";    // Currency, 1 decimal
            public const string C2 = "$#,##0.00;-$#,##0.00;;";  // Currency, 2 decimals

            // Percent formats (P)
            public const string P0 = "0%;-0%;;";                // Percent, no decimals
            public const string P1 = "0.0%;-0.0%;;";            // Percent, 1 decimal
            public const string P2 = "0.00%;-0.00%;;";          // Percent, 2 decimals

            // Fixed-point formats (F)
            public const string F0 = "0;-0;;";                  // Integer, no separator
            public const string F1 = "0.0;-0.0;;";              // 1 decimal, no separator
            public const string F2 = "0.00;-0.00;;";            // 2 decimals, no separator

            // Date formats (D)
            public const string DateMDY = "m/d/yyyy";   // 1/15/2026
            public const string DateShort = "d-mmm-yy"; // 15-Jan-26
        }

        #endregion
    }
}
