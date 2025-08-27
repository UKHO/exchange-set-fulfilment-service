using UKHO.ADDS.EFS.Jobs;

namespace UKHO.ADDS.Clients.SalesCatalogueService.Models
{
    public class ProductIdentifierRequest
    {
        public string[] ProductIdentifier { get; set; }
        public string CallbackUri { get; set; }
        public DataStandard ExchangeSetStandard { get; set; }
    }
}
