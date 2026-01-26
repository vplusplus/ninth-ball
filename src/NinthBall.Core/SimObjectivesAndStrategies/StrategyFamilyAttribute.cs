
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
    /// Each strategy must declare its family identifier.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    internal sealed class StrategyFamilyAttribute(StrategyFamily family) : System.Attribute
    {
        public readonly StrategyFamily Family = family;
    }
}
