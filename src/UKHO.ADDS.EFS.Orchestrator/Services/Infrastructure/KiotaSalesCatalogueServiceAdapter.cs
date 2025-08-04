using UKHO.ADDS.Clients.Kiota.SalesCatalogueService;
using UKHO.ADDS.Clients.Kiota.SalesCatalogueService.V2;

namespace UKHO.ADDS.EFS.Orchestrator.Services.Infrastructure;

/// <summary>
///     Adapter that wraps the Kiota-generated KiotaSalesCatalogueService to implement our interface.
/// </summary>
/// <remarks>
///     This adapter allows us to use dependency injection and interface-based programming
///     with the Kiota-generated client while maintaining loose coupling.
/// </remarks>
internal class KiotaSalesCatalogueServiceAdapter : IKiotaSalesCatalogueService
{
    private readonly KiotaSalesCatalogueService _kiotaService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="KiotaSalesCatalogueServiceAdapter" /> class.
    /// </summary>
    /// <param name="kiotaService">The Kiota-generated sales catalogue service.</param>
    public KiotaSalesCatalogueServiceAdapter(KiotaSalesCatalogueService kiotaService)
    {
        _kiotaService = kiotaService ?? throw new ArgumentNullException(nameof(kiotaService));
    }

    /// <summary>
    ///     Gets the V2 API endpoints for the Sales Catalogue Service.
    /// </summary>
    public V2RequestBuilder V2 => _kiotaService.V2;
}
