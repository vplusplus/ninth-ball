using NinthBall.Core;
using NinthBall.Core.PrettyPrint;
using System.Data;

namespace UnitTests.ClusterTraining
{
    [TestClass]
    public class RegimeAwarenessValidation
    {
        const string ReportsFolder = @"D:\Source\ninth-ball\src\UnitTests\Reports\";

        [TestMethod]
        public void PrintRegimeAwarenessSmoothing()
        {

            const int MyRegimeDiscoverySeed = 12345;
            int[] ThreeYearBlocksOnly = [3];

            // Prepare 3 year blocks
            var hRegimes = new HistoricalReturns().Returns
                .ReadBlocks(ThreeYearBlocksOnly)
                .ToList()
                .DiscoverRegimes(MyRegimeDiscoverySeed, numRegimes: 5);

            var regimeNames = hRegimes.Regimes.Select(x => x.RegimeLabel).ToArray();

            double[] regimeAwarenessList = { 1.0, 0.75, 0.5, 0.25, 0.0 };

            using var sw = new StreamWriter(Path.Combine(ReportsFolder, "RegimeAwareness-V2.md"));

            sw.PrintMarkdownTitle2("Regime transition smoothing");

            // Original - Unadjusted matrix
            var dt = RegimeTransitionsAsDataTable(hRegimes.RegimeTransitions, hRegimes.RegimeDistribution.Span, regimeNames);
            sw
                .PrintMarkdownTitle3($"Unadjusted regime transitions:")
                .PrintMarkdownTable(dt)
                .AppendLine();


            foreach (var regimeAwareness in regimeAwarenessList)
            {
                // Smooth and print
                var adjustedMatrix = RegimeAwareMovingBlockBootstrapper.ApplyRegimeAwareTransitionSmoothing(hRegimes.RegimeTransitions, hRegimes.RegimeDistribution.Span, regimeAwareness, out var adjustedRegimeDistribution);

                dt = RegimeTransitionsAsDataTable(adjustedMatrix, adjustedRegimeDistribution, regimeNames);
                sw
                    .PrintMarkdownTitle3($"Regime Awareness: {regimeAwareness:P0}")
                    .PrintMarkdownTable(dt)
                    .AppendLine();
            }
        }

        static DataTable RegimeTransitionsAsDataTable(TwoDMatrix regimeTransitions, ReadOnlySpan<double> regimeDistribution, string[] regimeNames)
        {
            var numRegimes = regimeTransitions.NumRows;

            var dt = new DataTable();

            dt.WithColumn("Regime");
            for(int i=0; i< numRegimes; i++)
            {
                var regimeLabel = regimeNames[i];
                dt.WithColumn<double>(regimeLabel, format: "P0", alignRight: true);
            }

            var values = new List<object>(dt.Columns.Count);

            // Regime distribution
            values.Clear();
            values.Add("Distribution");
            for(int i=0; i<numRegimes; i++)
            {
                values.Add(regimeDistribution[i]);
            }
            dt.Rows.Add(values.ToArray());

            for (int i = 0; i < numRegimes; i++)
            {
                var regimeLabel = regimeNames[i];
                var tx = regimeTransitions[i];

                values.Clear();
                values.Add(regimeLabel);
                for (int j = 0; j < numRegimes; j++) values.Add(tx[j]);
                dt.Rows.Add(values.ToArray());
            }

            return dt;
        }


    }
}
