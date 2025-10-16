using System.Net;
using System.Text.Json.Serialization;
using UKHO.ADDS.EFS.Domain.Jobs;

namespace UKHO.ADDS.EFS.Domain.Files
{
    public class Batch
    {
        public required BatchId BatchId { get; init; }
        public DateTime BatchExpiryDateTime { get; set; } = DateTime.MinValue;
        public HttpStatusCode ResponseCode { get; set; } = HttpStatusCode.OK;
    }
}
