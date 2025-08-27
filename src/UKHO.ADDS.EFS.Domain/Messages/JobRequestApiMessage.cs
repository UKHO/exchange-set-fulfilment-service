using UKHO.ADDS.EFS.Jobs;
using UKHO.ADDS.EFS.VOS;

namespace UKHO.ADDS.EFS.Messages
{
    /// <summary>
    /// <see cref="JobRequestApiMessage"/> is received via the Request API (and later converted into a <see cref="JobRequestQueueMessage"/>.
    /// </summary>
    public class JobRequestApiMessage
    {
        public MessageVersion Version { get; init; } = MessageVersion.From(1);

        public DataStandard DataStandard { get; set; }

        public required ProductNameList Products { get; set; }

        public required string Filter { get; set; }
    }
}
