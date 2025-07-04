using System.Net;
using UKHO.ADDS.EFS.Messages;

namespace UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging
{
    internal class SalesCatalogueServiceLog
    {
        public HttpStatusCode ResponseCode { get; set; }
        public string CorrelationId { get; set; }
        public JobRequestQueueMessage Message { get; set; }
    }
}
