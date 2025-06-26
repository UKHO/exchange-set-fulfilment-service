using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UKHO.ADDS.EFS.Messages
{
    public class BuilderRequestQueueMessage
    {
        public required string JobId { get; set; }
        public required string StorageAddress { get; set; }
        public required string BatchId { get; set; }
        public required string CorrelationId { get; set; }
    }
}
