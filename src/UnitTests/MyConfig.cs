using Microsoft.Extensions.Configuration;

namespace UnitTests
{
    internal static class MyConfig
    {
        public static IConfiguration Instance => LazyInstance.Value;

        static readonly Lazy<IConfiguration> LazyInstance = new ( () =>
            new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                .Build()
        );
    }
}
