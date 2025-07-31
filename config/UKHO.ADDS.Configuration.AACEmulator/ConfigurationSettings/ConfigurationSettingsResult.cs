using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using UKHO.ADDS.Configuration.AACEmulator.Common;

namespace UKHO.ADDS.Configuration.AACEmulator.ConfigurationSettings
{
    public class ConfigurationSettingsResult(
        IEnumerable<ConfigurationSetting> settings,
        DateTimeOffset? mementoDatetime = default,
        string? select = default) :
        IResult,
        IContentTypeHttpResult,
        IStatusCodeHttpResult,
        IValueHttpResult
    {
        public string? ContentType => MediaType.ConfigurationSettings;

        public async Task ExecuteAsync(HttpContext httpContext)
        {
            if (mementoDatetime.HasValue)
            {
                httpContext.Response.Headers["Memento-Datetime"] = mementoDatetime.Value.ToString("R");
            }

            if (StatusCode.HasValue)
            {
                httpContext.Response.StatusCode = StatusCode.Value;
            }

            await httpContext.Response.WriteAsJsonAsync(
                Value,
                new JsonSerializerOptions(JsonSerializerDefaults.Web)
                {
                    TypeInfoResolver = new DefaultJsonTypeInfoResolver
                    {
                        Modifiers =
                        {
                            new SelectJsonTypeInfoModifier(select?.Split(',')).Modify
                        }
                    }
                },
                ContentType);
        }

        public int? StatusCode => StatusCodes.Status200OK;

        public object Value => new
        {
            items = settings.Select(setting => new
            {
                etag = setting.Etag,
                key = setting.Key,
                label = setting.Label,
                content_type = setting.ContentType,
                value = setting.Value,
                tags = setting.Tags ?? new Dictionary<string, string>(),
                locked = setting.Locked,
                last_modified = setting.LastModified.ToString("O")
            })
        };
    }
}
