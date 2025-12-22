
using System.Collections.ObjectModel;

namespace NinthBall.Core
{
    internal sealed class FlatBootstrapper(SimParams SimParams, FlatBootstrap Options) : IBootstrapper
    {
        // Prepare ROI sequence once.
        readonly ReadOnlyCollection<HROI> FlatSequence = FillSameROI(Options.Stocks, Options.Bonds, numItems: SimParams.NoOfYears);

        // Growth is flat, meaningless to perform more than one iteration.
        public int GetMaxIterations(int numYears) => 1;

        // Returns same sequence for all iterations
        public IReadOnlyList<HROI> GetROISequence(int iterationIndex, int numYears)
        {
            return numYears == FlatSequence.Count
                ? FlatSequence 
                : throw new Exception($"Prepared {FlatSequence.Count} years of ROI. Now, the Simulation wants {numYears} years. This is not expected.");
        }

        // Prepare an array with exactly same ROI.
        static ReadOnlyCollection<HROI> FillSameROI(double stocksROI, double bondsROI, int numItems)
        {
            var oneROI = new HROI(0, stocksROI, bondsROI);
            var sequence = new HROI[numItems];
            Array.Fill(sequence, oneROI);
            return sequence.AsReadOnly();
        }

        public override string ToString() => $"Flat growth | Stocks: {Options.Stocks:P1} Bonds: {Options.Stocks:P1}";
    }
}
