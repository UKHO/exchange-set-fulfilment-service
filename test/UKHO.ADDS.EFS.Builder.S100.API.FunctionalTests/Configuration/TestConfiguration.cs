using Microsoft.Extensions.Configuration;

namespace UKHO.ADDS.EFS.Orchestrator.API.FunctionalTests.Support
{
    public class TestConfiguration
    {
        private readonly IConfigurationRoot _configuration;

        // Globally accessible project directory
        public static readonly string ProjectDirectory =
            Directory.GetParent(AppContext.BaseDirectory)!.Parent!.Parent!.Parent!.FullName;
        
        public TestConfiguration()
        {            
            IConfigurationBuilder builder = new ConfigurationBuilder()
                                            .AddJsonFile(Path.Combine(ProjectDirectory, "testConfig.json"), true, true);

            _configuration = builder.Build();
        }

        public string AzureStorageConnectionString => _configuration["Storage:ConnectionString"] ?? string.Empty;
        public string BuildMementoTable => _configuration["Storage:BuildMementoTable"] ?? string.Empty;
        public string ExchangeSetTimestampTable => _configuration["Storage:ExchangeSetTimestampTable"] ?? string.Empty;
        public string ExchangeSetContainerName => _configuration["Storage:ExchangeSetContainerName"] ?? string.Empty;
        public string OrchestratorApiEndpointName => _configuration["EndPoints:OrchestratorApiEndpoint"] ?? string.Empty;
        public string DownloadExchangeApiEndpoint => _configuration["EndPoints:DownloadExchangeApiEndpoint"] ?? string.Empty;
        public string ExchangeSetName => _configuration["ExchangeSet:ExchangeSetName"] ?? string.Empty;
        
    }
}
