namespace UKHO.ADDS.Clients.SalesCatalogueService.Models
{
    public class ProductIdentifierRequest
    {
        public string[] ProductIdentifier { get; set; }
        public string CallbackUri { get; set; }
        public string ExchangeSetStandard { get; set; }
        public string CorrelationId { get; set; }
    }
}
