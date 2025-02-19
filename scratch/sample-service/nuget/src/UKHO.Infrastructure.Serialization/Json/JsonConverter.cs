using System.Text.Json;
using System.Text.Json.Serialization;

namespace UKHO.Infrastructure.Serialization.Json
{
    public sealed class JsonConverter
    {
        private static readonly JsonSerializerOptions _defaultOptions;
        private static readonly JsonSerializerOptions _defaultOptionsNoFormat;

        static JsonConverter()
        {
            _defaultOptions = new()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true,
                Converters =
                {
                    new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
                }
            };

            _defaultOptionsNoFormat = new()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false,
                Converters =
                {
                    new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
                }
            };
        }

        public static string Serialize<T>(T value) => Serialize(value, _defaultOptions);

        public static string Serialize<T>(T value, JsonSerializerOptions options) => JsonSerializer.Serialize(value, options);

        public static T? Deserialize<T>(string json) => Deserialize<T>(json, _defaultOptions);

        public static T? Deserialize<T>(string json, JsonSerializerOptions options) => JsonSerializer.Deserialize<T>(json, options);

        public static JsonSerializerOptions DefaultOptions => _defaultOptions;

        public static JsonSerializerOptions DefaultOptionsNoFormat => _defaultOptionsNoFormat;
    }
}
