using Microsoft.Extensions.Diagnostics.HealthChecks;
using UKHO.ADDS.Aspire.Configuration.Remote;
using UKHO.ADDS.EFS.Configuration.Namespaces;

namespace UKHO.ADDS.EFS.Orchestrator.HealthCheck
{
    /// <summary>
    /// Health check implementation for the FileShareService
    /// </summary>
    internal class FileShareServiceHealthCheck : IHealthCheck
    {
        private readonly IExternalServiceRegistry _serviceRegistry;
        private readonly IHttpClientFactory _httpClientFactory;

        public FileShareServiceHealthCheck(IExternalServiceRegistry serviceRegistry, IHttpClientFactory httpClientFactory)
        {
            _serviceRegistry = serviceRegistry;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var endpoint = await _serviceRegistry.GetServiceEndpointAsync(ProcessNames.FileShareService);
                var httpClient = _httpClientFactory.CreateClient("FileShareHealthCheck");

                var healthEndpoint = $"{endpoint.Uri!}health";
                var response = await httpClient.GetAsync(healthEndpoint, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    return HealthCheckResult.Healthy("FileShareService is healthy");
                }

                return HealthCheckResult.Unhealthy($"FileShareService returned status code {response.StatusCode}");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("FileShareService health check failed", ex);
            }
        }
    }
}
