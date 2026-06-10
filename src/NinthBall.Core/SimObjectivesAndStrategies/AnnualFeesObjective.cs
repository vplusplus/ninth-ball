
namespace NinthBall.Core
{
    [StrategyFamily(StrategyFamily.Fees)]
    sealed class AnnualFeesObjective(AnnualFees F) : ISimObjective, ISimStrategy
    {
        ISimStrategy ISimObjective.CreateStrategy(int iterationIndex) => this;

        void ISimStrategy.Apply(ISimState context)
        {
            var preTxFees = Math.Round(context.Jan.PreTax.Amount * F.PreTax);
            var postTaxFees = Math.Round(context.Jan.PostTax.Amount * F.PostTax);
            var feesPct = (preTxFees + postTaxFees) / (context.Jan.PreTax.Amount + context.Jan.PostTax.Amount + 0.01);

            // Calculate fees
            context.Fees = new(
                Math.Round(context.Jan.PreTax.Amount  * F.PreTax),
                Math.Round(context.Jan.PostTax.Amount * F.PostTax),
                feesPct
            );
        }

        public override string ToString() => $"Annual fees | PreTax: {F.PreTax:P1} | PostTax: {F.PostTax:P1} | Cash: None";
    }


    [StrategyFamily(StrategyFamily.Fees)]
    sealed class FidelityWealthManagementFeesObjective : ISimObjective, ISimStrategy
    {
        ISimStrategy ISimObjective.CreateStrategy(int iterationIndex) => this;

        void ISimStrategy.Apply(ISimState context)
        {
            var preTaxAmount  = Math.Max(0, context.Jan.PreTax.Amount);
            var postTaxAmount = Math.Max(0, context.Jan.PostTax.Amount);
            var totalAssets   = preTaxAmount + postTaxAmount;

            if (totalAssets.AlmostZero(Precision.Amount))
            {
                context.Fees = new(
                    0.0,
                    0.0,
                    0.0
                );
            }
            else
            {
                var totalFees   = CalculateTieredFee(totalAssets, FidelityFeeTiers);
                var preTaxFees  = Math.Round(totalFees * preTaxAmount / totalAssets);
                var postTaxFees = Math.Round(totalFees * postTaxAmount / totalAssets);
                var feesPct     = (preTaxFees + postTaxFees) / (totalAssets + 0.01);

                context.Fees = new(
                    preTaxFees,
                    postTaxFees,
                    feesPct
                );
            }
        }

        public override string ToString() => $"Annual fees | Fidelity Wealth Management fees | 1.25% to 0.50%";

        public record FeeTier(double Amount, double FeeRate);

        // TODO: Move to configuration
        static readonly IReadOnlyList<FeeTier>  FidelityFeeTiers = new List<FeeTier>
        {
            new(         500_000,   0.0125),
            new(         500_000,   0.0110),
            new(       1_000_000,   0.0090),
            new(       3_000_000,   0.0070),
            new( double.MaxValue,   0.0050)
        };

        public static double CalculateTieredFee(double portfolioBalance, IReadOnlyList<FeeTier> tiers)
        {
            double remaining = portfolioBalance;
            double totalFee = 0;

            foreach (var tier in tiers)
            {
                if (remaining <= 0) break;

                double appliedAmount = Math.Min(remaining, tier.Amount);
                totalFee += appliedAmount * tier.FeeRate;
                remaining -= appliedAmount;
            }

            return totalFee;
        }

    }
}
