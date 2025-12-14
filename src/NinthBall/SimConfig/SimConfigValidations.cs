
using System.ComponentModel.DataAnnotations;

namespace NinthBall
{
    public partial record SimConfig
    {
        public SimConfig ThrowIfInvalid()
        {
            var results = this.TryValidate();
            if (results.Count > 0)
            {
                var warningMsg = string.Join(Environment.NewLine, results.Select(x => x.ErrorMessage));
                throw new FatalWarning(warningMsg);
            }

            return this;
        }

        public IList<ValidationResult> TryValidate()
        {
            var results = new List<ValidationResult>();

            OnlyOne("Living expenses", LivingExpenses, PrecalculatedLivingExpenses);

            //if (null != PreTaxWithdrawal && null != PrecalculatedWithdrawal) results.Add(new("Specify ONLY ONE withdrawal strategy"));
            //if (null == PreTaxWithdrawal && null == PrecalculatedWithdrawal) results.Add(new("Specify at least ONE withdrawal strategy"));
            //if (null != FlatGrowth && null != HistoricalGrowth) results.Add(new("Specify ONLY ONE Growth strategy"));
            //if (null == FlatGrowth && null == HistoricalGrowth) results.Add(new("Specify at least ONE Growth strategy"));
            
            Validator.TryValidateObject(this, new ValidationContext(this), results, validateAllProperties: true);

            return results;

            static void OnlyOne(string name, object obj1, object obj2)
            {
                if (obj1 is not null && obj2 is not null) throw new Exception($"{name} - Specify only one.");
                if (obj1 is null && obj2 is null) throw new Exception($"{name} - Specify atleast one.");
            }


        }
    }
}
