
namespace NinthBall
{
    sealed class UseBufferCashStrategy(UseBufferCash BC) : ISimObjective
    {
        int ISimObjective.Order => 20;

        ISimStrategy ISimObjective.CreateStrategy(int iterationIndex) => throw new NotImplementedException("UserBufferCashStrategy not implemented");
        public override string ToString() => $"Buffer cash | Use cash if growth < {BC.GrowthThreshold:P1} (Max: {BC.MaxAmount:C0})";
    }

    sealed class BufferRefillStrategy(BufferRefill RF) : ISimObjective
    {
        int ISimObjective.Order => 20;

        ISimStrategy ISimObjective.CreateStrategy(int iterationIndex) => throw new NotImplementedException("BufferRefillStrategy not implemented");
        public override string ToString() => $"Buffer cash | Refill if growth > {RF.GrowthThreshold:P1} (Max: {RF.MaxAmount:C0})";
    }

}
