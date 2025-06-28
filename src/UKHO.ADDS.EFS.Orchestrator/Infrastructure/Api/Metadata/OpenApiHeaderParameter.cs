namespace UKHO.ADDS.EFS.Orchestrator.Infrastructure.Api.Metadata
{
    internal class OpenApiHeaderParameter
    {
        public required string Name { get; init; }
        public string? Description { get; init; }
        public bool Required { get; init; } = false;
        public required string ExpectedValue { get; init; }
    }
}
