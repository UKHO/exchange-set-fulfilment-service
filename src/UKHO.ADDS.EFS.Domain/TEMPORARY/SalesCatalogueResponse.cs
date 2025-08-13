using System.Net;

namespace UKHO.ADDS.Clients.SalesCatalogueService.Models
{
    public class SalesCatalogueResponse
    {
        public SalesCatalogueProductResponse ResponseBody { get; set; }
        public HttpStatusCode ResponseCode { get; set; }
        public DateTime? LastModified { get; set; }
        public DateTime ScsRequestDateTime { get; set; }
    }    
}
