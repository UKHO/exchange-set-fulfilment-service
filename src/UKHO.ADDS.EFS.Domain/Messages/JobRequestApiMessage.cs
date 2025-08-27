using UKHO.ADDS.EFS.Jobs;
using UKHO.ADDS.EFS.Products;

namespace UKHO.ADDS.EFS.Messages
{
    /// <summary>
    /// <see cref="JobRequestApiMessage"/> is received via the Request API (and later converted into a <see cref="JobRequestQueueMessage"/>.
    /// </summary>
    public class JobRequestApiMessage
    {
        public DataStandard DataStandard { get; set; }

        public required ProductNameList Products { get; set; }

        public required string Filter { get; set; }
    }
}
