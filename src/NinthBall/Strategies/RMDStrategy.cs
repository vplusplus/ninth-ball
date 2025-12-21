namespace NinthBall
{
    [SimInput(typeof(RMDStrategy), typeof(RMD), Family = StrategyFamily.WithdrawalAdjustment)]
    sealed class RMDStrategy(RMD Options) : ISimObjective
    {
        int ISimObjective.Order => 29;

        ISimStrategy ISimObjective.CreateStrategy(int iterationIndex) => new Strategy(Options);

        sealed class Strategy(RMD R) : ISimStrategy
        {
            void ISimStrategy.Apply(ISimContext context)
            {
                if (context.Age < R.StartAge) return;

                double factor = GetRMDFactor(context.Age);
                
                // Validation: If we are at or past StartAge but have no table data, we must fail.
                if (factor <= 0) 
                {
                    throw new InvalidOperationException($"RMD table data missing for age {context.Age}. Please update the IRS Uniform Lifetime Table in {nameof(RMDStrategy)}.");
                }

                double requiredRMD = context.PreTaxBalance.Amount / factor;

                // Adjust up if current withdrawal is less than RMD
                if (context.Withdrawals.PreTax < requiredRMD)
                {
                    context.Withdrawals = context.Withdrawals with
                    {
                        PreTax = requiredRMD
                    };
                }
            }

            private static double GetRMDFactor(int age)
            {
                if (age < 70) return 0;
                if (age >= 115) return 2.9;

                return age switch
                {
                    70 => 29.1, 71 => 28.2, 72 => 27.4, 73 => 26.5, 74 => 25.5, 75 => 24.6, 76 => 23.7, 77 => 22.9, 78 => 22.0, 79 => 21.1,
                    80 => 20.2, 81 => 19.4, 82 => 18.5, 83 => 17.7, 84 => 16.8, 85 => 16.0, 86 => 15.2, 87 => 14.4, 88 => 13.7, 89 => 12.9,
                    90 => 12.2, 91 => 11.5, 92 => 10.8, 93 => 10.1, 94 => 9.5, 95 => 8.9, 96 => 8.4, 97 => 7.8, 98 => 7.3, 99 => 6.8,
                    100 => 6.4, 101 => 6.0, 102 => 5.6, 103 => 5.2, 104 => 4.9, 105 => 4.6, 106 => 4.3, 107 => 4.1, 108 => 3.9, 109 => 3.7,
                    110 => 3.5, 111 => 3.4, 112 => 3.3, 113 => 3.1, 114 => 3.0,
                    _ => 0
                };
            }
        }

        public override string ToString() => $"RMD | Starting at age {Options.StartAge}";
    }
}
