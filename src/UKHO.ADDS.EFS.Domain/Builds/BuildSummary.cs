namespace UKHO.ADDS.EFS.Builds
{
    public class BuildSummary
    {
        private readonly List<BuildNodeStatus> _statuses;
        readonly List<string> _logMessages;

        public BuildSummary()
        {
            _statuses = [];
            _logMessages = [];
        }

        public IEnumerable<BuildNodeStatus> Statuses => _statuses;

        public IEnumerable<string> LogMessages => _logMessages;

        public void AddStatus(BuildNodeStatus status)
        {
            _statuses.Add(status);
        }

        public void SetLogMessages(IEnumerable<string> logLines)
        {
            _logMessages.AddRange(logLines);
        }
    }
}
