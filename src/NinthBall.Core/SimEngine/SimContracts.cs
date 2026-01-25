
namespace NinthBall.Core
{
    internal interface ISimObjective
    {
        int Order { get => 50; }
        int MaxIterations { get => int.MaxValue; }
        ISimStrategy CreateStrategy(int iterationIndex);
    }

    internal interface ISimStrategy
    {
        void Apply(ISimState context);
    }
}

