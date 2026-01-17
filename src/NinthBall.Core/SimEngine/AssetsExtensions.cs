
namespace NinthBall.Core
{
    /// <summary>
    /// Provides transactional semantics on Assets
    /// </summary>
    internal static class AssetsExtensions
    {
        extension(Asset asset)
        {
            // Stocks allocation
            public double StocksAllocation => asset.Allocation;

            // Bonds allocation
            public double BondsAllocation => 1 - asset.Allocation;

            // Stocks portion of the asset
            public double StocksAmount => asset.Amount * asset.StocksAllocation;

            // Bonds portion of the asset.
            public double BondsAmount => asset.Amount * asset.BondsAllocation;

            // Applies withdrawal or deposit amount.
            public Asset Post(double amount) => amount == 0.0 ? asset : amount > 0.0 ? Deposit(asset, amount) : Withdraw(asset, Math.Abs(amount));

            // Adds suggested amount to the asset. Allocation remains the same.
            Asset Deposit(double amount) =>
                amount < 0.0 ? throw new Exception("Deposit amount must be positive") :
                new(
                    (asset.Amount + amount).RoundToCents(),
                    asset.Allocation
                );

            // Reduces suggested amount from the asset. Allocation remains the same.
            Asset Withdraw(double amount) =>
                amount < 0.0 ? throw new Exception("Withdrawal amount must be positive") :
                amount > asset.Amount + Precision.Amount ? throw new Exception($"Can't withdraw more than asset balance. | Available: {asset.Amount:C2} | Requested: {amount:C2}") :
                new Asset
                (
                    (asset.Amount - amount).RoundToCents(),
                    asset.Allocation
                );

            // Applies suggested ROI to the stocks and bonds. Allocation might change.
            public Asset Grow(double stocksROI, double bondsROI)
            {
                var stocksNew = Math.Max(0, asset.StocksAmount * (1 + stocksROI)).RoundToCents();
                var bondsNew  = Math.Max(0, asset.BondsAmount  * (1 + bondsROI)).RoundToCents();

                return new Asset
                (
                    stocksNew + bondsNew,
                    0 == stocksNew ? 0.0 : 0 == bondsNew ? 1.0 : stocksNew / (stocksNew + bondsNew)
                );
            }

            // Changes the Stocks vs Bonds allocation of the asset.
            public Asset Rebalance(double targetAllocation, double maxDrift) => Math.Abs(targetAllocation - asset.Allocation) > maxDrift
                ? new(asset.Amount, targetAllocation)
                : asset;
        }

        extension (Assets assets)
        {
            public Assets Withdraw(Withdrawals withdrawals) => new
            (
                PreTax:  assets.PreTax.Post(-withdrawals.PreTax),
                PostTax: assets.PostTax.Post(-withdrawals.PostTax),
                Cash:    assets.Cash.Post(-withdrawals.Cash)
            );

            public Assets Deposit(Deposits deposits) => new
            (
                PreTax:  assets.PreTax,
                PostTax: assets.PostTax.Post(deposits.PostTax),
                Cash:    assets.Cash.Post(deposits.Cash)
            );

            public Assets Rebalance(Allocation targetAllocation) => default == targetAllocation ? assets : new
            (
                assets.PreTax.Rebalance(targetAllocation.PreTax.Allocatin, targetAllocation.PreTax.MaxDrift),
                assets.PostTax.Rebalance(targetAllocation.PostTax.Allocatin, targetAllocation.PostTax.MaxDrift),
                assets.Cash
            );

            public Assets ApplyFees(Fees fees) => new
            (
                PreTax:  assets.PreTax.Post(-fees.PreTax),
                PostTax: assets.PostTax.Post(-fees.PostTax),
                Cash:    assets.Cash.Post(-fees.Cash)
            );

            public Assets Grow(ROI roi, out Change change)
            {
                var newPreTax  = assets.PreTax.Grow(stocksROI: roi.StocksROI, bondsROI: roi.BondsROI);
                var newPostTax = assets.PostTax.Grow(stocksROI: roi.StocksROI, bondsROI: roi.BondsROI);

                var changePreTax  = newPreTax.Amount  - assets.PreTax.Amount;
                var changePostTax = newPostTax.Amount - assets.PostTax.Amount;

                change = new(changePreTax, changePostTax, 0.0);
                //effectiveROI = (changePreTax + changePostTax) / (assets.PreTax.Amount + assets.PostTax.Amount);

                return new(newPreTax, newPostTax, assets.Cash);
            }
        }

        // Drops the fractional cents (double-precision-dust)
        private static double RoundToCents(this double amount) => Math.Round(amount, 2);
    }
}
