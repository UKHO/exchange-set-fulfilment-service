using System.Net;
using UKHO.ADDS.EFS.Messages;

namespace UKHO.ADDS.EFS.Orchestrator.Logging
{
    internal class SalesCatalogueServiceLog
    {
        public HttpStatusCode ResponseCode { get; set; }
        public string CorrelationId { get; set; }
        public ExchangeSetRequestQueueMessage Message { get; set; }
    }
}
