using NinthBall.Core;
using NinthBall.Reports.PrettyPrint;
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
            var dtMarketPersonality = MarketDynamicsAsDataTable(hRegimes, displayOrder);
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
            foreach(var rIdx in displayOrder)
            {
                var r = regimes.Regimes[rIdx];
                dt.WithColumn<double>(r.RegimeLabel, format: "P0", alignRight: true);
            }

            var values = new List<object>(dt.Columns.Count);

            // Regime distribution
            values.Clear();
            values.Add("Distribution");
            foreach (var rIdx in displayOrder)
            {
                var R = regimes.Regimes[rIdx];
                var dist = regimes.RegimeDistribution.Span[R.RegimeIdx];
                values.Add(dist);
            }
            dt.Rows.Add(values.ToArray());

            foreach (var rIdx in displayOrder)
            {
                var R  = regimes.Regimes[rIdx];
                var tx = regimes.RegimeTransitions[R.RegimeIdx];

                values.Clear();
                values.Add(R.RegimeLabel);
                foreach (var ridx2 in displayOrder) values.Add(tx[ridx2]);
                dt.Rows.Add(values.ToArray());
            }

            return dt;
        }

        static DataTable MarketDynamicsAsDataTable(HRegimes hRegimes, int[] displayOrder)
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

            foreach(var rIdx in displayOrder)
            {
                var R = hRegimes.Regimes[rIdx];

                dt.Rows.Add(
                [
                    R.RegimeLabel,

                    R.Stocks.Mean,
                    R.Stocks.Volatility,
                    R.Bonds.Mean,
                    R.Bonds.Volatility,
                    R.Inflation.Mean,
                    R.Inflation.Volatility,

                    R.StocksBondsCorrelation,
                    R.InflationStocksCorrelation,
                    R.InflationBondsCorrelation
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
