

using DocumentFormat.OpenXml.Drawing.Charts;
using NinthBall.Core;

namespace UnitTests
{
    [TestClass]

    public class AssetChangeTests
    {

        [TestMethod]
        public void PostTest()
        {
            var a = new Asset(1000, 0.6);
            Print(a);

            a = a.Rebalance(0.7, 0.05);
            Print(a);

            a = a.Post(100);
            Print(a);

            a = a.Post(-100);
            Print(a);

            static void Print(Asset a)
            {
                Console.WriteLine($"Stocks: {a.Stocks:C2} Bonds: {a.Bonds:C2} Alloc: {a.Allocation:P0}");
            }

        }

    }


    internal static class AssetExtensions
    {
        extension(Asset asset)
        {
            public double StocksAllocation => asset.Allocation;
            public double BondsAllocation => 1 - asset.Allocation;
            public double Stocks => asset.Amount * asset.StocksAllocation;
            public double Bonds => asset.Amount * asset.BondsAllocation;

            public Asset Post(double amount)  => amount > 0 
                ? Deposit(asset, amount) 
                : Withdraw(asset,Math.Abs(amount));

            private Asset Deposit(double amount) => 
                amount < 0 ? throw new Exception($"Can't deposit negative amount.") :
                new
                (
                    ResetNearZeroAndDropFractionalCents(asset.Amount + amount),
                    asset.Allocation
                );

            private Asset Withdraw(double amount) =>
                amount < 0 ? throw new Exception($"Can't withdraw negative amount.") :
                amount > asset.Amount + Precision.Amount ? throw new Exception($"Can't withdraw more than asset balance. | Available: {asset.Amount:C2} | Requested: {amount:C2}") : 
                new Asset
                (
                    ResetNearZeroAndDropFractionalCents(asset.Amount - amount),
                    asset.Allocation
                );

            public Asset Grow(double stocksROI, double bondsROI)
            {
                var stocksNew = ResetNearZeroAndDropFractionalCents(Math.Max(0, asset.Stocks * (1 + stocksROI)));
                var bondsNew  = ResetNearZeroAndDropFractionalCents(Math.Max(0, asset.Bonds  * (1 + bondsROI)));

                return new( 
                    stocksNew + bondsNew, 
                    0 == stocksNew ? 0.0 : 0 == bondsNew ? 1.0 : stocksNew / (stocksNew + bondsNew + Precision.Rate));
            }

            public Asset Rebalance(double targetAllocation, double maxDrift) => Math.Abs(targetAllocation - asset.Allocation) > maxDrift
                ? new(asset.Amount, targetAllocation)
                : asset;

            private static double ResetNearZeroAndDropFractionalCents(double amount) => Math.Round(amount.ResetNearZero(Precision.Amount), 2);

        }
    
        extension(Assets assets)
        {
            public Assets Grow(double stocksROI, double bondsROI) => new 
            (
                PreTax:  assets.PreTax.Grow(  stocksROI: stocksROI, bondsROI: bondsROI ),
                PostTax: assets.PostTax.Grow( stocksROI: stocksROI, bondsROI: bondsROI ),
                Cash:    assets.Cash
            );
        } 
    
    }
}
