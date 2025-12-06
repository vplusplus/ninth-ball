
namespace NinthBall
{
    /// <summary>
    /// Utility for unit testing.
    /// Allows you to create ISimObjective using a simple function with no additional boilerplate.
    /// </summary>
    public sealed class FxSimObjective(Action<SimContext> fxStrategy) : ISimObjective
    {
        public static ISimObjective Create(Action<SimContext> fxStrategy) => new FxSimObjective(fxStrategy);

        ISimStrategy ISimObjective.CreateStrategy(int iterationIndex) => null == fxStrategy
            ? throw new ArgumentNullException(nameof(fxStrategy))
            : new FXStrategy(fxStrategy);

        sealed class FXStrategy(Action<SimContext> fxStrategy) : ISimStrategy
        {
            void ISimStrategy.Apply(SimContext context) => fxStrategy(context);
        }
    }
}
