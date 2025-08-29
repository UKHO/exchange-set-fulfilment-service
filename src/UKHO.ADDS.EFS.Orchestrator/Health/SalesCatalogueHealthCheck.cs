using Microsoft.Extensions.Diagnostics.HealthChecks;
using UKHO.ADDS.Aspire.Configuration.Remote;
using UKHO.ADDS.Clients.Common.Authentication;
using UKHO.ADDS.EFS.Domain.Services.Configuration.Namespaces;

namespace UKHO.ADDS.EFS.Orchestrator.Health;

/// <summary>
/// Health check for Sales Catalogue Service connectivity
/// </summary>
internal class SalesCatalogueHealthCheck : BaseServiceHealthCheck
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SalesCatalogueHealthCheck"/> class.
    /// </summary>
    /// <param name="httpClientFactory">HTTP client factory for creating HTTP clients.</param>
    /// <param name="externalServiceRegistry">Registry for retrieving service endpoint information.</param>
    /// <param name="logger">Logger for recording diagnostic information.</param>
    /// <param name="authenticationTokenProvider">Provider for authentication tokens when making HTTP requests.</param>
    public SalesCatalogueHealthCheck(
        IHttpClientFactory httpClientFactory,
        IExternalServiceRegistry externalServiceRegistry,
        ILogger<SalesCatalogueHealthCheck> logger,
        IAuthenticationTokenProvider authenticationTokenProvider)
        : base(httpClientFactory, externalServiceRegistry, logger, authenticationTokenProvider)
    {
    }

    protected override string ServiceName => "Sales Catalogue Service";
    protected override string ProcessName => ProcessNames.SalesCatalogueService;
}
