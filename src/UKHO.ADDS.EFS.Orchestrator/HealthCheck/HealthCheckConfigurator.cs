using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;

namespace UKHO.ADDS.EFS.Orchestrator.HealthCheck
{
    internal static class HealthCheckConfigurator
    {
        public static void Configure(WebApplicationBuilder builder)
        {
            // Add the base health check
            builder.Services.AddHealthChecks()
                .AddCheck("self", () => HealthCheckResult.Healthy(), new[] { "live" });

            // Add HTTP clients for health checks
            builder.Services.AddHttpClient("FileShareHealthCheck");
            builder.Services.AddHttpClient("SalesCatalogueHealthCheck");

            // Register FileShareService health check
            builder.Services.AddHealthChecks()
                .AddCheck<FileShareServiceHealthCheck>(
                    "FileShareService",
                    HealthStatus.Unhealthy,
                    timeout: TimeSpan.FromSeconds(10),
                    tags: new[] { "readiness" });

            // Register SalesCatalogueService health check
            builder.Services.AddHealthChecks()
                .AddCheck<SalesCatalogueServiceHealthCheck>(
                    "SalesCatalogueService",
                    HealthStatus.Unhealthy,
                    timeout: TimeSpan.FromSeconds(10),
                    tags: new[] { "readiness" });
        }
    }
}
