

namespace NinthBall.Core
{
    internal static class AssetExtensions
    {
        extension(Asset asset)
        {
            public double StocksAllocation => asset.Allocation;
            public double BondsAllocation => 1 - asset.Allocation;
            public double Stocks => asset.Amount * asset.StocksAllocation;
            public double Bonds => asset.Amount * asset.BondsAllocation;

            public Asset Post(double amount) => amount > 0
                ? Deposit(asset, amount)
                : Withdraw(asset, Math.Abs(amount));

            private Asset Deposit(double amount) =>
                amount < 0 ? throw new Exception($"Can't deposit negative amount.") :
                new
                (
                    (asset.Amount + amount).RoundToCents(),
                    asset.Allocation
                );

            private Asset Withdraw(double amount) =>
                amount < 0 ? throw new Exception($"Can't withdraw negative amount.") :
                amount > asset.Amount + Precision.Amount ? throw new Exception($"Can't withdraw more than asset balance. | Available: {asset.Amount:C2} | Requested: {amount:C2}") :
                new Asset
                (
                    (asset.Amount - amount).RoundToCents(),
                    asset.Allocation
                );

            public Asset Grow(double stocksROI, double bondsROI)
            {
                var stocksNew = Math.Max(0, asset.Stocks * (1 + stocksROI)).RoundToCents();
                var bondsNew = Math.Max(0, asset.Bonds * (1 + bondsROI)).RoundToCents();

                return new(
                    stocksNew + bondsNew,
                    0 == stocksNew ? 0.0 : 0 == bondsNew ? 1.0 : stocksNew / (stocksNew + bondsNew));
            }

            public Asset Rebalance(double targetAllocation, double maxDrift) => Math.Abs(targetAllocation - asset.Allocation) > maxDrift
                ? new(asset.Amount, targetAllocation)
                : asset;

        }

        private static double RoundToCents(this double amount) => Math.Round(amount, 2);

        extension(Assets assets)
        {
            public Assets Grow(double stocksROI, double bondsROI) => new
            (
                PreTax: assets.PreTax.Grow(stocksROI: stocksROI, bondsROI: bondsROI),
                PostTax: assets.PostTax.Grow(stocksROI: stocksROI, bondsROI: bondsROI),
                Cash: assets.Cash
            );
        }

    }

    internal interface ISimState
    {
        // History
        ReadOnlyMemory<SimYear> PriorYears { get; }
        SimYear PriorYear { get; }

        // Current iteration
        int IterationIndex { get; }
        int YearIndex { get; }
        int Age { get; }
        double PYRunningInflationMultiplier { get; }

    }

    internal interface ISimStateJan1st : ISimState
    {
        Assets Jan { get; }

        Alloc TargetAllocation { get; set; }
        Fees Fees { get; set; }
        Incomes Incomes { get; set; }
        Expenses Expenses { get; set; }
        Withdrawals Withdrawals { get; set; }
    }

    internal interface ISimStateDec31st : ISimState
    {
        Assets Jan { get; }
        Fees Fees { get; }
        Incomes Incomes { get; }
        Expenses Expenses { get; }
        Withdrawals Withdrawals { get; }
        ROI ROI { get; set; }
    }

    internal interface ISimStrategy2
    {
        void Apply(ISimStateJan1st context) { }
        void Apply(ISimStateDec31st context) { }
    }

    internal interface ISimObjective2
    {
        int Order { get => 50; }
        int MaxIterations { get => int.MaxValue; }
        ISimStrategy2 CreateStrategy(int iterationIndex);
    }

    public readonly record struct Alloc(double Allocation, double MaxDrift);

    internal sealed record class SimState(int IterationIndex, int StartAge, Assets Initial, Memory<SimYear> Storage) :ISimState, ISimStateJan1st, ISimStateDec31st
    {
        public ReadOnlyMemory<SimYear> PriorYears => Storage.Slice(0, YearIndex);
        public SimYear PriorYear => YearIndex > 0 ? PriorYears.Span[^1] : new();

        public int YearIndex { get; private set; } = 0;
        public int Age => StartAge + YearIndex;
        public Assets Jan { get; private set; } = Initial;
        public double PYRunningInflationMultiplier => 0 == YearIndex ? 1.0 : PriorYear.RunningInflationMultiplier;
        public Alloc TargetAllocation { get; set; }
        public Fees Fees { get; set; }
        public Incomes Incomes { get; set; }
        public Expenses Expenses { get; set; }
        public Withdrawals Withdrawals { get; set; }
        public ROI ROI { get; set; }


        public void BeginYear(int yearIndex)
        {
            YearIndex = yearIndex;
            Jan = (yearIndex == 0) ? Initial : PriorYear.Dec;
            (Fees, Incomes, Expenses, Withdrawals, ROI) = (default, default, default, default, default);
        }

    }
}
