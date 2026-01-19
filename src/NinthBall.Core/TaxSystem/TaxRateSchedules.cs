
using Microsoft.Extensions.Configuration;

namespace NinthBall.Core
{
    /// <summary>
    /// Describes the tiered tax rate schedule.
    /// </summary>
    public sealed record TaxRateSchedule(IReadOnlyList<TaxRateSchedule.TaxBracket> Brackets)
    {
        public readonly record struct TaxBracket(double IncomeThreshold, double MarginalRate);
    }
    
    /// <summary>
    /// Known tax schedules. 
    /// TODO: Use Lazy pattern, read from optional-config, fall back to hardcoded defaults
    /// </summary>
    public static class TaxRateSchedules
    {
        static readonly Lazy<TaxRateSchedule> LazyFederal2026Joint      = new (() => FromConfigOrDefault("Federal2026Joint", FallbackFederal2026Joint!));
        static readonly Lazy<TaxRateSchedule> LazyFederalLTCG2026Joint  = new (() => FromConfigOrDefault("FederalLTCG2026Joint", FallbackFederalLTCG2026Joint!));
        static readonly Lazy<TaxRateSchedule> LazyNJ2026Joint           = new(() => FromConfigOrDefault("NJ2026Joint", FallbackNJ2026Joint!));

        public static TaxRateSchedule Federal => LazyFederal2026Joint.Value;
        public static TaxRateSchedule FederalLTCG => LazyFederalLTCG2026Joint.Value;
        public static TaxRateSchedule State => LazyNJ2026Joint.Value;

        /// <summary>
        /// Represents a tax schedule that uses single tax rate.
        /// </summary>
        public static TaxRateSchedule Flat(double taxRate) => new([new(0, taxRate)]);


        // TODO: Public properties should drop 2026 suffix. Private fall backs and defaults should carry year suffix
        public static double FedStdDeduction2026 => Config.GetValue("FedStdDeduction2026", 32200.0);

        // TODO: Probably doesn't belong here. Should move to state specific calculators.
        public static double NJPersonalExemption2026 => Config.GetValue("NJPersonalExemption2026", 1500);


        //......................................................................
        #region Fallback tax rate schedules if not externally configured.
        //......................................................................
        static TaxRateSchedule FromConfigOrDefault(string sectionName, TaxRateSchedule fallbackSchedule)
        {
            var configSection = Config.Current.GetSection(sectionName);
            var exists = null != configSection && configSection.Exists();
            var schedule = exists ? configSection!.Get<TaxRateSchedule>() : fallbackSchedule;
            if (null == schedule || null == schedule.Brackets || 0 == schedule.Brackets.Count) throw new FatalWarning($"Invalid TaxRateSchedule | Zero brackets | '{sectionName}'");
            return schedule;
        }

        /// <summary>
        /// 2026 Federal Income Tax Brackets for Married Filing Jointly.
        /// Values updated per IRS 2026 inflation adjustments.
        /// </summary>
        static readonly TaxRateSchedule FallbackFederal2026Joint = new
        ([
            new (0, 0.10),
            new (24800, 0.12),
            new (100800, 0.22),
            new (211400, 0.24),
            new (403550, 0.32),
            new (512450, 0.35),
            new (768700, 0.37)
        ]);

        /// <summary>
        /// 2026 Long-Term Capital Gains Brackets for Married Filing Jointly.
        /// </summary>
        public static readonly TaxRateSchedule FallbackFederalLTCG2026Joint = new
        ([
            new (0, 0.0),
            new (98900, 0.15),   // Updated from 89,250
            new (613700, 0.20)   // Updated from 553,850
        ]);

        /// <summary>
        /// 2026 New Jersey Gross Income Tax Brackets for Married Filing Jointly.
        /// </summary>
        public static readonly TaxRateSchedule FallbackNJ2026Joint = new
        ([
            new (0, 0.014),
            new (20000, 0.0175),
            new (50000, 0.0245),
            new (70000, 0.035),
            new (80000, 0.05525),
            new (150000, 0.0637),
            new (500000, 0.0897),
            new (1000000, 0.1075)
        ]);

        #endregion
    }

}
