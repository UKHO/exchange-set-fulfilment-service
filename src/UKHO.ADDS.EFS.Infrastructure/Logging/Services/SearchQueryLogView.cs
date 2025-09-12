namespace UKHO.ADDS.EFS.Infrastructure.Logging.Services
{
    internal class SearchQueryLogView
    {
        public int Limit { get; set; }
        public int Start { get; set; }
        public string Filter { get; set; }
    }
}
