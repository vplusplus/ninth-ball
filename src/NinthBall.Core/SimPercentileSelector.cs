namespace NinthBall.Core
{
    public static class SimPercentileSelector
    {

        extension(SimResult simResult)
        {
            public SimIteration MonotonicResultAtPercentile(double percentile)
            {
                if (0 == simResult.Iterations.Count) throw new InvalidOperationException("No results available");

                // Returns SimIteration that represents SimYear representing suggested percentile for each year.
                // Since iterations for a given is year is always ranked by real ending balance, 
                // other path specific attributes (blindly-carried-forward) such as inflation rate, running multipliers, prior year taxes, etc 
                // will be meaning less.

                int maxYears = simResult.SimParams.NoOfYears;

                // Compute the index of suggested percentile once.
                int maxIterations = simResult.Iterations.Count;
                int pctlIndex =
                    0.0 == percentile ? 0 :
                    1.0 == percentile ? maxIterations - 1 :
                    (int)Math.Floor(percentile * (maxIterations - 1));

                SimYear[] sampledYears = new SimYear[maxYears];

                // Prepare a reusable buffer for sorting to avoid excessive allocations.
                SimYear[] yearBuffer = new SimYear[maxIterations];

                for (int y = 0; y < maxYears; y++)
                {
                    for (int i = 0; i < maxIterations; i++)
                    {
                        var it = simResult.Iterations[i];
                        // If an iteration failed mid-way, ByYear will be shorter than maxYears.
                        // We use a default SimYear (which has DecReal = 0.0) for years after failure.
                        yearBuffer[i] = it.ByYear.Length > y ? it.ByYear.Span[y] : new SimYear();
                    }

                    // Sort the buffer for this specific year by inflation-adjusted ending balance.
                    Array.Sort(yearBuffer, (a, b) => a.DecReal.CompareTo(b.DecReal));

                    // Pick the SimYear representing the target percentile for this year.
                    sampledYears[y] = yearBuffer[pctlIndex];
                }

                return new SimIteration(pctlIndex, sampledYears[^1].DecReal > 0, sampledYears);
            }

        }

    }
}
