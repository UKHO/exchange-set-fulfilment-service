using UKHO.ADDS.EFS.Common.Messages;

namespace UKHO.ADDS.EFS.Common.Entities
{
    public class ExchangeSetRequest
    {

        public string Id { get; set; }

        public ExchangeSetRequestMessage Message { get; set; }
    }
}
