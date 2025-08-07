using UKHO.ADDS.EFS.Jobs;

namespace UKHO.ADDS.EFS.Messages
{
    /// <summary>
    /// <see cref="JobRequestApiMessage"/> is received via the Request API (and later converted into a <see cref="JobRequestQueueMessage"/>.
    /// </summary>
    public class JobRequestApiMessage
    {
        public required int Version { get; init; } = 1;

        public DataStandard DataStandard { get; set; }

        public required string[] Products { get; set; }

        public required string Filter { get; set; }
    }
}
