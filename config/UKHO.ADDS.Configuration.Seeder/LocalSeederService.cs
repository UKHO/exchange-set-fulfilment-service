using Azure.Data.AppConfiguration;
using Microsoft.Extensions.Hosting;
using UKHO.ADDS.Configuration.Schema;
using UKHO.ADDS.Configuration.Seeder.Json;

namespace UKHO.ADDS.Configuration.Seeder
{
    internal class LocalSeederService : IHostedService
    {
        private readonly IHostApplicationLifetime _hostApplicationLifetime;
        private readonly string _prefix;
        private readonly ConfigurationClient _configurationClient;
        private readonly ConfigurationWriter _writer;
        private readonly ExternalServiceDiscoWriter _discoWriter;
        private readonly string _configFilePath;
        private readonly string _newConfigFilePath;
        private readonly string _discoFilePath;

        public LocalSeederService(IHostApplicationLifetime hostApplicationLifetime, string prefix, ConfigurationClient configurationClient, ConfigurationWriter writer, ExternalServiceDiscoWriter discoWriter, string configFilePath, string newConfigFilePath, string discoFilePath)
        {
            _hostApplicationLifetime = hostApplicationLifetime;
            _prefix = prefix;
            _configurationClient = configurationClient;
            _writer = writer;
            _discoWriter = discoWriter;
            _configFilePath = configFilePath;
            _newConfigFilePath = newConfigFilePath;
            _discoFilePath = discoFilePath;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var newConfigJson = await File.ReadAllTextAsync(_newConfigFilePath, cancellationToken);
            var newConfigJsonCleaned = JsonStripper.StripJsonComments(newConfigJson);

            var flattenedConfig = JsonFlattener.Flatten(AddsEnvironment.Local, newConfigJsonCleaned);

            var templateDiscoJson = await File.ReadAllTextAsync(_discoFilePath, cancellationToken);
            var endpoints = await ExternalServiceDiscoParser.ParseAndResolveAsync(AddsEnvironment.Local, templateDiscoJson);

            await AzureConfigurationWriter.WriteConfiguration(_prefix, _configurationClient, flattenedConfig, endpoints);

            // TODO Remove old config code
            var configJson = await File.ReadAllTextAsync(_configFilePath, cancellationToken);
            await _writer.WriteConfigurationAsync(AddsEnvironment.Local, configJson);

            await _discoWriter.WriteExternalServiceDiscoAsync(endpoints);

            _hostApplicationLifetime.StopApplication();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
