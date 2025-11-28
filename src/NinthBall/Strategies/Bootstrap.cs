
namespace NinthBall
{
    /// <summary>
    /// Represents Stock and Bond ROI for one year.
    /// </summary>
    public sealed record YROI(int Year, double StocksROI, double BondROI);

    /// <summary>
    /// Represents a small sequence of historical returns.
    /// </summary>
    public sealed record Block(IReadOnlyList<YROI> Segment) { public readonly int ChronoIndex = Segment[0].Year; }

    public static class Bootstrap
    {
        /// <summary>
        /// Reads historical stock and bond ROI from Excel file.
        /// </summary>
        public static IReadOnlyList<YROI> ReadHistory(string xlFileName, string sheetName, bool skip1931)
        {
            ArgumentNullException.ThrowIfNull(xlFileName);
            ArgumentNullException.ThrowIfNull(sheetName);

            List<YROI> history = [];

            using (var xlReader = new ExcelReader(xlFileName))
            {
                var sheet = xlReader.GetSheets().Where(s => sheetName.Equals(s.SheetName, StringComparison.OrdinalIgnoreCase)).SingleOrDefault()
                    ?? throw new Exception($"Sheet not found | File: {Path.GetFileName(xlFileName)} | Sheet: '{sheetName}'");

                foreach (var row in sheet.GetRows())
                {
                    if (null == row) continue;

                    // Skip first (header) row.  Do not use IEnumerable.Skip(1) option; Use Rowindex.
                    var isFirstRow = null != row.RowIndex && 1 == row.RowIndex.Value;
                    if (isFirstRow) continue;

                    var cells = row.GetCellValues().ToArray();

                    if (
                        null != cells
                        && cells.Length >= 3
                        && int.TryParse(cells[0], out var year)
                        && double.TryParse(cells[1], out var stocksROI)
                        && double.TryParse(cells[2], out var bondROI)
                    )
                    {
                        // OPTIONAL constraint for exploratory "what-if" runs only.
                        // Excludes any block containing 1931 (the worst historical single-year result)
                        // to study sensitivity to extreme negative outliers. Disable for standard Monte Carlo.
                        if (skip1931 && year == 1931) continue; else history.Add(new(year, stocksROI, bondROI));
                    }
                }
            }

            return history.AsReadOnly();
        }

        public static IReadOnlyList<Block> ReadBlocks(this IReadOnlyList<YROI> history, IReadOnlyList<int> blockLengths)
        {
            if (0 == blockLengths.Count) throw new ArgumentException("Invalid blockLength(s). Please specify at least one.");
            if (blockLengths.Count != blockLengths.Distinct().Count()) throw new ArgumentException("Invalid blockLength(s). Expecting distinct numbers.");
            if (blockLengths.Any(x => x > history.Count)) throw new ArgumentException($"Invalid blockLength(s). Block size cannot be larger than history length ({history.Count}).");

            YROI[] arrHistory = history.ToArray();

            // We want repeatability in results.
            // The source data may not be pre-sorted, though its likely, we do not know.
            // The sampling technique will draw random block with uniform distribution.
            // Ordering the allBlocks here is only for repeatabilty across runs even if the source data sequence changes.
            return blockLengths
                .SelectMany(blockLength => ReadBlocks(arrHistory, blockLength))
                .OrderBy(b => b.ChronoIndex)
                .ThenBy(b => b.Segment.Count)
                .ToList()
                .AsReadOnly();

            static IEnumerable<Block> ReadBlocks(YROI[] history, int sequenceLength)
            {
                for (int i = 0; i <= (history.Length - sequenceLength); i++)
                {
                    YROI[] segment = new YROI[sequenceLength];
                    Array.Copy(history, i, segment, 0, sequenceLength);
                    yield return new Block(segment);
                }
            }
        }

        public static YROI[] SampleRandomMovingBlocks(Random rand, IReadOnlyList<Block> allBlocks, int numYears, bool noConsecutiveRepetition, bool skip1931)
        {
            ArgumentNullException.ThrowIfNull(rand);
            ArgumentNullException.ThrowIfNull(allBlocks);

            List<YROI> roiRandomSamples = new(numYears);
            Block prevBlock = null!;

            while (roiRandomSamples.Count < numYears)
            {
                // Next random block index, ranging from 0 to Count-1
                var blockIndex = rand.Next(0, allBlocks.Count);
                var nextBlock = allBlocks[blockIndex];

                // OPTIONAL constraint intended only for stress-testing / what-if scenarios.
                // Prevents drawing consecutive allBlocks with at least two years of overlapping sequence.
                // Intention is to reducing the chance of unrealistic repeated historical regimes (e.g., severe crash periods repeating back-to-back).
                // In normal runs, leave disabled to preserve pure bootstrap behavior.
                const int TwoYears = 2;
                if (noConsecutiveRepetition && null != prevBlock && HasOverlappingYears(prevBlock, nextBlock, maxOverlap: TwoYears)) continue;

                // Collect
                roiRandomSamples.AddRange(nextBlock.Segment);
                prevBlock = nextBlock;
            }

            // The last sample may provide more years than what we need.
            // Clip the sequence for number of years of interest.
            var roiSequence = roiRandomSamples.Take(numYears).ToArray();

            return roiSequence.ToArray();
        

            static bool HasOverlappingYears(Block prevBlock, Block nextBlock, int maxOverlap)
            {
                int overlapCount = 0;

                foreach (var yPrev in prevBlock.Segment)
                    foreach (var yNext in nextBlock.Segment)
                        if (yPrev == yNext && ++overlapCount >= maxOverlap) return true;

                return false;
            }        
        
        }
    }
}
