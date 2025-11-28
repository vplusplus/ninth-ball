
using System.Text.RegularExpressions;

namespace NinthBall
{
    internal static partial class CmdLine
    {
        public static readonly IReadOnlyDictionary<string, string> Options = ParseCommandlineOnce();

        public static string Optional(string name, string defaultValue) => Options.TryGetValue(name, out var something) && !string.IsNullOrWhiteSpace(something) ? something : defaultValue;

        public static string Required(string name) => Options.TryGetValue(name, out var something) && !string.IsNullOrWhiteSpace(something) ? something : throw new FatalWarning($"Missing commandline arg | --{name}");

        public static bool Switch(string name) => Options.ContainsKey(name) && bool.Parse(Options[name]);

        static IReadOnlyDictionary<string, string> ParseCommandlineOnce()
        {
            bool IsName(string something) => null != something && DashDashNoDash().IsMatch(something);

            var args = Environment.GetCommandLineArgs().Skip(1).Where(x => !string.IsNullOrEmpty(x)).Select(x => x.Trim()).ToArray();
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var index = 0;

            while (index < args.Length)
            {
                var current = args[index++];
                var next = index < args.Length ? args[index] : null;

                if (IsName(current))
                {
                    current = current.TrimStart('-').Trim();

                    if (null == next || IsName(next))
                    {
                        map[current] = "true";
                    }
                    else
                    {
                        map[current] = next;
                        index += 1;
                    }
                }
            }

            return map.AsReadOnly();
        }

        [GeneratedRegex("^--[^\\s-]+", RegexOptions.Compiled)] private static partial Regex DashDashNoDash();
    }
}
