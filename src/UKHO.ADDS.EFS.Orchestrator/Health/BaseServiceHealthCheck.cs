using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Net.Http.Headers;
using UKHO.ADDS.Aspire.Configuration.Remote;
using UKHO.ADDS.Clients.Common.Authentication;
using UKHO.ADDS.Clients.Common.Constants;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging;

namespace UKHO.ADDS.EFS.Orchestrator.Health
{
    /// <summary>
    /// Base class for service health checks that test connectivity to external services
    /// </summary>
    internal abstract class BaseServiceHealthCheck : IHealthCheck
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IExternalServiceRegistry _externalServiceRegistry;
        private readonly ILogger _logger;
        private readonly IAuthenticationTokenProvider? _authenticationTokenProvider;

        protected BaseServiceHealthCheck(
            IHttpClientFactory httpClientFactory,
            IExternalServiceRegistry externalServiceRegistry,
            ILogger logger,
            IAuthenticationTokenProvider? authenticationTokenProvider = null)
        {
            _httpClientFactory = httpClientFactory;
            _externalServiceRegistry = externalServiceRegistry;
            _logger = logger;
            _authenticationTokenProvider = authenticationTokenProvider;
        }

        /// <summary>
        /// Gets the service name for display in health check results
        /// </summary>
        protected abstract string ServiceName { get; }

        /// <summary>
        /// Gets the process name used to lookup the service endpoint
        /// </summary>
        protected abstract string ProcessName { get; }

        /// <summary>
        /// Gets additional endpoint parameters for service lookup
        /// </summary>
        protected virtual string EndpointParameters => string.Empty;

        /// <summary>
        /// Gets the endpoint host substitution strategy
        /// </summary>
        protected virtual EndpointHostSubstitution HostSubstitution => EndpointHostSubstitution.None;

        /// <summary>
        /// Performs a health check by testing connectivity to the configured service.
        /// </summary>
        /// <param name="context">A context object associated with the current health check.</param>
        /// <param name="cancellationToken">A System.Threading.CancellationToken that can be used to cancel the health check.</param>
        /// <returns>A Task that completes when the health check has finished, yielding the health check result.</returns>
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var endpoint = _externalServiceRegistry.GetServiceEndpoint(ProcessName, EndpointParameters, HostSubstitution);
                var healthEndpointUri = $"{endpoint.Uri!}health";
                var httpClient = _httpClientFactory.CreateClient();

                // Set authentication token in the request headers if provider is available
                if (_authenticationTokenProvider != null)
                {
                    var token = await _authenticationTokenProvider.GetTokenAsync();
                    if (!string.IsNullOrEmpty(token))
                    {
                        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(ApiHeaderKeys.BearerTokenHeaderKey, token);
                    }
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
