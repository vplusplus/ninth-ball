
using NinthBall.Core;
using NinthBall.Utils;
using NF = NinthBall.Utils.ExcelStylesheetBuilder.NumberFormats; 
// using static NinthBall.Outputs.Suggested;
// using System.ComponentModel.DataAnnotations;  // For GetColName, GetFormatHint, GetColorHint ...

namespace NinthBall.Outputs.Excel
{
    internal sealed class ExcelReport(OutputDefaults Defaults, OutputViews Views, OutputOptions Options)
    {
        readonly IReadOnlyList<double> Percentiles = Options.Excel.Percentiles ?? Defaults.Percentiles;
        readonly IReadOnlyList<CID> Columns = Views.ResolveView(Options.Excel.View);
        readonly IReadOnlyList<int> Iterations = Options.Excel.Iterations ?? [];

        // Colors & Styles for bespoke tabs (Strategy, Summary)
        static class MyColors
        {
            public const uint Black  = 0xFF000000;
            public const uint Red    = 0xFFC00000;
            public const uint Green  = 0xFF00B050;
            public const uint Purple = 0xFF7030A0;
            public const uint Gray   = 0x808080;
        };

        // StyleIDs used by custom tabs (Strategy, Summary)
        private readonly record struct SummaryStyles
        (
            uint ColHeader,
            uint Alloc,
            uint C0, uint C0Red, uint C0Green,
            uint ROI, uint ROIRed, uint ROIGreen,
            uint SumTxt, uint SumC, uint SumP, uint SRate,
            uint YYYY, uint YYYYRed
        );

        public async Task GenerateAsync(SimResult simResult)
        {
            ArgumentNullException.ThrowIfNull(simResult);

            string excelFileName = Path.GetFullPath(Options.Excel.File);
            var ssb = new ExcelStylesheetBuilder();

            // Register specific styles for Summary/Strategy tabs
            var summaryStyles = RegisterSummaryStyles(ssb);

            // Register dynamic styles for Percentile tabs need to be resolved on the fly
            // We use a caching convention inside RenderPercentile, or just rely on ssb caching?
            // ssb has internal caching, so we can just call ssb.RegisterStyle(spec) repeatedly.
            // It is efficient enough.

            using(var xl = new ExcelWriter(excelFileName))
            {
                // Render chosen strategies and related assumptions.
                RenderStrategy(xl, summaryStyles, simResult);

                // Render Summary sheet
                // NOTE: Still uses bespoke logic as it's a summary table, not a time-series
                RenderSummary(xl, summaryStyles, simResult);

                // Render one sheet per percentile
                foreach (var pctl in Percentiles)
                {
                    var iter = simResult.Percentile(pctl);
                    var sheetName = $"{pctl.PctlName}";
                    
                    RenderIteration(xl, ssb, iter, sheetName);
                }

                // Render one sheet per suggested un-ordered iteration
                foreach (var IT in Iterations)
                {
                    if (IT < 0 || IT > simResult.Iterations.Count - 1) continue;

                    // NOTE: We are selecting a specific simulation path by its IterationIndex.
                    // We are NOT selecting by index of sorted result, which can move around.
                    var iter = simResult.Iterations.Where(x => x.Index == IT).Single();
                    var sheetName = $"#{iter.Index:0000}";
                    
                    RenderIteration(xl, ssb, iter, sheetName);
                }

                // NOTE: Dispose will not save. We have to save.
                // Build stylesheet at the very end
                xl.Save(ssb.Build());

                Print.See("Excel report", excelFileName);
            }
        }

        SummaryStyles RegisterSummaryStyles(ExcelStylesheetBuilder ssb)
        {
            var BASE = new XLStyle()
            {
                Format = "General", FName = "Aptos Narrow", FSize = 11, IsBold = false,
                FColor = MyColors.Black, HAlign = HAlign.Left, VAlign = VAlign.Middle
            };
            var CENT = BASE with { HAlign = HAlign.Center };

            return new SummaryStyles
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

                YYYY:       ssb.RegisterStyle(CENT with { Format = "0;-0;;" }),
                YYYYRed:    ssb.RegisterStyle(CENT with { Format = "0;-0;;", FColor = MyColors.Red })
            );
        }

        void RenderStrategy(ExcelWriter xl, SummaryStyles styles, SimResult simResult)
        {
            using (var sheet = xl.BeginSheet("Strategy"))
            {
                sheet.WriteColumns(20, 120);

                var P = simResult.SimParams;
                var I = simResult.Iterations.First().ByYear.Span[0].Jan; 

                using (var rows = sheet.BeginSheetData())
                {
                    rows.BeginRow().EndRow();

                    rows
                        .BeginRow()
                        .Append("PreTax (401K)")
                        .Append($"{I.PreTax.Amount/1000000:C2} M")
                        .EndRow();

                    rows
                        .BeginRow()
                        .Append("PostTax")
                        .Append($"{I.PostTax.Amount/1000000:C2} M")
                        .EndRow();

                    rows
                        .BeginRow()
                        .Append("Allocation")
                        .Append($"{I.PreTax.Allocation:P0} - {1 - I.PreTax.Allocation:P0}")
                        .EndRow();

                    rows
                        .BeginRow()
                        .Append("Horizon")
                        .Append($"{simResult.SimParams.NoOfYears} years")
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

        void RenderSummary(ExcelWriter xl, SummaryStyles styles, SimResult simResult)
        {
            const double W10 = 10;
            const double W20 = 20;

            var P = simResult.SimParams;
            var I = simResult.Iterations.First().ByYear.Span[0].Jan;

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
                        row.Append("Percentiles");
                        foreach (var pctl in Percentiles) row.Append(pctl.PctlName, styles.SumTxt);
                        row.Append(" percentile");
                    }

                    using (var row = rows.BeginRow())
                    {
                        row.Append("Start (Real)");
                        foreach (var pctl in Percentiles)
                        {
                            var iter = simResult.Percentile(pctl);
                            var m = Mil(iter.ByYear.Span[0].Jan.Total);
                            row.Append(m, styles.SumC);
                        }
                        row.Append(" millions");
                    }


                    using (var row = rows.BeginRow())
                    {
                        row.Append("End (Real)");
                        foreach (var pctl in Percentiles)
                        {
                            var p = simResult.Percentile(pctl);
                            var m = Mil(p.LastGoodYear.Dec.Total / p.LastGoodYear.Metrics.InflationMultiplier);
                            row.Append(m, styles.SumC);
                        }
                        row.Append(" millions");
                    }

                    using (var row = rows.BeginRow())
                    {
                        row.Append("End (Nominal)");
                        foreach (var pctl in Percentiles)
                        {
                            var p = simResult.Percentile(pctl);
                            var m = Mil(p.LastGoodYear.Dec.Total);
                            row.Append(m, styles.SumC);
                        }
                        row.Append(" millions");
                    }

                    using (var row = rows.BeginRow())
                    {
                        row.Append("Change (Annualized)");
                        foreach (var pctl in Percentiles)
                        {
                            var p = simResult.Percentile(pctl);
                            var chng = p.LastGoodYear.Metrics.AnnualizedReturn;  // Nominal CAGR
                            row.Append(chng, styles.SumP);
                        }
                        row.Append("");
                    }
                }
            }
        }

        void RenderIteration(ExcelWriter xl, ExcelStylesheetBuilder ssb, SimIteration iteration, string sheetName)
        {
            //var sheetName = $"{pctl.PctlName}";
            var p = iteration;

            // Dynamically calculate widths based on Suggested format
            // Just a rough heuristic for now: 12 for most things, 4 for year/age
            var widths = Columns.Select(c => c == CID.Empty ? 2.0 : (c == CID.Year || c == CID.Age) ? 4.0 : 12.0).Cast<double?>().ToArray();

            // Base style with safe defaults
            var baseStyle = new XLStyle()
            {
                Format = "General",
                FName = "Calibri",
                FSize = 11,
                FColor = MyColors.Black,
                HAlign = HAlign.Right,
                VAlign = VAlign.Bottom
            };

            // Resolve Header Style
            var headerStyle = ssb.RegisterStyle(baseStyle with { IsBold = true, HAlign = HAlign.Center });

            using (var sheet = xl.BeginSheet(sheetName))
            {
                sheet.WriteColumns(widths.AsSpan());

                using (var rows = sheet.BeginSheetData())
                {
                    // Header
                    using (var row = rows.BeginRow())
                    {
                        foreach (var col in Columns)
                        {
                            if (col == CID.Empty)
                                row.Append("", headerStyle);
                            else
                                row.Append(col.GetColName(), headerStyle);
                        }
                    }

                    // Data
                    foreach (var y in p.ByYear.Span)
                    {
                        using (var row = rows.BeginRow())
                        {
                            foreach (var col in Columns)
                            {
                                if (col == CID.Empty)
                                {
                                    row.Append("");
                                    continue;
                                }

                                var value = y.GetCellValue(col, p);
                                if (value == null)
                                {
                                    row.Append("");
                                    continue;
                                }

                                // Style resolution
                                var formatHint = col.GetFormatHint();
                                var colorHint = y.GetCellColorHint(col, p);

                                // Map hints to XLStyle props
                                var fStr = GetFormatString(formatHint);
                                if (string.IsNullOrWhiteSpace(fStr))
                                {
                                    fStr = "General";   // Defensive fallback
                                }

                                var stylePrice = ssb.RegisterStyle(baseStyle with
                                {
                                    Format = fStr,
                                    FColor = GetColorHex(colorHint),
                                    HAlign = (col == CID.Year || col == CID.Age || col == CID.LikeYear) ? HAlign.Center : HAlign.Right
                                });

                                row.Append(value.Value, stylePrice);
                            }
                        }
                    }
                }
            }
        }


        static string GetFormatString(FormatHint f) => f switch
        {
            FormatHint.F0 => "0;-0;;",
            FormatHint.C0 => NF.C0,
            FormatHint.C1 => NF.C1,
            FormatHint.C2 => NF.C2,
            FormatHint.P0 => NF.P0,
            FormatHint.P1 => NF.P1,
            FormatHint.P2 => NF.P2,
            _ => "General"
        };
        
        static uint GetColorHex(ColorHint c) => c switch
        {
            ColorHint.Success => MyColors.Green,
            ColorHint.Danger  => MyColors.Red,
            ColorHint.Muted   => MyColors.Gray,
            ColorHint.Warning => 0xFFC09000, 
            ColorHint.Primary => 0xFF0070C0,
            _ => MyColors.Black
        };

        static double Mil(double value) => value / 1000000.0;
    }
}
