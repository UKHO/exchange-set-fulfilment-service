using Microsoft.Extensions.Configuration;

namespace UKHO.ADDS.Configuration
{
    public static class ConfigurationBuilderExtensions
    {
        public static IConfigurationBuilder AddAppConfiguration(this IConfigurationBuilder builder, string name)
        {
            var serviceEnvironmentKey = $"services__{name}__http__0";
            var url = Environment.GetEnvironmentVariable(serviceEnvironmentKey)!;

            builder.AddAzureAppConfiguration(o =>
            {
                var conStr = $"Endpoint={url};Id=abcd;Secret=c2VjcmV0;";

                o.Connect(conStr);
            });

            return builder;
        }
    }
}
