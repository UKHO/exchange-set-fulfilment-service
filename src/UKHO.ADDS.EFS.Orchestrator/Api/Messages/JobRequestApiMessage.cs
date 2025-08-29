using UKHO.ADDS.EFS.Domain.Jobs;

namespace UKHO.ADDS.EFS.Domain.Messages
{
    /// <summary>
    /// <see cref="JobRequestApiMessage"/> is received via the Request API (and later converted into a <see cref="JobRequestQueueMessage"/>.
    /// </summary>
    public class JobRequestApiMessage
    {
        public DataStandard DataStandard { get; set; }

        public required string[] Products { get; set; } = [];

        public required string Filter { get; set; }
    }
}
