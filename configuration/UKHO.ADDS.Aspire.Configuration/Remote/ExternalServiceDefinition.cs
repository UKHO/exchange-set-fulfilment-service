namespace UKHO.ADDS.Aspire.Configuration.Remote
{
    internal class ExternalServiceDefinition
    {
        public string Service { get; init; } = string.Empty;
        public List<ExternalEndpointTemplate> Endpoints { get; init; } = new();
    }
}
