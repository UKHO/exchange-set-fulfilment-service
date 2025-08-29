using Microsoft.Extensions.Diagnostics.HealthChecks;
using UKHO.ADDS.Aspire.Configuration.Remote;
using UKHO.ADDS.EFS.Domain.Services.Configuration.Namespaces;

namespace UKHO.ADDS.EFS.Orchestrator.Health;

/// <summary>
/// Health check for File Share Service connectivity
/// </summary>
internal class FileShareServiceHealthCheck : BaseServiceHealthCheck
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FileShareServiceHealthCheck"/> class.
    /// </summary>
    /// <param name="httpClientFactory">HTTP client factory for creating HTTP clients.</param>
    /// <param name="externalServiceRegistry">Registry for retrieving service endpoint information.</param>
    /// <param name="logger">Logger for recording diagnostic information.</param>
    public FileShareServiceHealthCheck(
        IHttpClientFactory httpClientFactory,
        IExternalServiceRegistry externalServiceRegistry,
        ILogger<FileShareServiceHealthCheck> logger)
        : base(httpClientFactory, externalServiceRegistry, logger)
    {
    }

    protected override string ServiceName => "File Share Service";
    protected override string ProcessName => ProcessNames.FileShareService;
}
