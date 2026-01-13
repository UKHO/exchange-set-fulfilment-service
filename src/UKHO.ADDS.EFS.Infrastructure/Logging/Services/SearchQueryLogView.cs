namespace UKHO.ADDS.EFS.Infrastructure.Logging.Services
{
    internal class SearchQueryLogView
    {
        public required int Limit { get; init; }
        public required int Start { get; init; }
        public required string Filter { get; init; }
    }
}
