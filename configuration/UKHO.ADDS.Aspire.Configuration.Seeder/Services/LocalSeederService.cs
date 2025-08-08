using Azure.Data.AppConfiguration;
using Microsoft.Extensions.Hosting;

namespace UKHO.ADDS.Aspire.Configuration.Seeder.Services
{
    internal class LocalSeederService : IHostedService
    {
        private readonly IHostApplicationLifetime _hostApplicationLifetime;
        private readonly ConfigurationService _configService;
        private readonly string _serviceName;
        private readonly ConfigurationClient _configurationClient;
        private readonly string _configFilePath;
        private readonly string _servicesFilePath;

        public LocalSeederService(IHostApplicationLifetime hostApplicationLifetime, ConfigurationService configService, string serviceName, ConfigurationClient configurationClient, string configFilePath, string servicesFilePath)
        {
            _hostApplicationLifetime = hostApplicationLifetime;
            _configService = configService;
            _serviceName = serviceName;
            _configurationClient = configurationClient;
            _configFilePath = configFilePath;
            _servicesFilePath = servicesFilePath;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _configService.SeedConfigurationAsync(AddsEnvironment.Local, _configurationClient, _serviceName, _configFilePath, _servicesFilePath, cancellationToken);

            _hostApplicationLifetime.StopApplication();
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
