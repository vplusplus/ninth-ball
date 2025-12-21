
namespace NinthBall
{
    [AttributeUsage(AttributeTargets.Class)]
    internal sealed class SimInputAttribute : System.Attribute
    {
        public readonly Type StrategyType;
        public readonly Type OptionsType;

        public SimInputAttribute(Type strategyType, Type optionsType)
        {
            ArgumentNullException.ThrowIfNull(strategyType);
            ArgumentNullException.ThrowIfNull(optionsType);

            this.StrategyType = strategyType;
            this.OptionsType = optionsType;
        }
    }
}
