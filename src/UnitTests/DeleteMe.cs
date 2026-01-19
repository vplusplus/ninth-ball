

namespace UnitTests
{
    public readonly record struct Taxes(Taxes.Inc GrossIncome, Taxes.TD Federal, Taxes.TD State)
    {
        public readonly record struct Inc(double OrdInc, double INT, double DIV, double LTCG)
        {
            public readonly double Total => OrdInc + INT + DIV + LTCG;
        }

        public readonly record struct TR(double OrdInc, double LTCG);

        public readonly record struct TD(double Deduction, double Taxable, TR MarginalRate, double Tax)
        {
            public readonly double EffectiveRate => Taxable < 0.01 ? 0.0 : Tax / Taxable;
        }

        public readonly double Total => Federal.Tax + State.Tax;

        public readonly double EffectiveRate => GrossIncome.Total < 0.01 ? 0.0 : Total / GrossIncome.Total;
    }


}

