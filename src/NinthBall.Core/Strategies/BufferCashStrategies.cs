
namespace NinthBall.Core
{
    [SimInput(typeof(UseBufferCashStrategy), typeof(UseBufferCash), Family = StrategyFamily.CashUsage)]
    sealed class UseBufferCashStrategy(UseBufferCash BC) : ISimObjective
    {
        int ISimObjective.Order => 20;

        ISimStrategy ISimObjective.CreateStrategy(int iterationIndex) => new Strategy(BC);
        public override string ToString() => $"Buffer cash | Use cash if growth < {BC.GrowthThreshold:P1} (Max: {BC.MaxAmount:C0})";

        private sealed class Strategy(UseBufferCash BC) : ISimStrategy
        {
            public void Apply(ISimContext ctx)
            {
                if (ctx.ROI.StocksROI < BC.GrowthThreshold)
                {
                    ctx.Withdrawals = ctx.Withdrawals with { Cash = ctx.Withdrawals.Cash + BC.MaxAmount };
                }
            }
        }
    }

    [SimInput(typeof(BufferRefillStrategy), typeof(BufferRefill), Family = StrategyFamily.CashRefill)]
    sealed class BufferRefillStrategy(BufferRefill RF) : ISimObjective
    {
        int ISimObjective.Order => 20;

        ISimStrategy ISimObjective.CreateStrategy(int iterationIndex) => new Strategy(RF);
        public override string ToString() => $"Buffer cash | Refill if growth > {RF.GrowthThreshold:P1} (Max: {RF.MaxAmount:C0})";

        private sealed class Strategy(BufferRefill RF) : ISimStrategy
        {
            public void Apply(ISimContext ctx)
            {
                if (ctx.ROI.StocksROI > RF.GrowthThreshold)
                {
                    ctx.Refills = ctx.Refills with { Cash = ctx.Refills.Cash + RF.MaxAmount };
                }
            }
        }
    }

}
