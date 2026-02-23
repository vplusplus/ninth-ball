
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NinthBall.Core;
using NinthBall.Core.PrettyPrint;
using System.Data;

namespace UnitTests
{
    [TestClass]
    public class MultipleSimulations
    {
        const string ReportsFolder = @"D:\Source\ninth-ball\src\UnitTests\Reports\";

        [TestMethod]
        public async Task WhatIfDifferentGrowthObjectives()
        {
            // Target objectives
            //string[] growthObjectives = ["FlatGrowth", "HistoricalGrowth", "ExpectedGrowth", "ConservativeGrowth", "HighRiskGrowth"];
            string[] growthObjectives = [ "FlatGrowth", "HistoricalGrowth" ];

            // Prepare base configuration
            var baseConfig = new ConfigurationBuilder()
                .AddSimulationDefaults()
                .AddYamlResources(typeof(MultipleSimulations).Assembly, ".TestInputs.")
                .Build();

            var dt = new DataTable();
            dt
                .WithColumn<string>("GrowthStrategy")
                .WithColumn<double>("RegimeAwareness", format: "P0")
                .WithColumn<int>("NumIterations", format: "N0")
                .WithColumn<double>("SurvivalRate", format: "P0")
                .WithColumn<double>("20th Pctl Balance", format: "C0")
                .WithColumn<double>("20th Pctl Real CAGR", format: "P2")
                ;



            foreach (var objective in growthObjectives)
            {
                //// Prepare overrides
                SimInputOverrides overrides = SimInputOverrides
                    .For<SimParams>()
                        .Append(x => x.Objectives, objective, baseConfig)
                    ;

                var simResult = RunSimulation(overrides);
                var pctl20 = simResult.Percentile(0.2).EndingBalanceReal;

                var row = new List<object>(6);
                row.Add(objective);
                row.Add(0.0);
                row.Add(simResult.Iterations.Count);
                row.Add(simResult.SurvivalRate);
                row.Add(simResult.Percentile(0.2).EndingBalanceReal);
                row.Add(simResult.Percentile(0.2).LastGoodYear.Growth.RealAnnualizedReturn);
                dt.Rows.Add(row.ToArray());
            }

            double[] regimeAwarenessList = { 0.0, 0.25, 0.5, 0.75, 1.0 };

            foreach(var regimeAwareness in regimeAwarenessList)
            {
                //// Prepare overrides
                SimInputOverrides overrides = SimInputOverrides
                    .For<SimParams>()
                        .Append(x => x.Objectives, "RandomHistoricalGrowth", baseConfig)
                    .For<MovingBlockBootstrapOptions>()
                        .With(x => x.RegimeAwareness, regimeAwareness)
                    ;

                var simResult = RunSimulation(overrides);
                var pctl20 = simResult.Percentile(0.2).EndingBalanceReal;

                var row = new List<object>(6);
                row.Add("RandomHistoricalGrowth");
                row.Add(regimeAwareness);
                row.Add(simResult.Iterations.Count);
                row.Add(simResult.SurvivalRate);
                row.Add(simResult.Percentile(0.2).EndingBalanceReal);
                row.Add(simResult.Percentile(0.2).LastGoodYear.Growth.RealAnnualizedReturn);
                dt.Rows.Add(row.ToArray());
            }

            using (var writer = File.CreateText(Path.Combine(ReportsFolder, "WhatIf-GrowthObjectives.md")))
            {
                writer
                    .PrintMarkdownTitle2("## What-if: Different growth objectives ");

                writer
                    .PrintMarkdownTable(dt)
                    .AppendLine();
            }

            return;


            static SimResult RunSimulation(SimInputOverrides overrides)
            {
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
                    return simResult;

                }
            }

        }
    }
}

