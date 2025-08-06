using System.Text.Json;

namespace UKHO.ADDS.Aspire.Configuration.Emulator.Common;

public interface IKeyValuePairJsonDecoder
{
    IEnumerable<KeyValuePair<string, string?>> Decode(
        JsonDocument document,
        string? prefix = null,
        string? separator = null);
}
