using System.Collections.ObjectModel;
using UKHO.ADDS.Configuration.Schema;

namespace UKHO.ADDS.Configuration.Services
{
    internal class ConfigurationStore
    {
        private readonly ConfigurationReader _reader;
        private readonly IConfiguration _configuration;
        private readonly ObservableCollection<StoredServiceConfiguration> _serviceConfigurations;
        private AddsEnvironment? _environment;

        public ConfigurationStore(ConfigurationReader reader, IConfiguration configuration)
        {
            _reader = reader;
            _configuration = configuration;
            _serviceConfigurations = [];
        }

        public ObservableCollection<StoredServiceConfiguration> Configuration => _serviceConfigurations;

        public async Task InitialiseAsync(AddsEnvironment environment)
        {
            _environment = environment;
            var configuration = await _reader.ReadConfigurationAsync(environment);

            _serviceConfigurations.Clear();
            foreach (var serviceConfiguration in configuration)
            {
                _serviceConfigurations.Add(serviceConfiguration);
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
