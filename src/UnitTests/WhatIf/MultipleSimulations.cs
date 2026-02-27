using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NinthBall.Core;
using NinthBall.Reports.PrettyPrint;
using System.Data;

namespace UnitTests.WhatIf
{
    [TestClass]
    public class MultipleSimulations
    {
        const string ReportsFolder = @"D:\Source\ninth-ball\src\UnitTests\Reports\";

        [TestMethod]
        public async Task DifferentGrowthObjectives()
        {
            // Prepare base configuration
            var baseConfig = new ConfigurationBuilder()
                .AddSimulationDefaults()
                .AddYamlResources(typeof(MultipleSimulations).Assembly, ".TestInputs.")
                .Build();

            // Capture inputs, for reporting later
            var p = baseConfig.ReadAndValidateRequiredSestion<SimParams>();
            var init = baseConfig.ReadAndValidateRequiredSestion<Initial>();
            var exp = baseConfig.ReadAndValidateRequiredSestion<LivingExpenses>();

            var dt = PrepareOutputTable();

            string[] growthObjectives = ["FlatGrowth", "HistoricalGrowth", "RandomHistoricalGrowth", "RandomGrowth"];   // ["FlatGrowth", "HistoricalGrowth";

            foreach (var growthObjective in growthObjectives)
            {
                SimInputOverrides overrides = SimInputOverrides
                    .For<SimParams>().Append(x => x.Objectives, growthObjective, baseConfig)
                    .For<BootstrapOptions>().With(x => x.RegimeAwareness, 1.0);

                AppendResult(dt, 
                    growthObjective, 
                    RunSimulation(overrides)
                );
            }

            var keyInputs = new
            {
                From = $"{p.StartAge} years", To   = $"{p.StartAge + p.NoOfYears} years",
                PreTax = $"{init.PreTax.Amount / 1000000:C1} M", PostTax = $"{init.PostTax.Amount / 1000000:C1} M", Year1 = $"{exp.FirstYearAmount/1000:C0} K"
            };

            using (var writer = File.CreateText(Path.Combine(ReportsFolder, "MultipleGrowthObjectives.md")))
            {
                writer
                    .PrintMarkdownTitle2("What-if: Different growth objectives ")

                    .PrintMarkdownTitle3("Input")
                    .PrintMarkdownRecordWide(keyInputs)

                    .PrintMarkdownTitle3("Results")
                    .PrintMarkdownTable(dt)
                    .AppendLine()

                    .PrintMarkdownTitle3("Input details:")

                    .PrintMarkdownTitle4("Simulation params:")
                    .PrintMarkdownJson(p)

                    .PrintMarkdownTitle4("Inittial:")
                    .PrintMarkdownJson(init)

                    .PrintMarkdownTitle4("Expenses:")
                    .PrintMarkdownJson(exp)

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

            static DataTable PrepareOutputTable()
            {
                return new DataTable()

                    .WithColumn<string>("GrowthStrategy")
                    .WithColumn<int>("NumIterations",       format: "N0")
                    .WithColumn<double>("SurvivalRate",     format: "P0")

                    .WithColumn<double>("Balance(r) 5th",   format: "C0")
                    .WithColumn<double>("Balance(r) 10th",  format: "C0")
                    .WithColumn<double>("Balance(r) 15th",  format: "C0")
                    .WithColumn<double>("Balance(r) 20th",  format: "C0")
                    ;
            }

            static void AppendResult(DataTable dt, string growthStrategy, SimResult simResult)
            {
                var row = new List<object>(6);

                row.Add(growthStrategy);
                row.Add(simResult.Iterations.Count);
                row.Add(simResult.SurvivalRate);

                row.Add(simResult.IterationAtPercentile(0.05).EndingBalanceReal);
                row.Add(simResult.IterationAtPercentile(0.10).EndingBalanceReal);
                row.Add(simResult.IterationAtPercentile(0.15).EndingBalanceReal);
                row.Add(simResult.IterationAtPercentile(0.20).EndingBalanceReal);

                dt.Rows.Add(row.ToArray());
            }

        }
    }
}

