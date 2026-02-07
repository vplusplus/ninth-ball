using System;
using System.Collections.Generic;
using System.Text;

namespace NinthBall.Core.Bootstrap
{
    internal class HistoricalRegimes
    {


        public static void DiscoverRegimes(IReadOnlyList<HBlock> blocks)
        {
            // Pre-check: We depend on cronoloty. We are sorting in too many places. Here we are going to pre check blocks are sorted.
            if (!IsSortedByYearAndBlockLength(blocks)) throw new Exception("Invalid input: Blocks are not pre-sorted by Year and block length.");

            // Prepare input for K-Mean clustering
            




        }

        static bool IsSortedByYearAndBlockLength(IReadOnlyList<HBlock> blocks)
        {
            for (int i = 1; i < blocks.Count; i++)
            {
                var prev = blocks[i - 1];
                var curr = blocks[i];

                // Years must be non-descending
                if (curr.StartYear < prev.StartYear) return false;

                // If years are same, length must be non-descending
                if (curr.StartYear == prev.StartYear && curr.Slice.Length < prev.Slice.Length) return false;
            }
            return true;
        }


    }
}
