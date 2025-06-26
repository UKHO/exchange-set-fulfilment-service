using System.Collections.ObjectModel;
using Grpc.Core;
using UKHO.ADDS.Configuration.Grpc;

namespace UKHO.ADDS.Configuration.Services
{
    internal class ConfigurationService : Grpc.ConfigurationService.ConfigurationServiceBase
    {
        private readonly ObservableCollection<StoredServiceConfiguration> _configuration;

        public ConfigurationService(ConfigurationStore service)
        {
            _configuration = service.Configuration;
        }

        public override Task<ServiceConfigurationResponse> GetConfiguration(ServiceConfigurationRequest request, ServerCallContext context)
        {
            var response = new ServiceConfigurationResponse();

            var service = _configuration.FirstOrDefault(svc =>
                string.Equals(svc.ServiceName, request.ServiceName, StringComparison.OrdinalIgnoreCase));

            if (service != null)
            {
                response.Properties.AddRange(
                    service.Properties.Values.Select(p => new Property
                    {
                        Path = p.Path,
                        Value = p.Value ?? string.Empty
                    }));
            }

            return Task.FromResult(response);
        }
    }
}
