namespace UKHO.ADDS.Aspire.Configuration.Remote
{
    public class RemoteEndpointTemplate
    {
        public string Service { get; init; } = string.Empty;
        public string Tag { get; init; } = string.Empty;
        public string Scheme { get; init; } = "https";
        public string OriginalTemplate { get; init; } = string.Empty;
        public string? Placeholder { get; init; }
        public string ResolvedUrl { get; init; } = string.Empty;
    }
}
