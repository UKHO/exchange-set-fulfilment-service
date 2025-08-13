using System.Text;
using System.Text.Json;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using System.Linq; // Ensure this is included

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.Hosting
{
    public static class HostApplicationBuilderExtensions
    {
        public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
        {
            builder.ConfigureOpenTelemetry();

            builder.AddDefaultHealthChecks();

            builder.Services.AddServiceDiscovery();

            builder.Services.ConfigureHttpClientDefaults(http =>
            {
                http.AddStandardResilienceHandler();
                http.AddServiceDiscovery();
            });

            return builder;
        }

        public static TBuilder ConfigureOpenTelemetry<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
        {
            builder.Logging.AddOpenTelemetry(logging =>
            {
                logging.IncludeFormattedMessage = true;
                logging.IncludeScopes = true;
            });

            builder.Services.AddOpenTelemetry()
                .WithMetrics(metrics =>
                {
                    metrics.AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddRuntimeInstrumentation();
                })
                .WithTracing(tracing =>
                {
                    tracing.AddSource(builder.Environment.ApplicationName)
                        .AddAspNetCoreInstrumentation()
                        //.AddGrpcClientInstrumentation()
                        .AddHttpClientInstrumentation();
                });

            builder.AddOpenTelemetryExporters();

            return builder;
        }

        private static TBuilder AddOpenTelemetryExporters<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
        {
            var useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

            if (useOtlpExporter)
            {
                builder.Services.AddOpenTelemetry().UseOtlpExporter();
            }

            if (!string.IsNullOrEmpty(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
            {
                builder.Services.AddOpenTelemetry().UseAzureMonitor();
            }

            return builder;
        }

        public static TBuilder AddDefaultHealthChecks<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
        {
            builder.Services.AddHealthChecks()
                .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);
            return builder;
        }

        public static WebApplication MapDefaultEndpoints(this WebApplication app)
        {
            app.MapHealthChecks("/health", new HealthCheckOptions
            {
                ResponseWriter = WriteHealthCheckResponse
            });

            app.MapHealthChecks("/alive", new HealthCheckOptions
            {
                Predicate = r => r.Tags.Contains("live"),
                ResponseWriter = WriteHealthCheckResponse
            });

            return app;
        }

        // This method now skips "StackExchange.Redis" and "sales-catalogue-service" from affecting overall health status
        private static Task WriteHealthCheckResponse(HttpContext context, HealthReport report)
        {
            context.Response.ContentType = "application/json; charset=utf-8";
            var options = new JsonWriterOptions { Indented = true };

            // Exclude these checks from the health status and results:
            var exclude = new[] { "StackExchange.Redis", "sales-catalogue-service" };

            // Only include checks that are NOT in the exclude list:
            var filteredEntries = report.Entries
                .Where(e => !exclude.Contains(e.Key))
                .ToDictionary(e => e.Key, e => e.Value);

            // Determine filtered overall status
            var filteredStatus = filteredEntries.All(e => e.Value.Status == HealthStatus.Healthy)
                ? HealthStatus.Healthy
                : HealthStatus.Unhealthy;

            using var memoryStream = new MemoryStream();
            using (var jsonWriter = new Utf8JsonWriter(memoryStream, options))
            {
                jsonWriter.WriteStartObject();
                jsonWriter.WriteString("status", filteredStatus.ToString());
                jsonWriter.WriteNumber("totalDuration", report.TotalDuration.TotalMilliseconds);

                jsonWriter.WriteStartObject("results");
                foreach (var healthCheck in filteredEntries)
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
                jsonWriter.WriteEndObject(); // end results
                jsonWriter.WriteEndObject(); // end root
            }

            return context.Response.WriteAsync(
                Encoding.UTF8.GetString(memoryStream.ToArray()));
        }
    }
}
