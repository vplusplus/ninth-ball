
using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.EMMA;
using DocumentFormat.OpenXml.Spreadsheet;
using NinthBall.Core;

namespace NinthBall
{
    internal static class ExcelOutput
    {
        readonly record struct SID(
            uint C0, uint C1, 
            uint P0, uint P1,
            uint BRC0, uint BRC1,
            uint BRP0, uint BRP1
        );

        // For annualization
        const double InflationRate = 0.03;

        public static async Task Generate(SimResult simResult, string excelFileName)
        {
            ArgumentNullException.ThrowIfNull(simResult);
            ArgumentNullException.ThrowIfNull(excelFileName);

            var stylesheet = ExcelStylesheet
                .Empty()
                .WithDefaultStyles(out var defaultStyles);

            var myStyles = new SID
            (
                // Default defaultStyles
                C0: defaultStyles.C0,
                C1: defaultStyles.C1,
                P0: defaultStyles.P0,
                P1: defaultStyles.P1,

                // Black and Red
                BRC0: stylesheet.GetOrAddCellFormat("$#,##0;[Red]-$#,##0;;"),
                BRC1: stylesheet.GetOrAddCellFormat("$#,##0.0;[Red]-$#,##0.0;;"),
                BRP0: stylesheet.GetOrAddCellFormat("#0%;[Red]-#0%;;"),
                BRP1: stylesheet.GetOrAddCellFormat("#0.0%;[Red]-#0.0%;;")
            );

            using(var xl = new ExcelWriter(excelFileName, stylesheet))
            {
                // Render chosen strategies and related assumptions.
                RenderStrategy(xl, simResult);

                // Render Summary sheet
                RenderSummary(xl, myStyles, simResult);

                // Render one sheet per percentile
                foreach(var pctl in Percentiles.Items.OrderByDescending(x => x.Pctl))
                {
                    RenderPercentile(xl, myStyles, simResult, pctl);
                }

                // NOTE: Dispose will not save. We have to save.
                xl.Save();
            }
        }

        static void RenderStrategy(ExcelWriter xl, SimResult simResult)
        {
            using (var sheet = xl.BeginSheet("Strategy"))
            {
                sheet.WriteColumns(20, 120);

                var firstYear = simResult.Iterations[0].ByYear.Span[0];

                using (var rows = sheet.BeginSheetData())
                {
                    rows.BeginRow().EndRow();

                    rows
                        .BeginRow()
                        .Append("PreTax (401K)")
                        .Append($"{firstYear.Jan.PreTax.Amount/1000000:C2} M")
                        .EndRow();

                    rows
                        .BeginRow()
                        .Append("PostTax")
                        .Append($"{firstYear.Jan.PostTax.Amount/1000000:C2} M")
                        .EndRow();

                    rows
                        .BeginRow()
                        .Append("Allocation")
                        .Append($"{firstYear.Jan.PreTax.Allocation:P0} - {1 - firstYear.Jan.PreTax.Allocation:P0}")
                        .EndRow();

                    rows
                        .BeginRow()
                        .Append("Horizon")
                        .Append($"{simResult.NoOfYears} years")
                        .EndRow();

                    rows
                        .BeginRow()
                        .Append("Simulation")
                        .Append($"{simResult.Iterations.Count:#,0} iterations")
                        .EndRow();

                    rows.BeginRow().EndRow();

                    foreach (var desc in simResult.Strategies)
                    {
                        var parts = desc.Split('|', 2);

                        var category = (2 == parts.Length ? parts[0] : string.Empty)?.Trim() ?? string.Empty;
                        var strategy = (2 == parts.Length ? parts[1] : desc)?.Trim() ?? string.Empty;

                        rows
                            .BeginRow()
                            .Append(category)
                            .Append(strategy)
                            .EndRow();
                    }
                }
            }
        }

        static void RenderSummary(ExcelWriter xl, SID styles, SimResult simResult)
        {
            const double W10 = 10;
            const double W20 = 20;

            using (var sheet = xl.BeginSheet("Summary"))
            {
                sheet.WriteColumns(W20, W10, W10, W10, W10, W10, W10, W10, W20);

                using (var rows = sheet.BeginSheetData())
                {
                    using (var row = rows.BeginRow())
                    {
                        row.Append("Survival rate");
                        row.Append(simResult.SurvivalRate, styles.P0);
                    }

                    using (var row = rows.BeginRow())
                    {
                        // Empty row
                    }

                    using (var row = rows.BeginRow())
                    {
                        row.Append("");
                        foreach(var pctl in Percentiles.Items) row.Append(pctl.Tag);
                        row.Append("");
                    }

                    using (var row = rows.BeginRow())
                    {
                        row.Append("Percentiles");
                        foreach (var pctl in Percentiles.Items) row.Append(pctl.Caption);
                        row.Append(" percentile");
                    }

                    using (var row = rows.BeginRow())
                    {
                        row.Append("End (Real)");
                        foreach (var pctl in Percentiles.Items)
                        {
                            var p = simResult.Percentile(pctl.Pctl);
                            var m = p.EndingBalance.InflationAdjustedValue(InflationRate, p.SurvivedYears).Mil();
                            row.Append(m, styles.BRC1);
                        }
                        row.Append(" millions");
                    }

                    using (var row = rows.BeginRow())
                    {
                        row.Append("End (Nominal)");
                        foreach (var pctl in Percentiles.Items)
                        {
                            var p = simResult.Percentile(pctl.Pctl);
                            var m = p.EndingBalance.Mil();
                            row.Append(m, styles.BRC1);
                        }
                        row.Append(" millions");
                    }

                    using (var row = rows.BeginRow())
                    {
                        row.Append("Change (Annualized)");
                        foreach (var pctl in Percentiles.Items)
                        {
                            var p = simResult.Percentile(pctl.Pctl);
                            var chng = AnnualizeChangePCT(p.ByYear);
                            row.Append(chng, styles.BRP1);
                        }
                        row.Append("");
                    }
                }
            }
        }

        static void RenderPercentile(ExcelWriter xl, SID styles, SimResult simResult, Percentiles.PCT pctl)
        {
            const double Blank = 3;
            const double W4 = 4;
            const double W6 = 6;
            const double W10 = 10;
            const double W12 = 12;

            var sheetName = $"{pctl.Caption}-{pctl.Tag}";
            var p = simResult.Percentile(pctl.Pctl);

            using (var sheet = xl.BeginSheet(sheetName))
            {
                sheet.WriteColumns(
                    W4, W4,                             // Year, Age
                    W12, W6, W12, W6, W12, Blank,       // Jan & Fees
                    W10, W10, W10, W10, W10, Blank,     // SS, ANN, 4K, Inv, Cash
                    W10, W10, Blank,                    // Tax, Exp
                    W12, W12, W12, W12, Blank,          // Change & Deposits
                    W12, W12, Blank,                    // Dec
                    W6, W10, W10                        // ROI-History
                );

                using (var rows = sheet.BeginSheetData())
                {
                    // Header
                    using (var row = rows.BeginRow())
                    {
                        row
                            .Append("Year")
                            .Append("Age")

                            .Append("Jan-401K")     // Jan, alloc and less fees
                            .Append("Alloc")
                            .Append("Jan-Inv")
                            .Append("Alloc")
                            .Append("Fees")
                            .Append("")

                            .Append("SS")           // Incomes & Withdrawals (inflow)
                            .Append("ANN")
                            .Append("401K")
                            .Append("Inv")
                            .Append("Cash")
                            .Append("")

                            .Append("PYTax")        // Expenses (outflow)
                            .Append("CYExp")
                            .Append("")

                            .Append("ROI-401K")     // ROI-Change and excess deposits
                            .Append("ROI-Inv")
                            .Append("Deposits-Inv")
                            .Append("Deposits-Cash")
                            .Append("")

                            .Append("Dec-401K")     // December
                            .Append("Dec-Inv")
                            .Append("")

                            .Append("Like")         // ROI History
                            .Append("ROI Stocks")
                            .Append("ROI Bonds")
                            ;

                    }

                    foreach(var y in p.ByYear.Span)
                    {
                        double fromPreTax = y.Withdrawals.PreTax * -1;
                        double fromPostTax = y.Deposits.PostTax - y.Withdrawals.PostTax;
                        double fromCash = y.Deposits.Cash - y.Withdrawals.Cash;

                        using (var row = rows.BeginRow())
                        {
                            row
                                .Append(y.Year + 1)
                                .Append(y.Age)

                                // Jan, Alloc and Fees
                                .Append(y.Jan.PreTax.Amount, styles.BRC0)
                                .Append(y.Jan.PreTax.Allocation, styles.P0)
                                .Append(y.Jan.PostTax.Amount, styles.BRC0)
                                .Append(y.Jan.PostTax.Allocation, styles.P0)
                                .Append(y.Fees.Total(), styles.BRC0)
                                .Append("")

                                // Incomes & withdrawals
                                .Append(y.Incomes.SS, styles.BRC0)
                                .Append(y.Incomes.Ann, styles.BRC0)
                                .Append(y.Withdrawals.PreTax, styles.BRC0)
                                .Append(y.Withdrawals.PostTax, styles.BRC0)
                                .Append(y.Withdrawals.Cash, styles.BRC0)
                                .Append("")

                                // Expenses
                                .Append(y.Expenses.PYTax, styles.BRC0)
                                .Append(y.Expenses.CYExp, styles.BRC0)
                                .Append("")

                                // Change in value and excess deposits
                                .Append(y.Change.PreTax, styles.BRC0)
                                .Append(y.Change.PostTax, styles.BRC0)
                                .Append(y.Deposits.PostTax, styles.BRC0)
                                .Append(y.Deposits.Cash, styles.BRC0)
                                .Append("")

                                .Append(y.Dec.PreTax.Amount, styles.BRC0)
                                .Append(y.Dec.PostTax.Amount, styles.BRC0)
                                .Append("")

                                .Append(y.ROI.LikeYear)
                                .Append(y.ROI.StocksROI, styles.BRP1)
                                .Append(y.ROI.BondsROI, styles.BRP1)
                                ;
                        }
                    }
                }
            }
        }

        static double AnnualizeChangePCT(ReadOnlyMemory<SimYear> byYear)
        {
            double compoundReturn = 1;
            int count = 0;

            for (int i = 0; i < byYear.Length; i++)
            {
                var r = byYear.Span[i].ChangePCT;
                checked { compoundReturn *= (1 + r); }
                count++;
            }

            return 0 == count ? 0.0 : Math.Pow(compoundReturn, 1.0 / count) - 1;
        }

        static double Mil(this double value) => value / 1000000.0;
    }
}

