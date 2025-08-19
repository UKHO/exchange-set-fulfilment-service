using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Net.Http.Headers;
using UKHO.ADDS.Aspire.Configuration.Remote;
using UKHO.ADDS.Clients.Common.Authentication;
using UKHO.ADDS.Clients.Common.Constants;
using UKHO.ADDS.EFS.Configuration.Namespaces;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging;

namespace UKHO.ADDS.EFS.Orchestrator.HealthChecks
{
    /// <summary>
    /// Health check for File Share Service connectivity
    /// </summary>
    public class FileShareServiceHealthCheck : IHealthCheck
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IExternalServiceRegistry _externalServiceRegistry;
        private readonly ILogger<FileShareServiceHealthCheck> _logger;
        private readonly IAuthenticationTokenProvider _authenticationTokenProvider;
        private const string ServiceName = "File Share Service";

        /// <summary>
        /// Initializes a new instance of the <see cref="FileShareServiceHealthCheck"/> class.
        /// </summary>
        /// <param name="httpClientFactory">HTTP client factory for creating HTTP clients.</param>
        /// <param name="externalServiceRegistry">Registry for retrieving service endpoint information.</param>
        /// <param name="logger">Logger for recording diagnostic information.</param>
        /// <param name="authenticationTokenProvider">Provider for authentication tokens when making HTTP requests.</param>
        public FileShareServiceHealthCheck(
            IHttpClientFactory httpClientFactory,
            IExternalServiceRegistry externalServiceRegistry,
            ILogger<FileShareServiceHealthCheck> logger,
            IAuthenticationTokenProvider authenticationTokenProvider)
        {
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _externalServiceRegistry = externalServiceRegistry ?? throw new ArgumentNullException(nameof(externalServiceRegistry));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _authenticationTokenProvider = authenticationTokenProvider ?? throw new ArgumentNullException(nameof(authenticationTokenProvider));
        }

        /// <summary>
        /// Performs a health check by testing connectivity to the File Share Service.
        /// </summary>
        /// <param name="context">A context object associated with the current health check.</param>
        /// <param name="cancellationToken">A System.Threading.CancellationToken that can be used to cancel the health check.</param>
        /// <returns>A Task that completes when the health check has finished, yielding the health check result.</returns>
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var endpoint = _externalServiceRegistry.GetServiceEndpoint(ProcessNames.FileShareService);
                var healthEndpointUri = $"{endpoint.Uri!}health";
                var httpClient = _httpClientFactory.CreateClient();
                
                // Set authentication token in the request headers
                var token = await _authenticationTokenProvider.GetTokenAsync();
                if (!string.IsNullOrEmpty(token))
                {
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(ApiHeaderKeys.BearerTokenHeaderKey, token);
                }
                
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
