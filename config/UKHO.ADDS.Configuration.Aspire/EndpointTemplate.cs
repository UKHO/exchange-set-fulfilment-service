namespace UKHO.ADDS.Configuration.Aspire
{
    internal class EndpointTemplate
    {
        public required string Name { get; init; }
        public required IResourceBuilder<ProjectResource> Resource { get; init; }
        public required bool UseHttps { get; init; }
        public string? Hostname { get; set; }
        public string? Path { get; set; }
    }
}
