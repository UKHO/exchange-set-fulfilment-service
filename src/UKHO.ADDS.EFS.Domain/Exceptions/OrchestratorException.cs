using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace UKHO.ADDS.EFS.Exceptions
{
    [ExcludeFromCodeCoverage]
    public class OrchestratorException : Exception
    {
        public EventId EventId { get; set; }

        public object[] MessageArguments { get; }

        public OrchestratorException(EventId eventId, string message, params object[] messageArguments) : base(message)
        {
            EventId = eventId;
            MessageArguments = messageArguments ?? [];
        }
    }
}
