using System.Net;
using System.Text.Json.Serialization;

namespace UKHO.ADDS.EFS.Products
{
    public class ProductEditionList
    {
        public ProductCountSummary ProductCountSummary { get; set; } = new ProductCountSummary();
        
        public List<ProductEdition> Products { get; set; } = new List<ProductEdition>();
        
        [JsonIgnore]
        public HttpStatusCode ResponseCode { get; set; }
    }
}
