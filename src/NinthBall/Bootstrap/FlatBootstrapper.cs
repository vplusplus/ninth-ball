
using DocumentFormat.OpenXml.Drawing.Charts;

namespace NinthBall
{
    internal sealed class FlatBootstrapper(SimParams SimParams, FlatBootstrap Options) : IBootstrapper
    {
        // Prepare ROI sequence once.
        readonly IReadOnlyList<HROI> FlatSequence = FillSameROI(Options.Stocks, Options.Bonds, numItems: SimParams.NoOfYears);

        // Growth is flat, meaningless to perform more than one iteration.
        public int GetMaxIterations(int numYears) => 1;

        // Returns same sequence for all iterations
        public IReadOnlyList<HROI> GetROISequence(int iterationIndex, int numYears)
        {
            return numYears == FlatSequence.Count
                ? FlatSequence 
                : throw new Exception($"Prepared {FlatSequence.Count} years of ROI. Now Simulation wants {numYears} years. This is not expected.");
        }

        // Simulation wants sequence of returns. 
        // Prepare one array with exactly same ROI.
        static HROI[] FillSameROI(double stocksROI, double bondsROI, int numItems)
        {
            var oneROI = new HROI(0, stocksROI, bondsROI);
            var arrSame = new HROI[numItems];
            Array.Fill(arrSame, oneROI);
            return arrSame;
        }
    }
}
