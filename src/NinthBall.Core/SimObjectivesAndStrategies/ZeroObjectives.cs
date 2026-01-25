

using System.Text.RegularExpressions;

namespace NinthBall.Core
{
    // SimEngine will expect one of each objective.
    // For what-if simulations, to mute a specific objective, use the NoOp objectives to be explict about the choice.
    abstract class NoOpObjective : ISimObjective, ISimStrategy
    {
        int ISimObjective.Order => int.MaxValue;
        ISimStrategy ISimObjective.CreateStrategy(int _) => this;
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
