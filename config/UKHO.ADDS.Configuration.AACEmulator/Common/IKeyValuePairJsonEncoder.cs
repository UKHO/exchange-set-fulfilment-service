using System.Text.Json;

namespace UKHO.ADDS.Configuration.AACEmulator.Common
{
    public interface IKeyValuePairJsonEncoder
    {
        JsonDocument Encode(
            IEnumerable<KeyValuePair<string, string?>> pairs,
            string? prefix = null,
            string? separator = null);
    }
}
