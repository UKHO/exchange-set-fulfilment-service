namespace UKHO.ADDS.Configuration
{
    internal class StoredServiceConfiguration
    {
        public required string ServiceName { get; init; }
        public required Dictionary<string, StoredProperty> Properties { get; init; }
    }
}
