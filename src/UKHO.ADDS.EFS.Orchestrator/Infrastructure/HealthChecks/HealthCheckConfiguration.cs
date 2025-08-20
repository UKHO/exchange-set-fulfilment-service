using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace UKHO.ADDS.EFS.Orchestrator.Infrastructure.HealthChecks
{
    /// <summary>
    /// Provides helper methods for configuring health check endpoints
    /// </summary>
    public static class HealthCheckConfiguration
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

        /// <summary>
        /// Creates health check options that include console output of health check results
        /// </summary>
        /// <param name="tagsFilter">Optional filter to only include checks with specific tags</param>
        /// <param name="excludeServices">Array of service names to exclude (case-insensitive)</param>
        /// <returns>Configured HealthCheckOptions with console output</returns>
        public static HealthCheckOptions CreateHealthCheckOptionsWithConsoleOutput(string[]? tagsFilter = null, params string[] excludeServices)
        {
            var options = CreateHealthCheckOptions(tagsFilter, excludeServices);
            
            // Add a ResponseWriter that outputs health check results to the console
            options.ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json";
                
                // Log the health check results to the console
                Console.WriteLine("Health Check Results:");
                Console.WriteLine($"Overall Status: {report.Status}");
                
                foreach (var entry in report.Entries)
                {
                    Console.WriteLine($"- {entry.Key}: {entry.Value.Status}");
                    if (entry.Value.Description != null)
                    {
                        Console.WriteLine($"  Description: {entry.Value.Description}");
                    }
                    
                    if (entry.Value.Exception != null)
                    {
                        Console.WriteLine($"  Error: {entry.Value.Exception.Message}");
                    }
                    
                    if (entry.Value.Data.Count > 0)
                    {
                        Console.WriteLine("  Data:");
                        foreach (var item in entry.Value.Data)
                        {
                            Console.WriteLine($"    {item.Key}: {item.Value}");
                        }
                    }
                }
                
                // Create a JSON response with health check details
                var response = new
                {
                    status = report.Status.ToString(),
                    results = report.Entries.ToDictionary(
                        entry => entry.Key,
                        entry => new
                        {
                            status = entry.Value.Status.ToString(),
                            description = entry.Value.Description,
                            duration = entry.Value.Duration.ToString()
                        })
                };
                
                // Serialize the response to JSON and write it to the HTTP response
                var json = JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true });
                await context.Response.WriteAsync(json);
            };
            
            return options;
        }
    }
}
