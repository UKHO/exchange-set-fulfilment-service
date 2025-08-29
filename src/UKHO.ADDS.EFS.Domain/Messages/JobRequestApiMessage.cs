using System.Text.Json.Serialization;
using UKHO.ADDS.EFS.Domain.Jobs;
using UKHO.ADDS.EFS.Domain.Products;

namespace UKHO.ADDS.EFS.Domain.Messages
{
    /// <summary>
    /// <see cref="JobRequestApiMessage"/> is received via the Request API (and later converted into a <see cref="JobRequestQueueMessage"/>.
    /// </summary>
    public class JobRequestApiMessage
    {
        public DataStandard DataStandard { get; set; }

        [JsonPropertyName("products")]
        public required ProductNameList Products { get; set; }

        public required string Filter { get; set; }
    }
}
