namespace UKHO.ADDS.Configuration
{
    public class DiscoEndpointTemplate
    {
        public string Key { get; init; } = string.Empty;
        public string Scheme { get; init; } = "https";
        public string OriginalTemplate { get; init; } = string.Empty;
        public string? Placeholder { get; init; }
        public string ResolvedUrl { get; init; } = string.Empty;
        public bool IsTemplate { get; init; }
    }
}
