using UKHO.ADDS.EFS.Common.Messages;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace UKHO.ADDS.EFS.Common.Entities
{
    public class ExchangeSetRequest
    {

        public string Id { get; set; }

        public string Timestamp { get; set; }

        public ExchangeSetRequestMessage Message { get; set; }
    }
}
