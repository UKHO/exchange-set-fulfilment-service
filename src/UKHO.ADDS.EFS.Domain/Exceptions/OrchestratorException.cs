using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace UKHO.ADDS.EFS.Domain.Exceptions
{
    [ExcludeFromCodeCoverage]
    public class OrchestratorException : Exception
    {
        public OrchestratorException(string message, params object[] messageArguments)
            : base(message)
        {
            MessageArguments = messageArguments ?? [];
        }

        public OrchestratorException(EventId eventId, string message, params object[] messageArguments)
            : base(message)
        {
            EventId = eventId;
            MessageArguments = messageArguments ?? [];
        }

        public EventId EventId { get; set; }

        public object[] MessageArguments { get; }
    }
}
