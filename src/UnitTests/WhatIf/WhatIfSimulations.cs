
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NinthBall.Core;
using System.Diagnostics;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace UnitTests.WhatIf
{
    /// <summary>
    /// Represents single what-if question.
    /// </summary>
    public readonly record struct Variant(double InitialBalance, double FirstYearExpense, int StartAge, int NoOfYears);

    /// <summary>
    /// Represents outcome of a single what-if question, captures key simulation metrics.
    /// </summary>
    public readonly record struct WhatIfResult
    {
        // Model Inputs
        public readonly int StartAge { init; get; }
        public readonly int NumYears { init; get; }
        public readonly int NumIterations { init; get; }
        public readonly double InitialBalance { init; get; }
        public readonly double Year0Expense { init; get; }
        public readonly string[] Strategies { init; get; }

        // Model predictions
        public readonly double SurvivalRate { init; get; }
        public readonly double RealBalance05th { init; get; }
        public readonly double RealBalance10th { init; get; }
        public readonly double RealBalance20th { init; get; }
        public readonly double RealBalance50th { init; get; }

        // IMPORTANT: Do not use primary constructor which will capture and hold on to SimResult.
        // We do not want to hold on to the costly SimResult data structure and its memory footprint.
        public WhatIfResult(SimResult simResult)
        {
            // Iteration #0 - year #0
            var I0Y0 = simResult.Iterations[0].ByYear.Span[0];

            // Invariants - Captuted values are same for Year#0 of ALL iterations.
            StartAge = simResult.SimParams.StartAge;
            NumYears = simResult.SimParams.NoOfYears;
            NumIterations = simResult.SimParams.Iterations;
            InitialBalance = I0Y0.Jan.PreTax.Amount + I0Y0.Jan.PostTax.Amount;
            Year0Expense = I0Y0.Expenses.Total;
            Strategies = simResult.Strategies.ToArray();

            // Aggregate results
            SurvivalRate = Math.Round(simResult.SurvivalRate, 2);
            RealBalance05th = simResult.IterationAtPercentile(0.05).EndingBalanceReal.RoundToMultiples(1000.0);
            RealBalance10th = simResult.IterationAtPercentile(0.10).EndingBalanceReal.RoundToMultiples(1000.0);
            RealBalance20th = simResult.IterationAtPercentile(0.20).EndingBalanceReal.RoundToMultiples(1000.0);
            RealBalance50th = simResult.IterationAtPercentile(0.50).EndingBalanceReal.RoundToMultiples(1000.0);
        }

        public readonly string AgeRange => $"{StartAge}-{StartAge + NumYears - 1} ({NumYears}y)";
    }

    [TestClass]
    public partial class WhatIfSimulations
    {
        const string ReportsFolder  = @"D:\Source\ninth-ball\src\UnitTests\Reports\";
        const string ReportFileName = @"WhatIf-Results.json.txt";

        [TestMethod]
        public void RunWhatIfSimulation()
        {
            // Prepare simulation configuration from /whatifinputs/*.yaml
            var baseConfig = new ConfigurationBuilder()
                .AddSimulationDefaults()
                .AddYamlResources(typeof(WhatIfSimulations).Assembly, ".WhatIfInputs.")
                .Build();

            // Prepare a combinations of initial balance, first year exp and start age combinations.
            var baseParams = baseConfig.ReadAndValidateRequiredSection<SimParams>();
            var whatIfOptions = baseConfig.ReadAndValidateRequiredSection<WhatIfOptions>();
            var varyBy = new List<Variant>();
            for (double ib = whatIfOptions.InitialBalance.Min; ib <= whatIfOptions.InitialBalance.Max; ib += whatIfOptions.InitialBalance.Steps)
                for (double y0Exp = whatIfOptions.FirstYearExpense.Min; y0Exp <= whatIfOptions.FirstYearExpense.Max; y0Exp += whatIfOptions.FirstYearExpense.Steps)
                    for (int startAge = (int)whatIfOptions.StartAge.Min; startAge <= whatIfOptions.StartAge.Max; startAge += (int)whatIfOptions.StartAge.Steps)
                    {
                        var adjustedNumYears = baseParams.NoOfYears - (startAge - baseParams.StartAge);
                        varyBy.Add(new Variant(ib, y0Exp, startAge, adjustedNumYears));
                    }
            Console.WriteLine($"Will test {varyBy.Count} combinations of initial-balance, year-0-expense and start-age.");

            // Simulate in parallel, collect results
            var elapsed = Stopwatch.StartNew();
            var whatIfResults = varyBy
                .AsParallel()
                .WithDegreeOfParallelism(Environment.ProcessorCount)
                .Select(x => TryOneVariation(baseConfig, x))
                .ToList();
            elapsed.Stop();
            Console.WriteLine($"Completed {whatIfResults.Count:#,0} simulations | Elapsed: {elapsed.Elapsed.TotalSeconds:#,0} sec");

            var output = new
            {
                Description = "Represents Monte Carlo what-if simulation results. WhatIfOptions represents range of questions asked. WhatIfResults represents key metrics of each Monte Carlo simulation.",
                WhatIfOptions = whatIfOptions,
                WhatIfResults = whatIfResults
            };

            // Serialize and save as json
            var jsonResults = JsonSerializer.Serialize(output, new JsonSerializerOptions() 
            { 
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
            
            File.WriteAllText(
                Path.Combine(WhatIfSimulations.ReportsFolder, WhatIfSimulations.ReportFileName),
                jsonResults
            );
        }

        static WhatIfResult TryOneVariation(IConfiguration baseConfiguration, Variant varyBy)
        {
            var overrides = SimInputOverrides
                .For<SimParams>()
                    .With(x => x.StartAge, varyBy.StartAge)
                    .With(x => x.NoOfYears, varyBy.NoOfYears)
                .For<Initial>()
                    .With(x => x.PreTax.Amount, varyBy.InitialBalance / 2)
                    .With(x => x.PostTax.Amount, varyBy.InitialBalance / 2)
                .For<LivingExpenses>()
                    .With(x => x.FirstYearAmount, varyBy.FirstYearExpense);

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
