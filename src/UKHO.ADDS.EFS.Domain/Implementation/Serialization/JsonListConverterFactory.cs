using System.Text.Json;
using System.Text.Json.Serialization;

namespace UKHO.ADDS.EFS.Domain.Implementation.Serialization
{
    /// <summary>
    ///     Converter factory that recognizes any type implementing IJsonListWrapper
    ///     and serializes/deserializes it as a bare JSON array.
    /// </summary>
    internal sealed class JsonListConverterFactory : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert) => FindListWrapperInterface(typeToConvert) is not null;

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            var iFace = FindListWrapperInterface(typeToConvert) ?? throw new InvalidOperationException($"Type {typeToConvert} does not implement IJsonListWrapper<T>.");

            var elementType = iFace.GetGenericArguments()[0];
            var converterType = typeof(JsonListConverter<,>).MakeGenericType(typeToConvert, elementType);

            return (JsonConverter)Activator.CreateInstance(converterType)!;
        }

        private static Type? FindListWrapperInterface(Type t) => t.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IJsonList<>));
    }
}
