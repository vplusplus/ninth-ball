using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Office.Word;
using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;

namespace NinthBall.Core.Bootstrap
{

    public readonly record struct FeatureMatrix(int NumSamples, int NumFeatures)
    {
        public readonly Memory<double> Storage = new double[ NumSamples * NumFeatures];

        public readonly int Count = NumSamples;

        public readonly Span<double> this[int idx] => Storage.Slice(idx * NumFeatures, NumFeatures).Span;
    }

    internal class HistoricalRegimes
    {


        public static void DiscoverRegimes(IReadOnlyList<HBlock> blocks)
        {
            // Pre-check: We depend on cronology. Pre-check blocks are sorted by year & sequence length.
            if (!IsSortedByYearAndBlockLength(blocks)) throw new Exception("Invalid input: Blocks are not pre-sorted by Year and sequence length.");

            // Prepare input for K-Mean clustering. Extract the features.
            var featureMatrix = ToFeatureMatrix(blocks);
            var normalizedFeatureMatrix = NormalizeFeatureMatrix(featureMatrix);




        }

        static FeatureMatrix NormalizeFeatureMatrix(FeatureMatrix rawFeatureMatrix)
        {
            var numSamples = rawFeatureMatrix.NumSamples;
            var numFeatures = rawFeatureMatrix.NumFeatures;
            var normalizedMatrix = new FeatureMatrix(numSamples, numFeatures);

            // 1. Calculate Mean and StdDev for each feature
            var means = new double[numFeatures];
            var stdDevs = new double[numFeatures];

            for (int f = 0; f < numFeatures; f++)
            {
                double sum = 0;
                for (int s = 0; s < numSamples; s++)
                {
                    sum += rawFeatureMatrix[s][f];
                }
                means[f] = sum / numSamples;

                double sumSqDiff = 0;
                for (int s = 0; s < numSamples; s++)
                {
                    double diff = rawFeatureMatrix[s][f] - means[f];
                    sumSqDiff += diff * diff;
                }
                
                double variance = sumSqDiff / numSamples;
                stdDevs[f] = Math.Sqrt(variance);
            }

            // 2. Perform Z-Score normalization: (x - mean) / stdDev
            for (int s = 0; s < numSamples; s++)
            {
                var rawRow = rawFeatureMatrix[s];
                var normalizedRow = normalizedMatrix[s];

                for (int f = 0; f < numFeatures; f++)
                {
                    // Handle zero variance edge case to avoid NaN
                    normalizedRow[f] = stdDevs[f] > 0 
                        ? (rawRow[f] - means[f]) / stdDevs[f] 
                        : 0.0; 
                }
            }

            return normalizedMatrix;
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

        static FeatureMatrix ToFeatureMatrix(IReadOnlyList<HBlock> blocks)
        {
            var matrix = new FeatureMatrix(blocks.Count, 5);

            int index = 0;
            var storage = matrix.Storage.Span;
            foreach (var block in blocks)
            {
                storage[index++] = block.Features.NominalCAGRStocks;
                storage[index++] = block.Features.NominalCAGRBonds;
                storage[index++] = block.Features.MaxDrawdownStocks;
                storage[index++] = block.Features.MaxDrawdownBonds;
                storage[index++] = block.Features.GMeanInflationRate;
            }

            return matrix;
        }



    }
}
