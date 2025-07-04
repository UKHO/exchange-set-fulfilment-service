namespace UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging
{
    internal class SearchQueryLogView
    {
        public int Limit { get; set; }
        public int Start { get; set; }
        public string Filter { get; set; }
    }
}
