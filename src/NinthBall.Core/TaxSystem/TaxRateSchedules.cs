
using System.ComponentModel.DataAnnotations;

namespace NinthBall.Core
{
    // Describes the tiered tax rate schedule.
    public sealed record TaxRateSchedule
    (
        double TaxDeductions,

        [property: ValidateNested]
        IReadOnlyList<TaxRateSchedule.TaxBracket> Brackets
    )
    {
        public readonly record struct TaxBracket
        (
            [property: Min(0)] double Threshold,
            [property: Range(0.0, 0.75)] double MTR
        );
    }

    public sealed record TaxRateSchedules
    (
        [property: ValidateNested] TaxRateSchedule Federal,
        [property: ValidateNested] TaxRateSchedule LTCG,
        [property: ValidateNested] TaxRateSchedule State
    )
    {
        // Represents a tax schedule that uses single tax rate.
        public static TaxRateSchedule Flat(double marginalTaxRate, double taxDeductions) => new
        (
            TaxDeductions:  taxDeductions,
            Brackets:       [new(0, marginalTaxRate)]
        );
    }

}
