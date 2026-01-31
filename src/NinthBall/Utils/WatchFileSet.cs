
namespace NinthBall.Utils
{
    // Keeps a tab on LastWriteTime of set of files
    sealed class WatchFileSet(params string[] filesToWatch)
    {
        // Assume DateTime.MinValue for all files.
        readonly Dictionary<string, DateTime> PriorTimestamps = filesToWatch.Where(f => !string.IsNullOrWhiteSpace(f)).Distinct().ToDictionary(f => f, f => DateTime.MinValue);

        /// <summary>
        /// Check for change to any file. Also, remember the current timestamp.
        /// </summary>
        public bool CheckForChangesAndRememberTimestamp()
        {
            var hasChanges = false;

            foreach ((var fileName, var oldTimestamp) in PriorTimestamps)
            {
                var newTimestamp = PriorTimestamps[fileName] = File.Exists(fileName) ? File.GetLastWriteTimeUtc(fileName) : DateTime.MinValue;
                hasChanges |= newTimestamp != oldTimestamp;
            }

            return hasChanges;
        }
    }
}
