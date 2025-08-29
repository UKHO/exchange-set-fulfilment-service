using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace UKHO.ADDS.EFS.Orchestrator.Infrastructure.HealthChecks
{
    /// <summary>
    /// Provides helper methods for configuring health check endpoints
    /// </summary>
    internal static class HealthCheckOptionsFactory
    {
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
                }
            };
        }
    }
}
