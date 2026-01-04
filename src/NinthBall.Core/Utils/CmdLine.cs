
using Microsoft.Extensions.Configuration;
using System.Text.RegularExpressions;

namespace NinthBall.Core
{

    /// <summary>
    /// Provides access to CommandLine options before host is built and configured.
    /// </summary>
    public static class CmdLine
    {
        // Lazy initialized CommandLine Options.
        static readonly Lazy<IConfiguration> LazyCommandLine = new(() =>
            new ConfigurationBuilder()
                .AddInMemoryCollection(ParseCommandLineOnce())
                .Build()
        );

        public static IConfiguration Current => LazyCommandLine.Value;

        public static string Optional(string name, string defaultValue) => Current.GetValue<string>(name, defaultValue);
        
        public static string Required(string name) => string.IsNullOrWhiteSpace(Current[name]) ? throw new FatalWarning($"Missing CommandLine arg | --{name}") : Current[name]!;
        
        public static bool Switch(string name) => Current.GetSection(name).Exists() && bool.Parse(Current[name]!);

        private static IEnumerable<KeyValuePair<string, string?>> ParseCommandLineOnce()
        {
            static bool IsName(string something) => null != something && DashDashNoDash.IsMatch(something);

            var args = Environment.GetCommandLineArgs().Skip(1).Where(x => !string.IsNullOrEmpty(x)).Select(x => x.Trim()).ToArray();
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
                        yield return new(current, "true");
                    }
                    else
                    {
                        yield return new(current, next); 
                        index += 1;
                    }
                }
            }
        }

        private static readonly Regex DashDashNoDash = new("^--[^\\s-]+", RegexOptions.Compiled);
    }
}
