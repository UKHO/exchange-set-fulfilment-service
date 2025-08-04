using UKHO.ADDS.Clients.Kiota.SalesCatalogueService.V2;

namespace UKHO.ADDS.EFS.Orchestrator.Services.Infrastructure;

/// <summary>
///     Interface for the Kiota-generated Sales Catalogue Service client.
/// </summary>
/// <remarks>
///     This interface abstracts the Kiota-generated KiotaSalesCatalogueService to enable
///     dependency injection, unit testing, and loose coupling. The concrete implementation
///     is typically a Kiota-generated client that provides access to Sales Catalogue API endpoints.
/// </remarks>
internal interface IKiotaSalesCatalogueService
{
    /// <summary>
    ///     Gets the V2 API endpoints for the Sales Catalogue Service.
    /// </summary>
    V2RequestBuilder V2 { get; }
}
