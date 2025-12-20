
namespace NinthBall
{
    /// <summary>
    /// Keeps a tab on LastWriteTime of set of files
    /// </summary>
    sealed class WatchFileSet(params string[] filesToWatch)
    {
        readonly Dictionary<string, DateTime> PriorTimestamps = filesToWatch
            .Where(f => !string.IsNullOrWhiteSpace(f))
            .ToDictionary(f => f, f => DateTime.MinValue);

        /// <summary>
        /// Check for change to any file. Also, remember the current timestamp.
        /// </summary>
        public bool CheckForChangesAndRememberTimestamp()
        {
            var hasChanges = false;

            foreach ((var fileName, var oldTimestamp) in PriorTimestamps)
            {
                var newTimestamp = PriorTimestamps[fileName] = null != fileName && File.Exists(fileName) ? File.GetLastWriteTimeUtc(fileName) : DateTime.MinValue;
                hasChanges |= newTimestamp != oldTimestamp;
            }

            return hasChanges;
        }

        /// <summary>
        /// Files currently under watch
        /// </summary>
        public IEnumerable<string> Watching => PriorTimestamps.Keys;

        /// <summary>
        /// Indicates if all files in the fileSet are present.
        /// </summary>
        public bool AllExists() => PriorTimestamps.Keys.All(f => null != f && File.Exists(f));

        /// <summary>
        /// Adds the file to the watch list.
        /// </summary>
        public void AlsoWatch(params string[] oneMoreFileName)
        {
            foreach (var fileName in oneMoreFileName)
                if (null != fileName && !PriorTimestamps.ContainsKey(fileName)) 
                    PriorTimestamps[fileName] = DateTime.MinValue;
        }
    }
}
