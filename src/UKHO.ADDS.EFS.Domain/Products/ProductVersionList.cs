using System.Net;

namespace UKHO.ADDS.EFS.Products
{
    public class ProductVersionList
    {
        public List<ProductVersion> ResponseBody { get; set; }
        public DateTime? LastModified { get; set; }
        public HttpStatusCode ResponseCode { get; set; }
    }
}
