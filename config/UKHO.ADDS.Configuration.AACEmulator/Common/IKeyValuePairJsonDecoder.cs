using System.Text.Json;

namespace UKHO.ADDS.Configuration.AACEmulator.Common
{
    public interface IKeyValuePairJsonDecoder
    {
        IEnumerable<KeyValuePair<string, string?>> Decode(
            JsonDocument document,
            string? prefix = null,
            string? separator = null);
    }
}
