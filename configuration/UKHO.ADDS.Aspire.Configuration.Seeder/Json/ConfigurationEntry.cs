namespace UKHO.ADDS.Aspire.Configuration.Seeder.Json
{
    internal class ConfigurationEntry(string configValue, string? contentType = null)
    {
        public string ConfigValue { get; } = configValue;
        public string? ContentType { get; set; } = contentType;

        public override string ToString() => ContentType is null
            ? $"ConfigValue: {ConfigValue}"
            : $"ConfigValue: {ConfigValue}, ContentType: {ContentType}";
    }
}
