
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
            var strategies = new HashSet<string>();
            var whatIfResults = varyBy
                .AsParallel()
                .WithDegreeOfParallelism(Environment.ProcessorCount)
                .Select(x => TryOneVariation(baseConfig, x, strategies))
                .ToList();
            elapsed.Stop();
            Console.WriteLine($"Completed {whatIfResults.Count:#,0} simulations | Elapsed: {elapsed.Elapsed.TotalSeconds:#,0} sec");

            // Run single simulation for the target values
            Variant targetInput = new(
                whatIfOptions.InitialBalance.Target,
                whatIfOptions.FirstYearExpense.Target,
                (int)whatIfOptions.StartAge.Target,
                baseParams.NoOfYears - ((int)whatIfOptions.StartAge.Target - baseParams.StartAge)
            );
            SimResult targetResult = RunOneSimulation(baseConfig, targetInput);




            // Extract common assumptions.
            // Group the strategies by its text, keep only common items, i.e. group count > 1
            string[] commonAssumptions = strategies
                .Where(x => !x.StartsWith("Living expenses"))
                .ToArray();


            var target20 = targetResult.IterationAtPercentile(0.20);

            var output = new
            {
                Description = WhatIfDescription,
                Assumptions = commonAssumptions,
                WhatIfOptions = whatIfOptions,
                WhatIfResults = whatIfResults,

                Target = new
                {
                    targetInput.InitialBalance,
                    targetInput.FirstYearExpense,
                    targetInput.StartAge,
                    targetInput.NoOfYears,

                    NoOfIterations = targetResult.Iterations.Count(),
                    SurvivalRate = Math.Round(targetResult.SurvivalRate, 2),

                    RealBalance05thPercentile = targetResult.IterationAtPercentile(0.05).EndingBalanceReal.RoundToMultiples(1000.0),
                    RealBalance10thPercentile = targetResult.IterationAtPercentile(0.10).EndingBalanceReal.RoundToMultiples(1000.0),
                    RealBalance20thPercentile = targetResult.IterationAtPercentile(0.20).EndingBalanceReal.RoundToMultiples(1000.0),
                    RealBalance50thPercentile = targetResult.IterationAtPercentile(0.50).EndingBalanceReal.RoundToMultiples(1000.0),
                    RealBalanceByIteration = targetResult.Iterations.AsEnumerable().Select(x => x.EndingBalanceReal.RoundToMultiples(1000.0)).ToArray(),

                    YearByYear20thPercentile = targetResult.IterationAtPercentile(0.20).ByYear.ToArray().Select(x => new 
                    { 
                        x.Year,
                        x.Age,

                        JanPreTaxBalanceNominal = x.Jan.PreTax.Amount.RoundToMultiples(1000.0),
                        JanPostTaxBalanceNominal = x.Jan.PostTax.Amount.RoundToMultiples(1000.0),

                        SocialSecurityIncome = x.Incomes.SS,
                        AnnuityIncome = x.Incomes.Ann,

                        PYTaxesTotal = x.Taxes.Total.RoundToMultiples(100.0),
                        PYTaxesFederal = x.Taxes.Federal.Tax.RoundToMultiples(100.0),
                        PYTaxesState = x.Taxes.State.Tax.RoundToMultiples(100.0),
                        PYTaxPCT = Math.Round(x.Taxes.TaxPCT, 3),
                        PYTaxPCTFederal = Math.Round(x.Taxes.TaxPCTFed, 3),
                        PYTaxPCTState = Math.Round(x.Taxes.TaxPCTState, 3),

                        LivingExpense = x.Expenses.LivExp,

                        WithdrawalsPreTax = x.Withdrawals.PreTax.RoundToMultiples(100.0),
                        WithdrawalsPostTax = x.Withdrawals.PostTax.RoundToMultiples(100.0),

                        DepositsPostTax = x.Deposits.PostTax.RoundToMultiples(100.0),

                        StocksROI = Math.Round(x.ROI.StocksROI, 3),
                        BondsROI = Math.Round(x.ROI.BondsROI, 3),
                        InflationRate = Math.Round(x.ROI.InflationRate, 3),
                        ChangePreTax = x.Change.PreTax.RoundToMultiples(10.0),
                        ChangePostTax = x.Change.PostTax.RoundToMultiples(10.0),

                        DecPreTaxBalanceNominal = x.Dec.PreTax.Amount.RoundToMultiples(1000.0),
                        DecPostTaxBalanceNominal = x.Dec.PostTax.Amount.RoundToMultiples(1000.0),

                        DecBalanceReal = x.DecReal.RoundToMultiples(1000.0),
                        RunningInflationMultiplier = Math.Round(x.InflationIndex.Consumer, 2),

                    }).ToArray(),
                }
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

        static WhatIfResult TryOneVariation(IConfiguration baseConfiguration, Variant varyBy, HashSet<string> strategies)
        {
            var simResult = RunOneSimulation(baseConfiguration, varyBy);

            lock (strategies)
            {
                foreach (var strategy in simResult.Strategies) strategies.Add(strategy);
            }

            return new(simResult);
        }

        static SimResult RunOneSimulation(IConfiguration baseConfiguration, Variant varyBy)
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
                return simResult;
            }
        }


        const string WhatIfDescription = @"
# Monte Carlo financial planning 'what-if' simulation results.

## Report elements:
* Assumptions represents simulation strategies that are common across all simulations
* WhatIfOptions represents range of questions asked. 
* WhatIfResults represents key metrics of each Monte Carlo simulation against the what-if questions.
* While the WhatIf elements provides the big picture, the Target elements takes a deeper dive into ONE chosen scenario.
* Target element represents result of single simulation for chosen target scenario
* Target.RealBalanceByIteration is real ending balances of each iteration, that can explain fat tail and skewness
* Target.YearByYear20thPercentile explains what happened year by year on the 20th percentile iteration of the chosen target scenario

## Guidence:
* Consider displaying survival rates are percentage without decimal places. Example: 95%, 98%, etc.
* When presenting survival matrix, if the survival rate is less than 90%, display blank.

";

    }
}
