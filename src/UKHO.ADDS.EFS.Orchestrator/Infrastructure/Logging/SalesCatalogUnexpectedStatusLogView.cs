using System.Net;
using UKHO.ADDS.EFS.Jobs;
using UKHO.ADDS.EFS.Messages;

namespace UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging
{
    internal class SalesCatalogUnexpectedStatusLogView
    {
        public ExchangeSetDataStandard DataStandard { get; init; }

        public required string Products { get; init; }

        public required string CorrelationId { get; init; }

        public HttpStatusCode StatusCode { get; init; }

        public static SalesCatalogUnexpectedStatusLogView Create(ExchangeSetJob job, HttpStatusCode statusCode) =>
            new() { DataStandard = job.DataStandard, Products = job.GetProductDelimitedList(), CorrelationId = job.GetCorrelationId(), StatusCode = statusCode };
    }
}
