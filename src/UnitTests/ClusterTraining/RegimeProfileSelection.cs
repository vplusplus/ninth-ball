using NinthBall.Core;
using NinthBall.Core.PrettyPrint;
using System.Data;

namespace UnitTests.ClusterTraining
{
    [TestClass]
    public class RegimeProfileSelection
    {
        const string ReportsFolder = @"D:\Source\ninth-ball\src\UnitTests\Reports\";

        const int MyRegimeDiscoverySeed = 12345;

        [TestMethod]
        public void DiscoverRegimesForMultipleK()
        {
            int[] ThreeYearBlocksOnly = [3];

            // Prepare 3 year blocks
            var hReturns = new HistoricalReturns().Returns;
            var hBlocks3Y = hReturns.ReadBlocks(ThreeYearBlocksOnly).ToList().AsReadOnly();

            for (int K = 3; K <= 5; K++)
            {
                var hRegimes = hBlocks3Y.DiscoverRegimes(MyRegimeDiscoverySeed, K);
                Console.WriteLine($"Exporting Report for K={K}...");
                
                var reportName = Path.Combine(ReportsFolder, $"HRegime-K{K}.md");
                ExportRegimeReport(hRegimes, reportName);
            }
        }

        private static void ExportRegimeReport(HRegimes hRegimes, string path)
        {
            using var sw = new StreamWriter(path);
            var K = hRegimes.Regimes.Count;

            // Cosmetic: Regimes can jump around. Try to display using soft-order.
            var displayOrder = Enumerable.Range(0, hRegimes.Regimes.Count).OrderByDescending(i => RegimeDisplayOrder(hRegimes.Regimes[i])).ToArray();

            var dtRegimeTransitions = RegimeTransitionsAsDataTable(hRegimes, displayOrder);
            var dtMarketPersonality = MarketDynamicsAsDataTable(hRegimes);
            var dtMomentsStocks     = MomentsAsDataTable(hRegimes, "Stocks",    displayOrder, sp => sp.Stocks);
            var dtMomentsBonds      = MomentsAsDataTable(hRegimes, "Bonds",     displayOrder, sp => sp.Bonds);
            var dtMomentsInfl       = MomentsAsDataTable(hRegimes, "Inflation", displayOrder, sp => sp.Inflation);
            var dtAssetCorrelations = AssetsCorrelationAsDataTable(hRegimes, displayOrder);

            sw
                .PrintMarkdownTitle2($"Regime Profile (K={K})")

                .PrintMarkdownTitle3("Market dynamics")
                .PrintMarkdownTable(dtMarketPersonality)

                .PrintMarkdownTitle3("Regime Distribution and Transition Probabilities")
                .PrintMarkdownTable(dtRegimeTransitions)

                .PrintMarkdownTitle3("Assets Behavior")
                .PrintMarkdownTable(dtMomentsStocks)
                .AppendLine()
                .PrintMarkdownTable(dtMomentsBonds)
                .AppendLine()
                .PrintMarkdownTable(dtMomentsInfl)

                .PrintMarkdownTitle3("Assets Correlation")
                .PrintMarkdownTable(dtAssetCorrelations)
                .AppendLine()
                ;

            sw.Flush();
        }

        static DataTable RegimeTransitionsAsDataTable(HRegimes regimes, int[] displayOrder)
        {
            var dt = new DataTable();

            dt.WithColumn("Regime");
            foreach(var rid in displayOrder)
            {
                var r = regimes.Regimes[rid];
                dt.WithColumn<double>(r.RegimeLabel, format: "P0", alignRight: true);
            }

            // Regime distribution
            var values = new object[dt.Columns.Count];
            values[0] = "Distribution";
            for (int i = 0; i < displayOrder.Length; i++) values[i + 1] = regimes.RegimeDistribution.Span[displayOrder[i]];
            dt.Rows.Add(values);

            foreach (var rid in displayOrder)
            {
                var r  = regimes.Regimes[rid];
                var tx = regimes.RegimeTransitions[rid];

                values = new object[dt.Columns.Count];
                values[0] = r.RegimeLabel;
                for(int i=0; i<displayOrder.Length; i++) values[i + 1] = tx[displayOrder[i]];
                dt.Rows.Add(values);
            }

            return dt;
        }

        static DataTable MarketDynamicsAsDataTable(HRegimes hRegimes)
        {
            var dt = new DataTable();
            dt
                .WithColumn("Regime")

                .WithColumn<double>("Stocks (μ)", format: "P1")
                .WithColumn<double>("Stocks (σ)", format: "P1")
                .WithColumn<double>("Bonds (μ)",  format: "P1")
                .WithColumn<double>("Bonds (σ)",  format: "P1")
                .WithColumn<double>("Infl (μ)",   format: "P1")
                .WithColumn<double>("Infl (σ)",   format: "P1")

                .WithColumn<double>("S&B Corr",   format: "N2")
                .WithColumn<double>("I&S Corr",   format: "N2")
                .WithColumn<double>("I&B Corr",   format: "N2");

            foreach (var r in hRegimes.Regimes.OrderByDescending(p => RegimeDisplayOrder(p)))
            {
                dt.Rows.Add(
                [
                    r.RegimeLabel,

                    r.Stocks.Mean,
                    r.Stocks.Volatility,
                    r.Bonds.Mean,
                    r.Bonds.Volatility,
                    r.Inflation.Mean,
                    r.Inflation.Volatility,

                    r.StocksBondsCorrelation,
                    r.InflationStocksCorrelation,
                    r.InflationBondsCorrelation
                ]);
            }

            return dt;
        }

        static DataTable MomentsAsDataTable(HRegimes hRegimes, string assetClass, int[] displayOrder, Func<Regime, Moments> fxMoment)
        {
            var dt = new DataTable();

            dt
                .WithColumn(assetClass)
                .WithColumn<Double>("Mean",         format: "P1")
                .WithColumn<Double>("Volatility",   format: "P1")
                .WithColumn<Double>("Skewness",     format: "N2")
                .WithColumn<Double>("Kurtosis",     format: "N2")
                .WithColumn<Double>("AutoCorr",     format: "N2")
                ;

            foreach(var rIdx in displayOrder)
            {
                var R = hRegimes.Regimes[rIdx];
                var M = fxMoment(R);

                dt.Rows.Add(
                [   
                    R.RegimeLabel,
                    M.Mean,
                    M.Volatility,
                    M.Skewness,
                    M.Kurtosis,
                    M.AutoCorrelation
                ]);
            }

            return dt;
        }

        static DataTable AssetsCorrelationAsDataTable(HRegimes hRegimes, int[] displayOrder)
        {
            var dt = new DataTable();

            dt
                .WithColumn("Regime")
                .WithColumn<Double>("Stocks & Bonds",       format: "F2")
                .WithColumn<Double>("Inflation & Stocks",   format: "F2")
                .WithColumn<Double>("Inflation & Bonds",    format: "F2")
                ;

            foreach (var rIdx in displayOrder)
            {
                var R = hRegimes.Regimes[rIdx];

                dt.Rows.Add(
                [
                    R.RegimeLabel,
                    R.StocksBondsCorrelation,
                    R.InflationStocksCorrelation,
                    R.InflationBondsCorrelation
                ]);
            }

            return dt;
        }

        // Some predictable order...
        static double RegimeDisplayOrder(Regime p) =>
            + p.Stocks.Mean
            - p.Stocks.Volatility
            + p.Stocks.Skewness
            - p.Stocks.Kurtosis
            - p.Bonds.Mean;

    }
}
