

using NinthBall.Core;

namespace UnitTests.WhatIf
{
    /// <summary>
    /// Model that represents a 'what-if' config section.
    /// </summary>
    public sealed record WhatIfOptions
    (
        double TargetSurvivalRate,
        WhatIfOptions.MinMaxSteps InitialBalance,
        WhatIfOptions.MinMaxSteps FirstYearExpense,
        WhatIfOptions.MinMaxSteps StartAge
    )
    {
        public readonly record struct MinMaxSteps(double Min, double Max, double Steps, double Target);
    }

    /// <summary>
    /// Represents single what-if question.
    /// </summary>
    public readonly record struct WhatIfVariant(double InitialBalance, double FirstYearExpense, int StartAge, int NoOfYears);

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
        public readonly double RealBalance05thPercentile { init; get; }
        public readonly double RealBalance10thPercentile { init; get; }
        public readonly double RealBalance20thPercentile { init; get; }
        public readonly double RealBalance50thPercentile { init; get; }

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
            RealBalance05thPercentile = simResult.IterationAtPercentile(0.05).EndingBalanceReal.RoundToMultiples(1000.0);
            RealBalance10thPercentile = simResult.IterationAtPercentile(0.10).EndingBalanceReal.RoundToMultiples(1000.0);
            RealBalance20thPercentile = simResult.IterationAtPercentile(0.20).EndingBalanceReal.RoundToMultiples(1000.0);
            RealBalance50thPercentile = simResult.IterationAtPercentile(0.50).EndingBalanceReal.RoundToMultiples(1000.0);
        }

        public readonly string AgeRange => $"{StartAge}-{StartAge + NumYears - 1} ({NumYears}y)";
    }


}
