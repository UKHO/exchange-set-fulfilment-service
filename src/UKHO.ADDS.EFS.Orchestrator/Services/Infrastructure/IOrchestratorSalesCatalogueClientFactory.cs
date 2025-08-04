using Microsoft.Kiota.Http.HttpClientLibrary.Middleware.Options;

namespace UKHO.ADDS.EFS.Orchestrator.Services.Infrastructure;

/// <summary>
///     Factory interface for creating instances of <see cref="IOrchestratorSalesCatalogueClient" />.
/// </summary>
internal interface IOrchestratorSalesCatalogueClientFactory
{
    /// <summary>
    ///     Creates a new instance of <see cref="IOrchestratorSalesCatalogueClient" />.
    /// </summary>
    /// <param name="kiotaSalesCatalogueService">The Kiota sales catalogue service.</param>
    /// <param name="headersInspectionHandlerOption">The headers inspection handler options.</param>
    /// <returns>A new instance of <see cref="IOrchestratorSalesCatalogueClient" />.</returns>
    IOrchestratorSalesCatalogueClient Create(
        IKiotaSalesCatalogueService kiotaSalesCatalogueService,
        IHeadersInspectionHandlerOption headersInspectionHandlerOption);
}
