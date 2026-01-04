
namespace NinthBall.Core
{
    internal enum StrategyFamily
    {
        None,
        PortfolioManagement,
        Income,
        Taxes,
        Fees,
        LifestyleExpenses,
        WithdrawalVelocity,
        WithdrawalAdjustment,
        CashUsage,
        CashRefill,
    }

    /// <summary>
    /// Each strategy can declare its primary input and family identifier.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    internal sealed class SimInputAttribute : System.Attribute
    {
        public readonly Type StrategyType;
        public readonly Type OptionsType;
        public StrategyFamily Family { get; init; } = StrategyFamily.None;

        public SimInputAttribute(Type strategyType, Type optionsType)
        {
            ArgumentNullException.ThrowIfNull(strategyType);
            ArgumentNullException.ThrowIfNull(optionsType);

            this.StrategyType = strategyType;
            this.OptionsType = optionsType;
        }
    }
}
