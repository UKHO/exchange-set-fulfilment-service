using System.Collections.ObjectModel;
using System.Text.Json;
using Azure.Data.Tables;
using UKHO.ADDS.Configuration.Schema;

namespace UKHO.ADDS.Configuration.Services
{
    internal class ConfigurationStore
    {
        private readonly ConfigurationReader _reader;
        private readonly TableServiceClient _tableServiceClient;
        private readonly IConfiguration _configuration;

        private readonly ObservableCollection<StoredServiceConfiguration> _serviceConfigurations;
        private readonly ObservableCollection<DiscoEndpointTemplate> _services;

        private AddsEnvironment? _environment;

        public ConfigurationStore(ConfigurationReader reader, TableServiceClient tableServiceClient, IConfiguration configuration)
        {
            _reader = reader;
            _tableServiceClient = tableServiceClient;
            _configuration = configuration;

            _serviceConfigurations = [];
            _services = [];
        }

        public ObservableCollection<StoredServiceConfiguration> Configuration => _serviceConfigurations;

        public ObservableCollection<DiscoEndpointTemplate> Services => _services;

        public async Task InitialiseAsync(AddsEnvironment environment)
        {
            _environment = environment;
            var configuration = await _reader.ReadConfigurationAsync(environment);

            _serviceConfigurations.Clear();
            foreach (var serviceConfiguration in configuration)
            {
                _serviceConfigurations.Add(serviceConfiguration);
            }

            var tableClient = _tableServiceClient.GetTableClient(WellKnownConfigurationName.ExternalServiceDiscoTableName);
            _services.Clear();

            await foreach (var entity in tableClient.QueryAsync<TableEntity>())
            {
                if (entity.TryGetValue("Endpoint", out var json) && json is string jsonString)
                {
                    var template = JsonSerializer.Deserialize<DiscoEndpointTemplate>(jsonString);
                    if (template is not null)
                    {
                        _services.Add(template);
                    }
                }
            }
        }

        public bool IsLocal
        {
            get
            {
                var addsEnvironment = AddsEnvironment.Parse(_configuration[WellKnownConfigurationName.AddsEnvironmentName]!);
                return addsEnvironment.IsLocal();
            }
        }
    }
}
