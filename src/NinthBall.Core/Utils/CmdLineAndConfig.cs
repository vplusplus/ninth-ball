
using Microsoft.Extensions.Configuration;
using System.Text.RegularExpressions;

namespace NinthBall.Core
{
    /// <summary>
    /// Provides access to CommandLine options before host is built and configured.
    /// </summary>
    public static class CmdLine
    {
        static readonly Lazy<IConfiguration> LazyCommandLine = new(() =>
            new ConfigurationBuilder()
                .AddInMemoryCollection(ParseCommandLineOnce())
                .Build()
        );

        public static IConfiguration Current => LazyCommandLine.Value;

        public static string Optional(string name, string defaultValue) => Current.GetValue<string>(name, defaultValue);
        
        public static string Required(string name) => string.IsNullOrWhiteSpace(Current[name]) ? throw new FatalWarning($"Missing CommandLine arg | --{name}") : Current[name]!;
        
        public static bool Switch(string name) => Current.GetSection(name).Exists() && bool.Parse(Current[name]!);

        public static IEnumerable<KeyValuePair<string, string?>> ParseCommandLineOnce()
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

    /// <summary>
    /// Provides access to configurations (AppSettings, Env, CmdLine) for parts of the sysdtem not governed by DI.
    /// </summary>
    public static class Config
    {
        static readonly Lazy<IConfiguration> LazyConfig = new(() =>
            new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                .AddEnvironmentVariables()
                .AddInMemoryCollection(CmdLine.ParseCommandLineOnce())
                .Build()
        );

        public static IConfiguration Current => LazyConfig.Value;

        // Similar to IConfiguration[] - Difference is empty string or whitespaces is treated as missing key
        public static string GetValue(string name, string defaultValue)
        {
            var something = Current[name];
            return string.IsNullOrWhiteSpace(something) ? defaultValue : something;
        }

        // Similar to IConfiguration.GetValue<T>() - Difference is empty string or whitespaces is treated as missing key
        public static TValue GetValue<TValue>(string name, TValue defaultValue)
        {
            var something = Current.GetValue<TValue>(name, defaultValue);
            return something ?? defaultValue;
        }

        // Similar to IConfiguration.GetValue<double>() - Accepts string formatted numbers (1,000,000) and Percenytages (60%)
        public static double GetPct(string name, double defaultValue)
        {
            var strValue = GetValue(name, null!);
            return string.IsNullOrWhiteSpace(strValue) ? defaultValue : PercentageToDoubleConverter.ParseDoubleOrPercentage(strValue);
        }
    }

}
