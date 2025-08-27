using System.Net;

namespace UKHO.ADDS.EFS.Products
{
    public class ProductList
    {
        public List<Product> Products { get; set; }
        public DateTime? LastModified { get; set; }
        public HttpStatusCode ResponseCode { get; set; }
    }
}
