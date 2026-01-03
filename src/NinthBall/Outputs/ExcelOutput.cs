

using NinthBall.Core;
using NF = NinthBall.Core.ExcelStylesheetBuilder.NumberFormats; 

namespace NinthBall
{
    internal static class ExcelOutput
    {
        static class MyColors
        {
            public const uint Black  = 0xFF000000;
            public const uint Red    = 0xFFC00000;
            public const uint Green  = 0xFF00B050;
            public const uint Blue   = 0xFF0070C0;
            public const uint Purple = 0xFF7030A0;
            public const uint Gray   = 0x808080;
        };

        // StyleIDs used by our report.
        private readonly record struct MyStyles
        (
            uint ColHeader,

            uint Alloc,
            uint C0, uint C0Red, uint C0Green,

            //uint C1,
            //uint P0,
            uint ROI, uint ROIRed, uint ROIGreen,

            uint SumTxt, uint SumC, uint SumP, uint SRate,

            uint YYYY, uint YYYYRed
        );


        // For annualization
        const double InflationRate = 0.03;

        public static async Task Generate(SimResult simResult, string excelFileName)
        {
            ArgumentNullException.ThrowIfNull(simResult);
            ArgumentNullException.ThrowIfNull(excelFileName);

            var BASE = new XLStyle()
            {
                Format = "General",
                FName = "Aptos Narrow",
                FSize = 11,
                IsBold = false,
                FColor = MyColors.Black,
                HAlign = HAlign.Left,
                VAlign = VAlign.Middle
            };
            var CENT = BASE with { HAlign = HAlign.Center };

            // Register the styles used by our report. Capture the CellFormatIds.
            var ssb = new ExcelStylesheetBuilder();
            var myStyles = new MyStyles
            (
                ColHeader:  ssb.RegisterStyle(CENT with { IsBold = true }),

                SumTxt:     ssb.RegisterStyle(CENT),
                SumC:       ssb.RegisterStyle(CENT with { Format = NF.C1 }),
                SumP:       ssb.RegisterStyle(CENT with { Format = NF.P1 }),
                SRate:      ssb.RegisterStyle(CENT with { Format = NF.P1, FColor = MyColors.Purple }),

                C0:         ssb.RegisterStyle(BASE with { Format = NF.C0 }),
                C0Red:      ssb.RegisterStyle(BASE with { Format = NF.C0, FColor = MyColors.Red }),
                C0Green:    ssb.RegisterStyle(BASE with { Format = NF.C0, FColor = MyColors.Green }),

                Alloc:      ssb.RegisterStyle(CENT with { Format = NF.P0, FColor = MyColors.Gray}),


                ROI:        ssb.RegisterStyle(BASE with { Format = NF.P1 }),
                ROIRed:     ssb.RegisterStyle(BASE with { Format = NF.P1, FColor = MyColors.Red }),
                ROIGreen:   ssb.RegisterStyle(BASE with { Format = NF.P1, FColor = MyColors.Green }),

                YYYY:       ssb.RegisterStyle(CENT with { Format = "0;-0;;", }),
                YYYYRed:    ssb.RegisterStyle(CENT with { Format = "0;-0;;", FColor = MyColors.Red })
            );
            var stylesheet = ssb.Build();

            using(var xl = new ExcelWriter(excelFileName, stylesheet))
            {
                // Render chosen strategies and related assumptions.
                RenderStrategy(xl, myStyles, simResult);

                // Render Summary sheet
                RenderSummary(xl, myStyles, simResult);

                // Render one sheet per percentile
                foreach(var pctl in Percentiles.Items) RenderPercentile(xl, myStyles, simResult, pctl);

                // NOTE: Dispose will not save. We have to save.
                xl.Save();
            }
        }

        static void RenderStrategy(ExcelWriter xl, MyStyles styles, SimResult simResult)
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

        static void RenderSummary(ExcelWriter xl, MyStyles styles, SimResult simResult)
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
                        row.Append(simResult.SurvivalRate, styles.SRate);
                    }

                    using (var row = rows.BeginRow())
                    {
                        // Empty row
                    }

                    using (var row = rows.BeginRow())
                    {
                        row.Append("");
                        foreach(var pctl in Percentiles.Items) row.Append(pctl.Tag, styles.SumTxt);
                        row.Append("");
                    }

                    using (var row = rows.BeginRow())
                    {
                        row.Append("Percentiles");
                        foreach (var pctl in Percentiles.Items) row.Append(pctl.Caption, styles.SumTxt);
                        row.Append(" percentile");
                    }

                    using (var row = rows.BeginRow())
                    {
                        row.Append("Start (Real)");
                        foreach (var pctl in Percentiles.Items)
                        {
                            var p = simResult.Percentile(pctl.Pctl);
                            var m = p.StartingBalance.Mil();
                            row.Append(m, styles.SumC);
                        }
                        row.Append(" millions");
                    }


                    using (var row = rows.BeginRow())
                    {
                        row.Append("End (Real)");
                        foreach (var pctl in Percentiles.Items)
                        {
                            var p = simResult.Percentile(pctl.Pctl);
                            var m = p.EndingBalance.InflationAdjustedValue(InflationRate, p.SurvivedYears).Mil();
                            row.Append(m, styles.SumC);
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
                            row.Append(m, styles.SumC);
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
                            row.Append(chng, styles.SumP);
                        }
                        row.Append("");
                    }
                }
            }
        }

        static void RenderPercentile(ExcelWriter xl, MyStyles styles, SimResult simResult, Percentiles.PCT pctl)
        {
            const double Blank = 2;
            const double W4 = 4;
            const double W6 = 6;
            const double W8 = 8;
            const double W12 = 12;

            var sheetName = $"{pctl.Caption}-{pctl.Tag}";
            var p = simResult.Percentile(pctl.Pctl);

            using (var sheet = xl.BeginSheet(sheetName))
            {
                double?[] colWidths = Enumerable.Range(0, 23).Select(x => (double?)W12).ToArray();
                colWidths[0] = colWidths[1] = W4;
                colWidths[3] = colWidths[5] = W6;
                colWidths[7] = colWidths[12] = colWidths[15] = colWidths[19] = Blank;
                colWidths[20] = W6;
                colWidths[21] = colWidths[22] = W8;

                sheet.WriteColumns(colWidths.AsSpan());

                using (var rows = sheet.BeginSheetData())
                {
                    // Header
                    using (var row = rows.BeginRow())
                    {
                        row
                            .Append("Year", styles.ColHeader)
                            .Append("Age", styles.ColHeader)

                            .Append("Jan-401K", styles.ColHeader)     // Jan, alloc and less fees
                            .Append("Alloc", styles.ColHeader)
                            .Append("Jan-Inv", styles.ColHeader)
                            .Append("Alloc", styles.ColHeader)
                            .Append("Fees", styles.ColHeader)
                            .Append("", styles.ColHeader)

                            .Append("SS", styles.ColHeader)           // Incomes & Withdrawals (inflow)
                            .Append("ANN", styles.ColHeader)
                            .Append("401K", styles.ColHeader)
                            .Append("Inv", styles.ColHeader)
                            // .Append("Cash", styles.ColHeader)
                            .Append("", styles.ColHeader)

                            .Append("PYTax", styles.ColHeader)        // Expenses (outflow)
                            .Append("CYExp", styles.ColHeader)
                            .Append("", styles.ColHeader)

                            .Append("ROI-401K", styles.ColHeader)     // ROI-Change and excess deposits
                            .Append("ROI-Inv", styles.ColHeader)
                            .Append("Deposits-Inv", styles.ColHeader)
                            .Append("", styles.ColHeader)

                            .Append("Like", styles.ColHeader)         // ROI History
                            .Append("Stocks", styles.ColHeader)
                            .Append("Bonds", styles.ColHeader)
                            ;

                    }

                    foreach(var y in p.ByYear.Span)
                    {
                        double fromPreTax = y.Withdrawals.PreTax * -1;
                        double fromPostTax = y.Deposits.PostTax - y.Withdrawals.PostTax;
                        double fromCash = y.Deposits.Cash - y.Withdrawals.Cash;

                        using (var row = rows.BeginRow())
                        {
                            uint likeYearStyle = y.ROI.StocksROI < 0 || y.ROI.BondsROI < 0 ? styles.ROIRed : styles.ROIGreen;

                            row
                                .Append(y.Year + 1)
                                .Append(y.Age)

                                // Jan, Alloc and Fees
                                .Append(y.Jan.PreTax.Amount, styles.C0)
                                .Append(y.Jan.PreTax.Allocation, styles.Alloc)
                                .Append(y.Jan.PostTax.Amount, styles.C0)
                                .Append(y.Jan.PostTax.Allocation, styles.Alloc)
                                .Append(y.Fees.Total(), styles.C0)
                                .Append("")

                                // Incomes & withdrawals
                                .Append(y.Incomes.SS, styles.C0)
                                .Append(y.Incomes.Ann, styles.C0)
                                .Append(y.Withdrawals.PreTax, styles.C0)
                                .Append(y.Withdrawals.PostTax, styles.C0)
                                // .Append(y.Withdrawals.Cash, styles.C0)
                                .Append("")

                                // Expenses
                                .Append(y.Expenses.PYTax, styles.C0)
                                .Append(y.Expenses.CYExp, styles.C0)
                                .Append("")

                                // Change in value and excess deposits
                                .Append(y.Change.PreTax, y.Change.PreTax < 0 ? styles.C0Red : styles.C0Green)
                                .Append(y.Change.PostTax, y.Change.PostTax < 0 ? styles.C0Red : styles.C0Green)
                                .Append(y.Deposits.PostTax, styles.C0)
                                .Append("")

                                .Append(y.ROI.LikeYear,  y.ROI.StocksROI < 0 || y.ROI.BondsROI < 0 ? styles.YYYYRed : styles.YYYY)
                                .Append(y.ROI.StocksROI, y.ROI.StocksROI < 0 ? styles.ROIRed : styles.ROIGreen)
                                .Append(y.ROI.BondsROI,  y.ROI.BondsROI < 0  ? styles.ROIRed : styles.ROIGreen)
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

