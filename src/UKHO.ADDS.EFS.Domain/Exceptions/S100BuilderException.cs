using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace UKHO.ADDS.EFS.Exceptions
{
    [ExcludeFromCodeCoverage]
    public class S100BuilderException : Exception
    {
        public EventId EventId { get; set; }

        public object[] MessageArguments { get; }

        public S100BuilderException(string message, params object[] messageArguments) : base(message)
        {
            MessageArguments = messageArguments ?? [];
        }
        public S100BuilderException(EventId eventId, string message, params object[] messageArguments) : base(message)
        {
            EventId = eventId;
            MessageArguments = messageArguments ?? [];
        }
    }
}
