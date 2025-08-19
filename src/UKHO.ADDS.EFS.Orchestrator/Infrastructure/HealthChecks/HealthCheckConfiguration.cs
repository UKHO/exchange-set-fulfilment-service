using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text;
using System.Text.Json;

namespace UKHO.ADDS.EFS.Orchestrator.Infrastructure.HealthChecks
{
    /// <summary>
    /// Provides helper methods for configuring health check endpoints
    /// </summary>
    public static class HealthCheckConfiguration
    {
        /// <summary>
        /// Writes health check results as a detailed JSON response
        /// </summary>
        public static Task WriteHealthCheckResponse(HttpContext context, HealthReport report)
        {
            context.Response.ContentType = "application/json; charset=utf-8";

            var options = new JsonWriterOptions { Indented = true };

            using var memoryStream = new MemoryStream();
            using (var jsonWriter = new Utf8JsonWriter(memoryStream, options))
            {
                jsonWriter.WriteStartObject();
                jsonWriter.WriteString("status", report.Status.ToString());
                jsonWriter.WriteNumber("totalDuration", report.TotalDuration.TotalMilliseconds);

                jsonWriter.WriteStartObject("results");

                foreach (var healthCheck in report.Entries)
                {
                    jsonWriter.WriteStartObject(healthCheck.Key);

                    jsonWriter.WriteString("status", healthCheck.Value.Status.ToString());
                    jsonWriter.WriteNumber("duration", healthCheck.Value.Duration.TotalMilliseconds);

                    if (healthCheck.Value.Exception != null)
                    {
                        jsonWriter.WriteStartObject("error");
                        jsonWriter.WriteString("message", healthCheck.Value.Exception.Message);
                        jsonWriter.WriteString("stackTrace", healthCheck.Value.Exception.StackTrace);
                        jsonWriter.WriteEndObject();
                    }

                    if (healthCheck.Value.Data.Count > 0)
                    {
                        jsonWriter.WriteStartObject("data");
                        foreach (var item in healthCheck.Value.Data)
                        {
                            jsonWriter.WritePropertyName(item.Key);
                            JsonSerializer.Serialize(jsonWriter, item.Value);
                        }
                        jsonWriter.WriteEndObject();
                    }

                    jsonWriter.WriteEndObject();
                }

                jsonWriter.WriteEndObject();
                jsonWriter.WriteEndObject();
            }

            return context.Response.WriteAsync(
                Encoding.UTF8.GetString(memoryStream.ToArray()));
        }

        /// <summary>
        /// Creates health check options that exclude specific service checks (like Redis)
        /// </summary>
        /// <param name="tagsFilter">Optional filter to only include checks with specific tags</param>
        /// <param name="excludeServices">Array of service names to exclude (case-insensitive)</param>
        /// <returns>Configured HealthCheckOptions</returns>
        public static HealthCheckOptions CreateHealthCheckOptions(string[]? tagsFilter = null, params string[] excludeServices)
        {
            return new HealthCheckOptions
            {
                Predicate = healthCheck => 
                {
                    // First check if any of the excluded services match the health check name
                    if (excludeServices.Any(service => 
                        healthCheck.Name.Contains(service, StringComparison.OrdinalIgnoreCase)))
                    {
                        return false;
                    }

                    // If we have a tags filter, check that at least one tag matches
                    if (tagsFilter != null && tagsFilter.Length > 0)
                    {
                        return tagsFilter.Any(tag => healthCheck.Tags.Contains(tag));
                    }

                    // Otherwise include the health check
                    return true;
                },
                ResponseWriter = WriteHealthCheckResponse
            };
        }
    }
}
