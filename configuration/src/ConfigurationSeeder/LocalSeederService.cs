using Microsoft.Extensions.Hosting;
using UKHO.ADDS.Configuration.Schema;

namespace ConfigurationSeeder
{
    internal class LocalSeederService : IHostedService
    {
        private readonly IHostApplicationLifetime _hostApplicationLifetime;
        private readonly ConfigurationWriter _writer;
        private readonly string _configFilePath;

        public LocalSeederService(IHostApplicationLifetime hostApplicationLifetime, ConfigurationWriter writer, string configFilePath)
        {
            _hostApplicationLifetime = hostApplicationLifetime;
            _writer = writer;
            _configFilePath = configFilePath;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var json = await File.ReadAllTextAsync(_configFilePath, cancellationToken);
            await _writer.WriteConfigurationAsync(AddsEnvironment.Local, json);

            _hostApplicationLifetime.StopApplication();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
