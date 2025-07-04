namespace UKHO.ADDS.EFS.Builds
{
    public class BuildSummary
    {
        public string JobId { get; set; }

        public string BatchId { get; set; }

        public List<BuildNodeStatus>? Statuses { get; set; }
        
        public List<string>? LogMessages {get; set; }

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
