
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NinthBall.Core;

namespace UnitTests.WhatIf
{
    [TestClass]
    public partial class MultipleSimulations
    {
        public const string ReportsFolder = @"D:\Source\ninth-ball\src\UnitTests\Reports\";

        private static IConfiguration MyBaseConfiguration => new ConfigurationBuilder()
            .AddSimulationDefaults()
            .AddYamlResources(typeof(MultipleSimulations).Assembly, ".TestInputs.")
            .Build();

        private static SimResult RunSimulation(IConfiguration baseConfiguration, SimInputOverrides overrides)
        {
            var builder = Host.CreateEmptyApplicationBuilder(settings: new());

            builder.Configuration
                .AddConfiguration(baseConfiguration)
                .AddOverrides(overrides);

            builder.Services
                .AddSimulationComponents();

            using var session = builder.Build();

            return session.Services
                .GetRequiredService<ISimulation>()
                .Run();
        }
    }
}
