using System.Net;
using UKHO.ADDS.EFS.Jobs;
using UKHO.ADDS.EFS.Orchestrator.Jobs;

namespace UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging;

internal class SalesCatalogUnexpectedStatusLogView
{
    public DataStandard DataStandard { get; init; }

    public required string Products { get; init; }

    public required string CorrelationId { get; init; }

    public HttpStatusCode StatusCode { get; init; }

    public static SalesCatalogUnexpectedStatusLogView Create(Job job, HttpStatusCode statusCode) =>
        new()
        {
            DataStandard = job.DataStandard,
            Products = job.RequestedProducts.ToString(),
            CorrelationId = job.GetCorrelationId(),
            StatusCode = statusCode
        };
}
