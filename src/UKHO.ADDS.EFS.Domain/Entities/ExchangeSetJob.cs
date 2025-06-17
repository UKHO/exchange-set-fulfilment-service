using UKHO.ADDS.Clients.SalesCatalogueService.Models;
using UKHO.ADDS.EFS.Messages;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace UKHO.ADDS.EFS.Entities
{
    public class ExchangeSetJob
    {
        public string Id { get; set; }

        public List<S100Products> Products { get; set; }

        public DateTime Timestamp { get; set; }

        public DateTime? SalesCatalogueTimestamp { get; set; }

        public ExchangeSetJobState State { get; set; }

        public ExchangeSetDataStandard DataStandard { get; set; }

        public string BatchId { get; set; }

        public string CorrelationId { get; set; }
    }
}
