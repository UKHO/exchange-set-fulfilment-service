
using UKHO.ADDS.EFS.Domain.Products;

namespace UKHO.ADDS.EFS.Builder.S100.Pipelines.Assemble.Models
{
    internal class BatchProductDetail
    {
        public ProductName ProductName { get; set; }
        public EditionNumber EditionNumber { get; set; }
        public IEnumerable<int?> UpdateNumbers { get; set; }
    }
}
