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
        /// <param name="excludeServices">Set of service names to exclude (case-insensitive)</param>
        /// <returns>Configured HealthCheckOptions</returns>
        public static HealthCheckOptions CreateHealthCheckOptions(ISet<string>? tagsFilter = null, ISet<string>? excludeServices = null)
        {
            // Create case-insensitive HashSet for excludeServices if provided
            var excludeServicesSet = excludeServices != null
                ? new HashSet<string>(excludeServices, StringComparer.OrdinalIgnoreCase)
                : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            return new HealthCheckOptions
            {
                Predicate = healthCheck =>
                {
                    // First check if the health check name is in the excluded services set
                    if (excludeServicesSet.Any(service => service.Equals(healthCheck.Name, StringComparison.OrdinalIgnoreCase)))
                    {                        
                        return false;
                    }
                    // If we have a tags filter, check that at least one tag matches
                    if (tagsFilter != null && tagsFilter.Count > 0)
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
