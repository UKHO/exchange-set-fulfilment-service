namespace UKHO.ADDS.Configuration
{
    internal class StoredProperty
    {
        public required string Path { get; init; }
        public required string? Value { get; set; }
        public string? Type { get; init; }
        public bool Required { get; init; }
        public bool Secret { get; init; }
    }
}
