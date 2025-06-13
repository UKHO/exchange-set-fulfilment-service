using Microsoft.Extensions.Configuration;

namespace UKHO.ADDS.EFS.Orchestrator.API.FunctionalTests.Support
{
    public class TestConfiguration
    {
        private readonly IConfigurationRoot _configuration;

        public TestConfiguration()
        {            
            IConfigurationBuilder builder = new ConfigurationBuilder()
                                            .AddJsonFile(Path.GetFullPath(@"..\..\..\") + "testConfig.json", true, true);

            _configuration = builder.Build();
        }

        public string AzureStorageConnectionString => _configuration["Storage:ConnectionString"] ?? string.Empty;
        public string NodeStatusTable => _configuration["Storage:NodeStatusTable"] ?? string.Empty;
        public string ExchangeSetContainerName => _configuration["Storage:ExchangeSetContainerName"] ?? string.Empty;
    }
}
