
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NinthBall.Core;
using NinthBall.Reports;
using System.Text;
using UnitTests.WhatIf;

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
                .AddReportDefaults()
                .AddYamlResources(typeof(MultipleSimulations).Assembly, ".TestInputs.")
                .Build();

            var buffer = new StringBuilder();
            foreach (var objective in growthObjectives)
            {
                // Prepare overrides
                InputOverrides overrides = new InputOverrides()
                    .For<Initial>()
                        .With(x => x.PreTax.Amount, 1000000)
                        .With(x => x.PostTax.Amount, 1000000)
                    .For<SimParams>()
                        // .With(x => x.Iterations, 100)
                        // .With(x => x.NoOfYears, 30)
                        .Append(x => x.Objectives, objective, baseConfig)
                    ;

                // We create and destroy DI container for each simulation session.
                var simSessionBuilder = Host.CreateEmptyApplicationBuilder(settings: new());

                // Apply embedded defaults and base configurations.
                simSessionBuilder.Configuration
                    .AddSimulationDefaults()
                    .AddReportDefaults()
                    .AddYamlResources(typeof(MultipleSimulations).Assembly, ".TestInputs.")
                    .AddInMemoryCollection(overrides)
                    ;

                // Assemble simulation and reporting components.
                simSessionBuilder.Services
                    .AddSimulationComponents()
                    .AddReportComponents()
                    ;

                using (var simSession = simSessionBuilder.Build())
                {
                    // Run simulation
                    ISimulation simulation = simSession.Services.GetRequiredService<ISimulation>();
                    ISimulationReports simReports = simSession.Services.GetRequiredService<ISimulationReports>();
                    var simResult = simulation.Run();

                    // Collect what-if metrics (for now stubbing to use string builder)
                    buffer.AppendLine($"{objective,-30} {simResult.SurvivalRate:P1}");
                }
            }

            Console.WriteLine();
            Console.WriteLine(buffer.ToString());
        }
    }
}

