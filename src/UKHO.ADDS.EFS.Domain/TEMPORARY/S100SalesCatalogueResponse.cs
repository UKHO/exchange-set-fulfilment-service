using System.Net;

namespace UKHO.ADDS.Clients.SalesCatalogueService.Models
{
    public class S100SalesCatalogueResponse
    {
        public List<S100Products> ResponseBody { get; set; }
        public DateTime? LastModified { get; set; }
        public HttpStatusCode ResponseCode { get; set; }

    }
}
