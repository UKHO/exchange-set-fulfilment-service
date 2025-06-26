using System.Collections.ObjectModel;
using UKHO.ADDS.Configuration.Schema;

namespace UKHO.ADDS.Configuration.Services
{
    internal class ConfigurationService
    {
        private readonly ConfigurationReader _reader;
        private readonly ObservableCollection<StoredServiceConfiguration> _configuration;
        private AddsEnvironment? _environment;

        public ConfigurationService(ConfigurationReader reader)
        {
            _reader = reader;
            _configuration = [];
        }

        public ObservableCollection<StoredServiceConfiguration> Configuration => _configuration;

        public async Task InitialiseAsync(AddsEnvironment environment)
        {
            _environment = environment;
            var configuration = await _reader.ReadConfigurationAsync(environment);

            _configuration.Clear();
            foreach (var serviceConfiguration in configuration)
            {
                _configuration.Add(serviceConfiguration);
            }
        }
    }
}
