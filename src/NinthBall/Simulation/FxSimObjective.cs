
namespace NinthBall
{
    /// <summary>
    /// Helper for unit-testing.
    /// Enables composing a simulation strategy using a simple function with no additional boilerplate.
    /// </summary>
    public sealed class FxSimObjective(Action<ISimContext> fxStrategy) : ISimObjective
    {
        public static ISimObjective Create(Action<ISimContext> fxStrategy) => new FxSimObjective(fxStrategy);

        ISimStrategy ISimObjective.CreateStrategy(int iterationIndex) => null == fxStrategy
            ? throw new ArgumentNullException(nameof(fxStrategy))
            : new FXStrategy(fxStrategy);

        sealed class FXStrategy(Action<ISimContext> fxStrategy) : ISimStrategy
        {
            void ISimStrategy.Apply(ISimContext context) => fxStrategy(context);
        }
    }
}
