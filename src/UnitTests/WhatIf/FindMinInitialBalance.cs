using Microsoft.Extensions.Configuration;
using NinthBall.Core;
using NinthBall.Reports.PrettyPrint;
using System.Data;
using System.Diagnostics;

namespace UnitTests.WhatIf
{
    public partial class MultipleSimulations
    {
        [TestMethod]
        public async Task FindMinimumInitialBalance()
        {
            const string ReportFileName = "MinInitialBalance.md";
            const double MinBalance = 1000000;
            const double MaxBalance = 3000000;
            const double Steps = 100000;

            // Elimination criteria
            const double MinSurvivalRate = 0.9;

            // Base configuration (same as MultipleSimulations)
            var baseConfig = new ConfigurationBuilder()
                .AddSimulationDefaults()
                .AddYamlResources(typeof(MultipleSimulations).Assembly, ".TestInputs.")
                .Build();

            // Read required sections (only for reporting)
            var p = baseConfig.ReadAndValidateRequiredSestion<SimParams>();
            var init = baseConfig.ReadAndValidateRequiredSestion<Initial>();
            var exp = baseConfig.ReadAndValidateRequiredSestion<LivingExpenses>();

            // Prepare output table
            var dt = new DataTable()
                .WithColumn<double>("PreTax",           format:"C0")
                .WithColumn<double>("PostTax",          format: "C0")
                .WithColumn<double>("SurvivalRate",     format: "P1")
                .WithColumn<double>("Balance(r) 10th",  format: "C0")
                .WithColumn<double>("Balance(r) 20th",  format: "C0")
                .WithColumn<double>("milliSec",         format: "F0")
                ;

            // Sweep range
            for (double initialBalance = MinBalance; initialBalance <= MaxBalance; initialBalance += Steps)
            {
                var overrides = SimInputOverrides
                    .For<Initial>()
                    .With(x => x.PreTax.Amount, initialBalance)
                    .With(x => x.PostTax.Amount, initialBalance);

                var elapsed = Stopwatch.StartNew();
                var simResult = MultipleSimulations.RunSimulation(overrides);
                elapsed.Stop();

                if (simResult.SurvivalRate < MinSurvivalRate) continue;

                var row = new List<object> { 
                    initialBalance,
                    initialBalance,
                    simResult.SurvivalRate,
                    simResult.IterationAtPercentile(0.1).EndingBalanceReal,
                    simResult.IterationAtPercentile(0.2).EndingBalanceReal,
                    elapsed.Elapsed.TotalMilliseconds
                };
                dt.Rows.Add(row.ToArray());
            }

            var keyInputs = new
            {
                From    = $"{p.StartAge} years",
                To      = $"{p.StartAge + p.NoOfYears} years",
                Year1   = $"{exp.FirstYearAmount / 1000:C0} K"
            };

            // Write markdown report
            using (var writer = File.CreateText(Path.Combine(MultipleSimulations.ReportsFolder, ReportFileName)))
            {
                writer
                    .PrintMarkdownTitle2("Initial Balance vs Survival Rate")

                    .PrintMarkdownTitle3("Input")
                    .PrintMarkdownRecordWide(keyInputs)

                    .PrintMarkdownTitle3("Results")
                    .PrintMarkdownTable(dt)
                    .AppendLine();
            }
        }
    }
}
