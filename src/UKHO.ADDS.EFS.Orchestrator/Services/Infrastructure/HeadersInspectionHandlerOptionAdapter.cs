using Microsoft.Kiota.Http.HttpClientLibrary.Middleware.Options;

namespace UKHO.ADDS.EFS.Orchestrator.Services.Infrastructure;

/// <summary>
///     Adapter that wraps the Kiota HeadersInspectionHandlerOption to implement our interface.
/// </summary>
/// <remarks>
///     This adapter allows us to use dependency injection and interface-based programming
///     with the Kiota middleware option while maintaining loose coupling.
/// </remarks>
internal class HeadersInspectionHandlerOptionAdapter : IHeadersInspectionHandlerOption
{
    private readonly HeadersInspectionHandlerOption _headersInspectionHandlerOption;

    /// <summary>
    ///     Initializes a new instance of the <see cref="HeadersInspectionHandlerOptionAdapter" /> class.
    /// </summary>
    /// <param name="headersInspectionHandlerOption">The Kiota headers inspection handler option.</param>
    public HeadersInspectionHandlerOptionAdapter(HeadersInspectionHandlerOption headersInspectionHandlerOption)
    {
        _headersInspectionHandlerOption = headersInspectionHandlerOption ?? throw new ArgumentNullException(nameof(headersInspectionHandlerOption));
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="HeadersInspectionHandlerOptionAdapter" /> class
    ///     with a default HeadersInspectionHandlerOption.
    /// </summary>
    public HeadersInspectionHandlerOptionAdapter() 
        : this(new HeadersInspectionHandlerOption())
    {
    }

    /// <summary>
    ///     Gets or sets a value indicating whether to inspect request headers.
    /// </summary>
    public bool InspectRequestHeaders 
    { 
        get => _headersInspectionHandlerOption.InspectRequestHeaders; 
        set => _headersInspectionHandlerOption.InspectRequestHeaders = value; 
    }

    /// <summary>
    ///     Gets or sets a value indicating whether to inspect response headers.
    /// </summary>
    public bool InspectResponseHeaders 
    { 
        get => _headersInspectionHandlerOption.InspectResponseHeaders; 
        set => _headersInspectionHandlerOption.InspectResponseHeaders = value; 
    }

    /// <summary>
    ///     Gets the request headers that were captured during inspection.
    /// </summary>
    public IDictionary<string, IEnumerable<string>>? RequestHeaders => _headersInspectionHandlerOption.RequestHeaders;

    /// <summary>
    ///     Gets the response headers that were captured during inspection.
    /// </summary>
    public IDictionary<string, IEnumerable<string>>? ResponseHeaders => _headersInspectionHandlerOption.ResponseHeaders;
}
