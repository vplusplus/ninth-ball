using NinthBall.Core;
using NinthBall.Reports.PrettyPrint;
using System.Data;

namespace UnitTests.ClusterTraining
{
    [TestClass]
    public class RegimeCountSelection
    {
        const string ReportsFolder = @"D:\Source\ninth-ball\src\UnitTests\Reports\";

        const int MyRegimeDiscoverySeed = 12345;

        [TestMethod]
        public void DiscoverRegimesForMultipleK()
        {
            int[] ThreeYearBlocksOnly = [3];

            // Prepare 3 year blocks
            var hBlocks3Y = new HistoricalReturns().Returns
                .ReadBlocks(ThreeYearBlocksOnly);

            // Use different cluster sizes, 
            for (int K = 3; K <= 5; K++)
            {
                var hRegimes = hBlocks3Y.DiscoverRegimes(MyRegimeDiscoverySeed, K);
                ExportRegimeReport(hRegimes, Path.Combine(ReportsFolder, $"HRegime-K{K}.md"));
            }
        }

        private static void ExportRegimeReport(HRegimes hRegimes, string path)
        {
            using var sw = new StreamWriter(path);
            var K = hRegimes.Regimes.Count;

            // Cosmetic: Regimes can jump around. Try to display using soft-order.
            var displayOrder = Enumerable.Range(0, hRegimes.Regimes.Count).OrderByDescending(i => RegimeDisplayOrder(hRegimes.Regimes[i])).ToArray();

            var dtMarketDynamics    = DTMarketDynamics(hRegimes, displayOrder);
            var dtRegimeTransitions = DTRegimeTransitions(hRegimes, displayOrder);
            var dtMomentsStocks     = DTMoments(hRegimes, displayOrder, "Stocks",    sp => sp.Stocks);
            var dtMomentsBonds      = DTMoments(hRegimes, displayOrder, "Bonds",     sp => sp.Bonds);
            var dtMomentsInfl       = DTMoments(hRegimes, displayOrder, "Inflation", sp => sp.Inflation);
            var dtAssetCorrelations = DTAssetsCorrelations(hRegimes, displayOrder);

            sw
                .PrintMarkdownTitle2($"Regime Profile (K={K})")

                .PrintMarkdownTitle3("Market dynamics")
                .PrintMarkdownTable(dtMarketDynamics)

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

        static DataTable DTRegimeTransitions(HRegimes regimes, int[] displayOrder)
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

        static DataTable DTMarketDynamics(HRegimes hRegimes, int[] displayOrder)
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

        static DataTable DTMoments(HRegimes hRegimes, int[] displayOrder, string assetClass, Func<Regime, Moments> fxMoment)
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

        static DataTable DTAssetsCorrelations(HRegimes hRegimes, int[] displayOrder)
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
