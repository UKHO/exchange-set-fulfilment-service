using Azure.Messaging.EventGrid;

namespace UKHO.ADDS.Configuration.AACEmulator.Messaging.EventGrid
{
    public interface IEventGridEventFactory
    {
        public EventGridEvent Create(string eventType, string dataVersion, BinaryData data);
    }
}
