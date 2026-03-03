
using NinthBall.Core;

namespace UnitTests.WhatIf
{
    /// <summary>
    /// Represents key simulation outcome metrics.
    /// </summary>
    public readonly record struct WhatIfMetrics
    {
        // Invariants - Inputs
        public readonly int StartAge;
        public readonly int NumYears;
        public readonly int NumIterations;
        public readonly double InitialBalance;
        public readonly double Year0Expense;

        // Aggregate results
        public readonly double SurvivalRate;
        public readonly double RBal05th;
        public readonly double RBal10th;
        public readonly double RBal20th;
        public readonly double RBal50th;

        // IMPORTANT: Do not use primary constructor which will capture and hold on to SimResult.
        // We do not want to hold on to the costly SImResult data structure and its memory footprint.
        public WhatIfMetrics(SimResult simResult)
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
            SurvivalRate = simResult.SurvivalRate;
            RBal05th = simResult.IterationAtPercentile(0.05).EndingBalanceReal;
            RBal10th = simResult.IterationAtPercentile(0.10).EndingBalanceReal;
            RBal20th = simResult.IterationAtPercentile(0.20).EndingBalanceReal;
            RBal50th = simResult.IterationAtPercentile(0.50).EndingBalanceReal;
        }

        public readonly string AgeRange => $"{StartAge}-{StartAge+NumYears-1} ({NumYears}y)";
    }
}
