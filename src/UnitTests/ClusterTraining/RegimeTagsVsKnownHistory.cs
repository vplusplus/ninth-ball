using NinthBall.Core;
using NinthBall.Reports.PrettyPrint;
using System.Data;

namespace UnitTests.ClusterTraining
{
    [TestClass]
    public class RegimeTagsVsKnownHistory
    {
        const string ReportsFolder = @"D:\Source\ninth-ball\src\UnitTests\Reports\";
        readonly record struct RegimeMap(int FromYear, int ToYear, string RegimeLabel);

        [TestMethod]
        public void PrintRegimeTagsAndTheirBlocks()
        {
            const int MyRegimeDiscoverySeed = 12345;
            int[] ThreeYearBlocksOnly = [3];

            // Use 3 year blocks, discover regimes.
            var hRegimes = new HistoricalReturns().Returns
                .ReadBlocks(ThreeYearBlocksOnly)
                .DiscoverRegimes(MyRegimeDiscoverySeed, numRegimes: 5);

            // Read 3/4/5 year block and map them to regimes.
            var allBlocks = new HistoricalReturns().Returns.ReadBlocks([3, 4, 5]);
            var regimeAwareBlocks = RegimeAwareMovingBlockBootstrapper.MapBlocksToRegimesOnce(allBlocks, hRegimes);


            var map = new List<RegimeMap>();
            for(int regimeIdx = 0; regimeIdx < regimeAwareBlocks.Count; regimeIdx++)
            {
                var label = hRegimes.Regimes[regimeIdx].RegimeLabel;

                foreach(var block in regimeAwareBlocks[regimeIdx])
                {
                    map.Add(new(block.StartYear, block.EndYear, label));
                }
            }
            map = map.OrderBy(m => m.FromYear).ThenBy(m => m.ToYear).ToList();

            var dt = new DataTable().WithColumn<int>("FromYear").WithColumn<int>("ToYear").WithColumn<string>("Regime");
            foreach(var item in map)
            {
                dt.Rows.Add([
                    item.FromYear, item.ToYear, item.RegimeLabel    
                ]);
            }

            using var sw = new StreamWriter(Path.Combine(ReportsFolder, "BlocksAndTheirRegimes.md"));
            sw.PrintMarkdownTitle2("Blocks and their regimes");
            sw.PrintMarkdownTable(dt);
            sw.WriteLine();

        }
    }
}
