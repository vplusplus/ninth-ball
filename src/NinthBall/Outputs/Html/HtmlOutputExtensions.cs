

namespace NinthBall.Outputs.Html
{
    internal static class HtmlOutputExtensions
    {
        internal static string ToTextFormat(this FormatHint hint)
        {
            // Since our Format enum maps 1:1 to .NET number formats...
            return hint.ToString();
        }

        internal static double[] ToRelativeWidths(this WidthHint[] widthHints)
        {
            ArgumentNullException.ThrowIfNull(widthHints);
            if (0 == widthHints.Length) throw new ArgumentNullException(nameof(widthHints));

            var totalUnits = widthHints.Sum(x => ToWidthUnit(x));
            return widthHints.Select(x => ToWidthUnit(x) / totalUnits).ToArray();

            static double ToWidthUnit(WidthHint hint) => hint switch
            {
                WidthHint.W1x => 1.0,
                WidthHint.W2x => 2.0,
                WidthHint.W3x => 3.0,
                WidthHint.W4x => 4.0,
                _ => 2.0
            };
        }


        internal static string ToCSSColor(this ColorHint hint)
        {
            // None, Success, Warning, Danger, Muted
            return
                hint == ColorHint.None    ? string.Empty :
                hint == ColorHint.Danger  ? "text-danger" :
                hint == ColorHint.Success ? "text-success" :
                hint == ColorHint.Warning ? "text-warning fw-bold" :
                hint == ColorHint.Primary ? "text-primary" :
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
