using Microsoft.Extensions.Hosting;
using UKHO.ADDS.Configuration.Schema;

namespace UKHO.ADDS.Configuration.Seeder
{
    internal class LocalSeederService : IHostedService
    {
        private readonly IHostApplicationLifetime _hostApplicationLifetime;
        private readonly ConfigurationWriter _writer;
        private readonly ExternalServiceDiscoWriter _discoWriter;
        private readonly string _configFilePath;
        private readonly string _discoFilePath;

        public LocalSeederService(IHostApplicationLifetime hostApplicationLifetime, ConfigurationWriter writer, ExternalServiceDiscoWriter discoWriter, string configFilePath, string discoFilePath)
        {
            _hostApplicationLifetime = hostApplicationLifetime;
            _writer = writer;
            _discoWriter = discoWriter;
            _configFilePath = configFilePath;
            _discoFilePath = discoFilePath;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var configJson = await File.ReadAllTextAsync(_configFilePath, cancellationToken);
            await _writer.WriteConfigurationAsync(AddsEnvironment.Local, configJson);

            var templateDiscoJson = await File.ReadAllTextAsync(_discoFilePath, cancellationToken);

            var endpoints = await ExternalServiceDiscoParser.ParseAndResolveAsync(AddsEnvironment.Local, templateDiscoJson);

            await _discoWriter.WriteExternalServiceDiscoAsync(endpoints);

            _hostApplicationLifetime.StopApplication();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
