
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
    /// Identifies the type of tax schedule for DI and resolution.
    /// </summary>
    public enum TaxScheduleKind { Federal, LTCG, State }

    /// <summary>
    /// Known tax schedules and fallbacks.
    /// </summary>
    public static class TaxRateSchedules
    {
        /// <summary>
        /// Represents a tax schedule that uses single tax rate.
        /// </summary>
        public static TaxRateSchedule Flat(double taxRate) => new([new(0, taxRate)]);

        //......................................................................
        #region Default/Fallback tax rate schedules
        //......................................................................

        public static TaxRateSchedule FromConfigOrDefault(string sectionName, TaxRateSchedule fallbackSchedule)
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
        public static readonly TaxRateSchedule FallbackFed2026 = new
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
        public static readonly TaxRateSchedule FallbackFedLTCG2026 = new
        ([
            new (0, 0.0),
            new (98900, 0.15),   // Updated from 89,250
            new (613700, 0.20)   // Updated from 553,850
        ]);

        /// <summary>
        /// 2026 New Jersey Gross Income Tax Brackets for Married Filing Jointly.
        /// </summary>
        public static readonly TaxRateSchedule FallbackNJ2026 = new
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
