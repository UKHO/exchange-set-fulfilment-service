using Microsoft.Kiota.Http.HttpClientLibrary.Middleware.Options;
using UKHO.ADDS.Clients.Kiota.SalesCatalogueService;

namespace UKHO.ADDS.EFS.Orchestrator.Services.Infrastructure;

/// <summary>
///     Factory for creating instances of <see cref="IOrchestratorSalesCatalogueClient" />.
/// </summary>
internal class OrchestratorSalesCatalogueClientFactory : IOrchestratorSalesCatalogueClientFactory
{
    private readonly ILogger<OrchestratorSalesCatalogueClient> _logger;

    /// <summary>
    ///     Initializes a new instance of the <see cref="OrchestratorSalesCatalogueClientFactory" /> class.
    /// </summary>
    /// <param name="logger">Logger for the orchestrator sales catalogue client.</param>
    public OrchestratorSalesCatalogueClientFactory(ILogger<OrchestratorSalesCatalogueClient> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    ///     Creates a new instance of <see cref="IOrchestratorSalesCatalogueClient" />.
    /// </summary>
    /// <param name="kiotaSalesCatalogueService">The Kiota sales catalogue service.</param>
    /// <param name="headersInspectionHandlerOption">The headers inspection handler options.</param>
    /// <returns>A new instance of <see cref="IOrchestratorSalesCatalogueClient" />.</returns>
    public IOrchestratorSalesCatalogueClient Create(
        IKiotaSalesCatalogueService kiotaSalesCatalogueService,
        IHeadersInspectionHandlerOption headersInspectionHandlerOption
        )

    {
        return new OrchestratorSalesCatalogueClient(kiotaSalesCatalogueService, headersInspectionHandlerOption, _logger);
    }
}
