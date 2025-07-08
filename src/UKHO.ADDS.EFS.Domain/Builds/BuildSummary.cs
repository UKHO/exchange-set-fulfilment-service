using System.Text.Json.Serialization;

namespace UKHO.ADDS.EFS.Builds
{
    public abstract class BuildSummary
    {
        /// <summary>
        ///     The Job ID
        /// </summary>
        public string JobId { get; set; }

        /// <summary>
        ///     The File Share Batch ID associated with the job.
        /// </summary>
        public string BatchId { get; set; }

        /// <summary>
        ///     The collection of statuses for each node in the build process.
        /// </summary>
        public List<BuildNodeStatus>? Statuses { get; set; }

        /// <summary>
        ///     The collection of log messages generated during the build process.
        /// </summary>
        public List<string>? LogMessages { get; set; }

        /// <summary>
        ///     Used to provide an identifier based on the job id for storage
        /// </summary>
        [JsonIgnore]
        public string SummaryId => $"{JobId}-summary";

        public void AddStatus(BuildNodeStatus status)
        {
            Statuses ??= [];
            Statuses.Add(status);
        }

        public void SetLogMessages(IEnumerable<string> logLines)
        {
            LogMessages ??= [];
            LogMessages.AddRange(logLines);
        }
    }
}
