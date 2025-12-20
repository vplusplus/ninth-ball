
namespace NinthBall
{
    sealed class UseBufferCashStrategy(UseBufferCash BC) : ISimObjective
    {
        int ISimObjective.Order => 20;

        ISimStrategy ISimObjective.CreateStrategy(int iterationIndex) => throw new NotImplementedException("UserBufferCashStrategy not implemented");
    }

    sealed class BufferRefillStrategy(BufferRefill RF) : ISimObjective
    {
        int ISimObjective.Order => 20;

        ISimStrategy ISimObjective.CreateStrategy(int iterationIndex) => throw new NotImplementedException("BufferRefillStrategy not implemented");
    }

}
