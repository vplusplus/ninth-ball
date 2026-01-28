
namespace NinthBall.Core
{
    internal enum StrategyFamily
    {
        None,
        Income,
        Expenses,
        Fees,
        Taxes,
        Withdrawals,
        Growth,
        Rebalance,
        RMD
    }

    /// <summary>
    /// Represents a simulation objective.
    /// </summary>
    internal interface ISimObjective
    {
        int MaxIterations { get => int.MaxValue; }
        ISimStrategy CreateStrategy(int iterationIndex);
    }

    /// <summary>
    /// Implements the simulation objective.
    /// </summary>
    internal interface ISimStrategy
    {
        void Apply(ISimState context);
    }

    /// <summary>
    /// Mutable working-memory of one iteration.
    /// </summary>
    internal interface ISimState
    {
        //....................................................
        // Current iteration
        //....................................................
        int IterationIndex { get; }
        int StartAge { get; }
        int YearIndex { get; }
        int Age { get; }

        SimYear PriorYear { get; }
        Metrics PriorYearMetrics { get; }

        Assets Initial { get; }
        Rebalanced Rebalanced { get; set; }
        Assets Jan { get; }
        Fees Fees { get; set; }
        Taxes Taxes { get; set; }
        Incomes Incomes { get; set; }
        Expenses Expenses { get; set; }
        Withdrawals Withdrawals { get; set; }
        ROI ROI { get; set; }

        void Rebalance(double preTaxAllocation, double postTaxAllocation, double maxDrift);
    }

    /// <summary>
    /// Readonly working-memory of one iteration.
    /// </summary>
    internal interface IReadOnlySimState
    {
        int IterationIndex { get; }
        int StartAge { get; }
        int YearIndex { get; }
        int Age { get; }

        SimYear PriorYear { get; }
        Metrics PriorYearMetrics { get; }

        Assets Initial { get; }
        Rebalanced Rebalanced { get; }
        Assets Jan { get; }
        Fees Fees { get; }
        Taxes Taxes { get; }
        Incomes Incomes { get; }
        Expenses Expenses { get; }
        Withdrawals Withdrawals { get; }
        ROI ROI { get; }
    }

    /// <summary>
    /// Each strategy must declare its family identifier.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    internal sealed class StrategyFamilyAttribute(StrategyFamily family) : System.Attribute
    {
        public readonly StrategyFamily Family = family;
    }
}

