
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
        public void MemberPathTests()
        {
            string[] paths = 
            [
                ConfigPath.GetPropertyPath<SimParams, double>(x => x.NoOfYears),
                ConfigPath.GetPropertyPath<SimParams, double>(x => x.StartAge),
                ConfigPath.GetPropertyPath<Initial,   double>(x => x.PreTax.Allocation),
                ConfigPath.GetPropertyPath<Initial,   double>(x => x.PreTax.Amount),
                ConfigPath.GetPropertyPath<Initial,   double>(x => x.PostTax.Allocation),
                ConfigPath.GetPropertyPath<Initial,   double>(x => x.PostTax.Amount),
            ];

            foreach (var item in paths) Console.WriteLine(item);
        }

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
                var overrides = new InputOverrides()
                    //.InitialPreTaxAmount(2000000, baseConfig)
                    //.InitialPostTaxAmount(2000000, baseConfig)
                    .WithObjective(objective, baseConfig);

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

/*
         [TestMethod]
        public async Task RunTestSimulations()
        {
            var now = DateTime.Now;
            var baseDir = @$"D:/Junk/SimReports-{now:MMdd-HHmmss}/";

            if (!Directory.Exists(baseDir)) Directory.CreateDirectory(baseDir);

            string[] growthObjectives = ["FlatGrowth", "HistoricalGrowth", "RandomHistoricalGrowth", "ExpectedGrowth", "ConservativeGrowth", "HighRiskGrowth"];

            var overrides = new InputOverrides();


            var cfg = new ConfigurationBuilder()
                .AddSimulationDefaults()
                .AddReportDefaults()
                .AddYamlResources(typeof(MultipleSimulations).Assembly, ".TestInputs.")
                // HERE I would like to read an existing config which as an array of string and append another item
                // .AddOneStringToExistingStringArray("My:Section:Property", "Test")
                .AddInMemoryCollection(overrides!)
                .Build();

            foreach(var growthObjective in growthObjectives)
            {
                var htmlFileName = Path.Combine(baseDir, $"{growthObjective}.html");

                var oldParams = cfg.GetSection("SimParams").Get<SimParams>();
                string[] newObjectives = [.. oldParams!.Objectives, growthObjective];
                var newParams = oldParams! with
                {
                    Objectives = newObjectives
                };

                var oldOutput = cfg.GetSection("Outputs").Get<OutputOptions>();
                var newOutput = oldOutput! with
                {
                    Html = oldOutput.Html with
                    {
                        File = htmlFileName
                    }
                };

                await RunSimulation(newParams, newOutput);
            }
        }

        private static async Task RunSimulation(params object[] overrides)
        {
            // We create and destroy DI container for each simulation session.
            var simSessionBuilder = Host.CreateEmptyApplicationBuilder(settings: new());

            // Apply default choices and user overrides.
            simSessionBuilder.Configuration
                .AddSimulationDefaults()
                .AddReportDefaults()
                .AddYamlResources(typeof(MultipleSimulations).Assembly, ".TestInputs.")
                ;

            // Assemble simulation and reporting components.
            simSessionBuilder.Services
                .AddSimulationComponents()
                .AddReportComponents()
                ;

            // Apply overrides
            // foreach (var obj in overrides) if (null != obj) simSessionBuilder.Services.AddSingleton(obj.GetType(), obj);

            // Run simulation, export reports
            using (var simSession = simSessionBuilder.Build())
            {
                // Resolve required services
                ISimulation simulation = simSession.Services.GetRequiredService<ISimulation>();
                ISimulationReports simReports = simSession.Services.GetRequiredService<ISimulationReports>();

                var simResults = simulation.Run();
                await simReports.GenerateAsync(simResults);

                var sr = simResults.SurvivalRate;
                var txtSurvivalRate = sr > 0.99 ? $"{sr:P1}" : $"{sr:P0}";
                Console.WriteLine($" Survival rate: {txtSurvivalRate}");
            }
        }
*/