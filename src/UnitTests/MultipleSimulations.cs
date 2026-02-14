
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NinthBall.Core;
using System.Text;

namespace UnitTests
{
    [TestClass]
    public class MultipleSimulations
    {
        [TestMethod]
        public async Task RunTestSimulations()
        {
            // Target objectives
            string[] growthObjectives = ["FlatGrowth", "HistoricalGrowth", "RandomHistoricalGrowth", "ExpectedGrowth", "ConservativeGrowth", "HighRiskGrowth"];

            // Prepare base configuration
            var baseConfig = new ConfigurationBuilder()
                .AddSimulationDefaults()
                .AddYamlResources(typeof(MultipleSimulations).Assembly, ".TestInputs.")
                .Build();

            var buffer = new StringBuilder();
            foreach (var objective in growthObjectives)
            {
                // Prepare overrides
                SimInputOverrides overrides = SimInputOverrides
                    .For<Initial>()
                        .With(x => x.PreTax.Amount, 1000000)
                        .With(x => x.PostTax.Amount, 1000000)
                    .For<SimParams>()
                        .Append(x => x.Objectives, objective, baseConfig)
                    ;

                // We create and destroy DI container for each simulation session.
                var simSessionBuilder = Host.CreateEmptyApplicationBuilder(settings: new());

                // Apply embedded defaults and base configurations.
                simSessionBuilder.Configuration
                    .AddSimulationDefaults()
                    .AddYamlResources(typeof(MultipleSimulations).Assembly, ".TestInputs.")
                    .AddOverrides(overrides)
                    ;

                // Assemble simulation and reporting components.
                simSessionBuilder.Services
                    .AddSimulationComponents()
                    ;

                using (var simSession = simSessionBuilder.Build())
                {
                    // Run simulation
                    var simResult = simSession.Services.GetRequiredService<ISimulation>().Run();

                    // Collect what-if metrics (for now stubbing to use string builder)
                    buffer.AppendLine($"{objective,-30} {simResult.SurvivalRate:P1}");
                }
            }

            Console.WriteLine();
            Console.WriteLine(buffer.ToString());
        }
    }
}

