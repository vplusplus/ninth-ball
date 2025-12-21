
namespace NinthBall.Core
{
    internal enum StrategyFamily
    {
        None,
        LifestyleExpenses,
        WithdrawalVelocity,
        WithdrawalAdjustment,
        CashUsage,
        CashRefill,
        PortfolioManagement,
        Taxes,
        Fees,
        Income
    }


    /// <summary>
    /// Each strategy can declare the primary input it needs using SimInputAttribute
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
