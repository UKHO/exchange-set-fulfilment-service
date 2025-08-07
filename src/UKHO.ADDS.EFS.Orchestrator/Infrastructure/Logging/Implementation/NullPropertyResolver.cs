// British Crown Copyright © 2018,
// All rights reserved.
// 
// You may not copy the Software, rent, lease, sub-license, loan, translate, merge, adapt, vary
// re-compile or modify the Software without written permission from UKHO.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
// OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT
// SHALL CROWN OR THE SECRETARY OF STATE FOR DEFENCE BE LIABLE FOR ANY DIRECT, INDIRECT,
// INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED
// TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR
// BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
// CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING
// IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY
// OF SUCH DAMAGE.

using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace UKHO.Logging.EventHubLogProvider
{
    internal class NullPropertyResolver : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert)
        {
            return !typeToConvert.IsPrimitive && 
                   !typeToConvert.IsEnum && 
                   typeToConvert != typeof(string) && 
                   typeToConvert != typeof(DateTime) && 
                   typeToConvert != typeof(DateTimeOffset) &&
                   !typeof(Exception).IsAssignableFrom(typeToConvert);
        }

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            var converterType = typeof(NullCheckingConverter<>).MakeGenericType(typeToConvert);
            return (JsonConverter)Activator.CreateInstance(converterType)!;
        }

        private class NullCheckingConverter<T> : JsonConverter<T>
        {
            public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                // Default deserialization
                return JsonSerializer.Deserialize<T>(ref reader, options);
            }

            public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
            {
                writer.WriteStartObject();

                var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
                
                foreach (var prop in properties)
                {
                    if (!prop.CanRead)
                        continue;

                    object? propValue = null;
                    bool shouldSerialize = false;

                    try
                    {
                        propValue = prop.GetValue(value);
                        shouldSerialize = true;
                    }
                    catch
                    {
                        // Ignore properties that throw exceptions when accessed
                    }

                    if (shouldSerialize)
                    {
                        var propName = GetPropertyName(prop, options);
                        writer.WritePropertyName(propName);
                        JsonSerializer.Serialize(writer, propValue, options);
                    }
                }

                writer.WriteEndObject();
            }

            private string GetPropertyName(PropertyInfo prop, JsonSerializerOptions options)
            {
                // Check for JsonPropertyNameAttribute
                var nameAttribute = prop.GetCustomAttribute<JsonPropertyNameAttribute>();
                if (nameAttribute != null)
                {
                    return nameAttribute.Name;
                }

                // Apply naming policy if one exists
                if (options.PropertyNamingPolicy != null)
                {
                    return options.PropertyNamingPolicy.ConvertName(prop.Name);
                }

                return prop.Name;
            }
        }
    }
}
