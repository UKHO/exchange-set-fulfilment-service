namespace UKHO.ADDS.Aspire.Configuration.Remote
{
    public class RemoteServiceDefinition
    {
        public string Service { get; init; } = string.Empty;
        public string ClientId { get; init; } = string.Empty;
        public List<RemoteEndpointTemplate> Endpoints { get; init; } = new();
    }
}
