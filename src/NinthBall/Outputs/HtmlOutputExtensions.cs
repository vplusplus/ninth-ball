

namespace NinthBall.Outputs
{
    internal static class HtmlOutputExtensions
    {
        internal static string ToTextFormat(this FormatHint hint)
        {
            return hint switch
            {
                FormatHint.F0 => "F0",
                FormatHint.C0 => "C0",
                FormatHint.C1 => "C1",
                FormatHint.C2 => "C2",
                FormatHint.P0 => "P0",
                FormatHint.P1 => "P1",
                FormatHint.P2 => "P2",
                _ => string.Empty,
            };
        }

        internal static double ToHtmlWidthPCT(this WidthHint hint)
        {
            // WDefault=4%, WSmall=2%, WMedium=4%, W3=6%, WXLarge=8%
            return hint switch
            {
                WidthHint.WSmall => 0.02,
                WidthHint.WDefault => 0.04,
                WidthHint.WMedium => 0.04,
                WidthHint.WLarge => 0.06,
                WidthHint.WXLarge => 0.08,
                _ => 0.04
            };

        }

        internal static string ToCSSColor(this ColorHint hint)
        {
            // None, Success, Warning, Danger, Muted
            return
                hint == ColorHint.None    ? string.Empty :
                hint == ColorHint.Danger  ? "text-danger" :
                hint == ColorHint.Success ? "text-success" :
                hint == ColorHint.Warning ? "text-warning" :
                hint == ColorHint.Muted   ? "text-secondary" :
                string.Empty;
        }

        internal static string ToCSSTextAlignment(this AlignHint hint)
        {
            // Left, Center, Right
            return
                hint == AlignHint.Left ? "text-start" :
                hint == AlignHint.Center ? "text-center" :
                "text-end";
        }
    }
}
