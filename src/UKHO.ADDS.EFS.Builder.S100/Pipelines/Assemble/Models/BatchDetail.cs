using System.Text.Json.Serialization;
using UKHO.ADDS.EFS.Builder.S100.Pipelines.Assemble;

namespace UKHO.ADDS.EFS.Builder.S100.Pipelines.Assemble.Models
{
    public class BatchDetail
    {
        public string BatchId { get; set; }

        public string Status { get; set; }

        public IEnumerable<Attribute> Attributes { get; set; }

        public string BusinessUnit { get; set; }

        public DateTime? BatchPublishedDate { get; set; }

        public DateTime? ExpiryDate { get; set; }

        public IEnumerable<BatchFile> Files { get; set; }
        
    }
}
