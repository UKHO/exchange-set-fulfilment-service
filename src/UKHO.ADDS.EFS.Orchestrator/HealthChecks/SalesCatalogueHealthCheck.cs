using Microsoft.Extensions.Diagnostics.HealthChecks;
using UKHO.ADDS.Aspire.Configuration.Remote;
using UKHO.ADDS.EFS.Configuration.Namespaces;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging;

namespace UKHO.ADDS.EFS.Orchestrator.HealthChecks
{
    /// <summary>
    /// Health check for Sales Catalogue Service connectivity
    /// </summary>
    public class SalesCatalogueHealthCheck : IHealthCheck
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IExternalServiceRegistry _externalServiceRegistry;
        private readonly ILogger<SalesCatalogueHealthCheck> _logger;
        private const string ServiceName = "Sales Catalogue Service";

        /// <summary>
        /// Initializes a new instance of the <see cref="SalesCatalogueHealthCheck"/> class.
        /// </summary>
        /// <param name="httpClientFactory">HTTP client factory for creating HTTP clients.</param>
        /// <param name="externalServiceRegistry">Registry for retrieving service endpoint information.</param>
        /// <param name="logger">Logger for recording diagnostic information.</param>
        public SalesCatalogueHealthCheck(
            IHttpClientFactory httpClientFactory,
            IExternalServiceRegistry externalServiceRegistry,
            ILogger<SalesCatalogueHealthCheck> logger)
        {
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _externalServiceRegistry = externalServiceRegistry ?? throw new ArgumentNullException(nameof(externalServiceRegistry));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Performs a health check by testing connectivity to the Sales Catalogue Service.
        /// </summary>
        /// <param name="context">A context object associated with the current health check.</param>
        /// <param name="cancellationToken">A System.Threading.CancellationToken that can be used to cancel the health check.</param>
        /// <returns>A Task that completes when the health check has finished, yielding the health check result.</returns>
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var endpoint = await _externalServiceRegistry.GetServiceEndpointAsync(ProcessNames.SalesCatalogueService);
                var healthEndpointUri = $"{endpoint.Uri!}health";
                var httpClient = _httpClientFactory.CreateClient();
                
                using var response = await httpClient.GetAsync(healthEndpointUri, cancellationToken);
                
                if (response.IsSuccessStatusCode)
                {
                    return HealthCheckResult.Healthy($"{ServiceName} responded with {response.StatusCode}");
                }
                else
                {
                    var errorMessage = $"Service returned status code {response.StatusCode}";
                    _logger.LogHealthCheckFailedStatusCode(ServiceName, (int)response.StatusCode);
                    return HealthCheckResult.Unhealthy($"{ServiceName} health check failed", 
                        new Exception(errorMessage));
                }
            }
            catch (Exception ex)
            {
                _logger.LogHealthCheckError(ServiceName, ex);
                return HealthCheckResult.Unhealthy($"{ServiceName} health check failed", ex);
            }
        }
    }
}
