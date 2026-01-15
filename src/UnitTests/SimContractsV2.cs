
//namespace NinthBall.Core
//{
//    internal static class AssetExtensionsV2
//    {
//        extension(Asset asset)
//        {
//            public double StocksAllocation => asset.Allocation;
//            public double BondsAllocation => 1 - asset.Allocation;
//            public double Stocks => asset.Amount * asset.StocksAllocation;
//            public double Bonds => asset.Amount * asset.BondsAllocation;

//            public Asset Post(double amount) => amount > 0
//                ? Deposit(asset, amount)
//                : Withdraw(asset, Math.Abs(amount));

//            private Asset Deposit(double amount) =>
//                amount < 0 ? throw new Exception($"Can't deposit negative amount.") :
//                new
//                (
//                    (asset.Amount + amount).RoundToCents(),
//                    asset.Allocation
//                );

//            private Asset Withdraw(double amount) =>
//                amount < 0 ? throw new Exception($"Can't withdraw negative amount.") :
//                amount > asset.Amount + Precision.Amount ? throw new Exception($"Can't withdraw more than asset balance. | Available: {asset.Amount:C2} | Requested: {amount:C2}") :
//                new Asset
//                (
//                    (asset.Amount - amount).RoundToCents(),
//                    asset.Allocation
//                );

//            public Asset Grow(double stocksROI, double bondsROI)
//            {
//                var stocksNew = Math.Max(0, asset.Stocks * (1 + stocksROI)).RoundToCents();
//                var bondsNew = Math.Max(0, asset.Bonds * (1 + bondsROI)).RoundToCents();

//                return new(
//                    stocksNew + bondsNew,
//                    0 == stocksNew ? 0.0 : 0 == bondsNew ? 1.0 : stocksNew / (stocksNew + bondsNew));
//            }

//            public Asset Rebalance(double targetAllocation, double maxDrift) => Math.Abs(targetAllocation - asset.Allocation) > maxDrift
//                ? new(asset.Amount, targetAllocation)
//                : asset;

//        }

//        private static double RoundToCents(this double amount) => Math.Round(amount, 2);

//        extension(Assets assets)
//        {
//            public Assets Grow(double stocksROI, double bondsROI) => new
//            (
//                PreTax: assets.PreTax.Grow(stocksROI: stocksROI, bondsROI: bondsROI),
//                PostTax: assets.PostTax.Grow(stocksROI: stocksROI, bondsROI: bondsROI),
//                Cash: assets.Cash
//            );
//        }

//    }

//    internal interface ISimStateJan1st
//    {
//        // View of the world on Jan 1st
//        // Mutable strategy recommondations on Jan 1st 
//        // Zero exposure to future
//    }

//    internal interface ISimStateDec31st
//    {
//        // View of the world on Dec 31st 
//        // Mutable market performance (ROI, Inflation rate, etc.)
//        // Immutable past including choices finalized on Jan 1st
//    }

//    internal interface ISimStrategy2
//    {
//        void Apply(ISimStateJan1st context) { }
//        void Apply(ISimStateDec31st context) { }
//    }

//    internal interface ISimObjective2
//    {
//        int Order { get => 50; }
//        int MaxIterations { get => int.MaxValue; }
//        ISimStrategy2 CreateStrategy(int iterationIndex);
//    }




//}
