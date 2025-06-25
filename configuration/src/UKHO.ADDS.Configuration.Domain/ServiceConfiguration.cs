namespace UKHO.ADDS.Configuration
{
    public sealed class ServiceConfiguration
    {
        public required string ServiceName { get; init; }
        public required Dictionary<string, FlattenedProperty> Properties { get; init; }
    }
}
