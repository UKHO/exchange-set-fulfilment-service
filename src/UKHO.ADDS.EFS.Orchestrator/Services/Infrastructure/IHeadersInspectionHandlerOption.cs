using Microsoft.Kiota.Abstractions;

namespace UKHO.ADDS.EFS.Orchestrator.Services.Infrastructure;

/// <summary>
///     Interface for HTTP headers inspection handler options used in Kiota middleware.
/// </summary>
/// <remarks>
///     This interface abstracts the Microsoft Kiota HeadersInspectionHandlerOption class
///     to enable dependency injection, unit testing, and loose coupling. The concrete
///     implementation is typically the HeadersInspectionHandlerOption from the Kiota
///     HTTP client library middleware.
/// </remarks>
internal interface IHeadersInspectionHandlerOption : IRequestOption
{
    /// <summary>
    ///     Gets or sets a value indicating whether to inspect request headers.
    /// </summary>
    bool InspectRequestHeaders { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether to inspect response headers.
    /// </summary>
    bool InspectResponseHeaders { get; set; }

    /// <summary>
    ///     Gets the request headers that were captured during inspection.
    /// </summary>
    IDictionary<string, IEnumerable<string>>? RequestHeaders { get; }

    /// <summary>
    ///     Gets the response headers that were captured during inspection.
    /// </summary>
    IDictionary<string, IEnumerable<string>>? ResponseHeaders { get; }
}
