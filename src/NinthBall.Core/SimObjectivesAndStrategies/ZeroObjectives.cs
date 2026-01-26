

using System.Text.RegularExpressions;

namespace NinthBall.Core
{
    // SimEngine requires a strategy for every StrategyFamily.
    // For what‑if runs, use a NoOp objective to explicitly mute any objective you want to ignore.
    abstract class NoOpObjective : ISimObjective, ISimStrategy
    {
        // Push to end.
        int ISimObjective.Order => int.MaxValue;

        // I am the objective and I am the strategy.
        ISimStrategy ISimObjective.CreateStrategy(int _) => this;

        // Do nothing.
        void ISimStrategy.Apply(ISimState _) { }

        public override string ToString() => Humanize(this.GetType().Name);
        
        static string Humanize(string input) => RxCaps.Replace(input.Replace("Objective", string.Empty), " $1");

        static readonly Regex RxCaps= new("(?<!^)([A-Z])", RegexOptions.Compiled);
    }

    [StrategyFamily(StrategyFamily.Income)]         sealed class NoAdditionalIncomeObjective : NoOpObjective;
    [StrategyFamily(StrategyFamily.Expenses)]       sealed class NoLivingExpensesObjective : NoOpObjective;
    [StrategyFamily(StrategyFamily.Withdrawals)]    sealed class NoWithdrawalObjective : NoOpObjective;
    [StrategyFamily(StrategyFamily.RMD)]            sealed class NoRequiredMinimumDistributionObjective : NoOpObjective;
    [StrategyFamily(StrategyFamily.Fees)]           sealed class NoAnnualFeesObjective : NoOpObjective;
    [StrategyFamily(StrategyFamily.Taxes)]          sealed class NoTaxObjective : NoOpObjective;
    [StrategyFamily(StrategyFamily.Growth)]         sealed class NoGrowthObjective : NoOpObjective;
    [StrategyFamily(StrategyFamily.Rebalance)]      sealed class NoRebalanceObjective : NoOpObjective;

}
