using NinthBall.Core;

namespace NinthBall
{
    internal static class ExcelOutput
    {
        // For annualization
        const double InflationRate = 0.03;

        public static async Task Generate(SimResult simResult, string excelFileName)
        {
            ArgumentNullException.ThrowIfNull(simResult);
            ArgumentNullException.ThrowIfNull(excelFileName);

            var stylesheet = ExcelStylesheet
                .Empty()
                .WithDefaultStyles(out var styles);

            using(var xl = new ExcelWriter(excelFileName, stylesheet))
            {
                // Render Summary sheet
                RenderSummary(xl, styles, simResult);

                // Render one sheet per percentile
                foreach(var pctl in Percentiles.Items)
                {
                    RenderPercentile(xl, styles, simResult, pctl);
                }

                // NOTE: Dispose will not save. We have to save.
                xl.Save();
            }
        }

        static void RenderSummary(ExcelWriter xl, StyleIDs styles, SimResult simResult)
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
                        row.Append("");
                        foreach(var pctl in Percentiles.Items) row.Append(pctl.Tag);
                        row.Append("");
                    }

                    using (var row = rows.BeginRow())
                    {
                        row.Append("Percentiles");
                        foreach (var pctl in Percentiles.Items) row.Append(pctl.Pctl, styles.P0);
                        row.Append(" th-percentile");
                    }

                    using (var row = rows.BeginRow())
                    {
                        row.Append("End (Real)");
                        foreach (var pctl in Percentiles.Items)
                        {
                            var p = simResult.Percentile(pctl.Pctl);
                            var m = p.EndingBalance.InflationAdjustedValue(InflationRate, p.SurvivedYears).Mil();
                            row.Append(m, styles.C1);
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
                            row.Append(m, styles.C1);
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
                            row.Append(chng, styles.P1);
                        }
                        row.Append(" PCT");
                    }
                }
            }
        }

        static void RenderPercentile(ExcelWriter xl, StyleIDs styles, SimResult simResult, Percentiles.PCT pctl)
        {
            const double W4 = 4;
            const double W6 = 6;
            const double W8 = 8;
            const double W10 = 10;
            const double W12 = 12;

            var p = simResult.Percentile(pctl.Pctl);

            using (var sheet = xl.BeginSheet(pctl.Tag))
            {
                sheet.WriteColumns(
                    W4, W4,
                    W12, W6, W12, W6,
                    W10, W10, W10, 
                    W10, W10, 
                    W10, W10, W10,
                    W10, W10,
                    W12, W12
                );

                using (var rows = sheet.BeginSheetData())
                {
                    // Header
                    using (var row = rows.BeginRow())
                    {
                        row
                            .Append("Year")
                            .Append("Age")

                            .Append("J-401K")
                            .Append("A-401K")
                            .Append("J-Inv")
                            .Append("A-Inv")

                            .Append("Fees")
                            .Append("PYTax")
                            .Append("CYExp")

                            .Append("SS")
                            .Append("ANN")

                            .Append("X-401K")
                            .Append("X-Inv")
                            .Append("X-Cash")

                            .Append("+/- 401K")
                            .Append("+/- Inv")

                            .Append("D-401K")
                            .Append("D-Inv")
                            ;

                    }

                    foreach(var y in p.ByYear.Span)
                    {
                        using (var row = rows.BeginRow())
                        {
                            row
                                .Append(y.Year)
                                .Append(y.Age)

                                .Append(y.Jan.PreTax.Amount, styles.C0)
                                .Append(y.Jan.PreTax.Allocation, styles.P0)
                                .Append(y.Jan.PostTax.Amount, styles.C0)
                                .Append(y.Jan.PostTax.Allocation, styles.P0)

                                .Append(y.Fees.Total(), styles.C0)
                                .Append(y.Expenses.PYTax, styles.C0)
                                .Append(y.Expenses.CYExp, styles.C0)

                                .Append(y.Incomes.SS, styles.C0)
                                .Append(y.Incomes.Ann, styles.C0)

                                .Append(y.Withdrawals.PreTax, styles.C0)
                                .Append(y.Withdrawals.PostTax, styles.C0)
                                .Append(y.Withdrawals.Cash, styles.C0)

                                .Append(y.Change.PreTax, styles.C0)
                                .Append(y.Change.PostTax, styles.C0)

                                .Append(y.Dec.PreTax.Amount, styles.C0)
                                .Append(y.Dec.PostTax.Amount, styles.C0)
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
