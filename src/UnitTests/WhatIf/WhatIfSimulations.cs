
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NinthBall.Core;

namespace UnitTests.WhatIf
{
    [TestClass]
    public partial class WhatIfSimulations
    {
        public const string ReportsFolder = @"D:\Source\ninth-ball\src\UnitTests\Reports\";

        // TODO: Move base configuration to Core assembly
        private static IConfiguration MyBaseConfiguration => new ConfigurationBuilder()
            .AddSimulationDefaults()
            .AddYamlResources(typeof(WhatIfSimulations).Assembly, ".WhatIfInputs.")
            .Build();

        private static WhatIfMetrics RunOneSimulation(IConfiguration baseConfiguration, SimInputOverrides overrides)
        {
            ArgumentNullException.ThrowIfNull(baseConfiguration);
            ArgumentNullException.ThrowIfNull(overrides);

            var builder = Host.CreateEmptyApplicationBuilder(settings: new());

            builder.Configuration
                .AddConfiguration(baseConfiguration)
                .AddOverrides(overrides);

            builder.Services
                .AddSimulationComponents();

            using (var session = builder.Build())
            {
                var simResult = session.Services.GetRequiredService<ISimulation>().Run();
                return new(simResult);
            }
        }

    }
}