using System.Text.RegularExpressions;

namespace NinthBall.Utils
{
    public static class PlaceHolders
    {
        static readonly Regex PlaceholderRegex = new(@"\{([a-z0-9-]+)(?::([^}]+))?\}", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static string ResolvePlaceholders(string input, Func<string, string?, string> fxResolver)
        {
            ArgumentNullException.ThrowIfNull(input);
            ArgumentNullException.ThrowIfNull(fxResolver);

            int loopCount = 0;
            string output = input;

            while (true)
            {
                if (++loopCount > 100) throw new InvalidOperationException("Too many placeholder resolution iterations");

                var match = PlaceholderRegex.Match(output);
                if (!match.Success) return output;

                string tag = match.Groups[1].Value;
                string? optionalFormatSpec = match.Groups[2].Success ? match.Groups[2].Value : null;
                string resolved = fxResolver(tag, optionalFormatSpec) ?? throw new InvalidOperationException($"Unable to resolve placeholder '{tag}'");

                output = output.Replace(match.Value, resolved);
            }
        }

        public static string ResolvePlaceholders(string input, Dictionary<string, object> values)
        {
            ArgumentNullException.ThrowIfNull(input);
            ArgumentNullException.ThrowIfNull(values);

            return ResolvePlaceholders(input, (tag, format) =>
            {
                if (!values.TryGetValue(tag, out var rawValue) || rawValue is null)
                    throw new InvalidOperationException($"No value provided for placeholder '{tag}'");

                // If no format spec, just ToString()
                if (format is null)
                    return rawValue.ToString() ?? string.Empty;

                // Apply format specifier
                return rawValue switch
                {
                    IFormattable f => f.ToString(format, null),
                    _ => throw new InvalidOperationException(
                            $"Placeholder '{tag}' does not support format '{format}'")
                };
            });
        }

    }
}
