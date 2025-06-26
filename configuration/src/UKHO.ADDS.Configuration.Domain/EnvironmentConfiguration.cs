using UKHO.ADDS.Configuration.Schema;

namespace UKHO.ADDS.Configuration
{
    internal sealed class EnvironmentConfiguration
    {
        public required AddsEnvironment Environment { get; init; }
        public required List<ServiceConfiguration> Services { get; init; }
    }
}
