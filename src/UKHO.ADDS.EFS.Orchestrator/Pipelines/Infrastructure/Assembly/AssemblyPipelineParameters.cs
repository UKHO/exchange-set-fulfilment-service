using UKHO.ADDS.EFS.Jobs;
using UKHO.ADDS.EFS.Messages;
using UKHO.ADDS.EFS.Orchestrator.Jobs;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Assembly;

internal class AssemblyPipelineParameters
{
    public required int Version { get; init; }

    public required DateTime Timestamp { get; init; }

    public required DataStandard DataStandard { get; init; }

    public required string Products { get; init; }

    public required string Filter { get; init; }

    public required string JobId { get; init; }

    public required IConfiguration Configuration { get; init; }

    /// <summary>
    /// The original request type for S100 endpoints
    /// </summary>
    public Messages.RequestType? RequestType { get; init; }

    /// <summary>
    /// The callback URI for asynchronous notifications
    /// </summary>
    public string? CallbackUri { get; init; }

    /// <summary>
    /// Product identifier filter for S100 updates since requests (s101, s102, s104, s111)
    /// </summary>
    public string? ProductIdentifier { get; init; }

    public Job CreateJob()
    {
        return new Job()
        {
            Id = JobId,
            Timestamp = Timestamp,
            DataStandard = DataStandard,
            RequestedProducts = Products,
            RequestedFilter = Filter,
            CallbackUri = CallbackUri
        };
    }

    public static AssemblyPipelineParameters CreateFrom(JobRequestApiMessage message, IConfiguration configuration, string correlationId) =>
        new()
        {
            Version = message.Version,
            Timestamp = DateTime.UtcNow,
            DataStandard = message.DataStandard,
            Products = message.Products,
            Filter = message.Filter,
            JobId = correlationId,
            Configuration = configuration
        };

    /// <summary>
    /// Creates parameters from S100 Product Names request
    /// </summary>
    public static AssemblyPipelineParameters CreateFromS100ProductNames(List<string> productNames, IConfiguration configuration, string correlationId, string? callbackUri = null) =>
        new()
        {
            Version = 2,
            Timestamp = DateTime.UtcNow,
            DataStandard = DataStandard.S100,
            Products = string.Join(",", productNames),
            Filter = "productNames",
            JobId = correlationId,
            Configuration = configuration,
            RequestType = Messages.RequestType.ProductNames,
            CallbackUri = callbackUri
        };

    /// <summary>
    /// Creates parameters from S100 Product Versions request
    /// </summary>
    public static AssemblyPipelineParameters CreateFromS100ProductVersions(S100ProductVersionsRequest request, IConfiguration configuration, string correlationId, string? callbackUri = null) =>
        new()
        {
            Version = 2,
            Timestamp = DateTime.UtcNow,
            DataStandard = DataStandard.S100,
            Products = string.Join(",", request.ProductVersions.Select(pv => $"{pv.ProductName}:{pv.EditionNumber}:{pv.UpdateNumber}")),
            Filter = "productVersions",
            JobId = correlationId,
            Configuration = configuration,
            RequestType = Messages.RequestType.ProductVersions,
            CallbackUri = callbackUri
        };

    /// <summary>
    /// Creates parameters from S100 Updates Since request
    /// </summary>
    public static AssemblyPipelineParameters CreateFromS100UpdatesSince(S100UpdatesSinceRequest request, IConfiguration configuration, string correlationId, string? productIdentifier = null, string? callbackUri = null) =>
        new()
        {
            Version = 2,
            Timestamp = DateTime.UtcNow,
            DataStandard = DataStandard.S100,
            Products = "all",
            Filter = $"updatesSince:{request.SinceDateTime:O}" + (productIdentifier != null ? $",productIdentifier:{productIdentifier}" : ""),
            JobId = correlationId,
            Configuration = configuration,
            RequestType = Messages.RequestType.UpdatesSince,
            ProductIdentifier = productIdentifier,
            CallbackUri = callbackUri
        };
}
