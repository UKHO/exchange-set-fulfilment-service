using HandlebarsDotNet;

namespace UKHO.ADDS.EFS.Builder.S100.Services
{
    internal class FileNameGenerator
    {
        private const string VersionPrefix = "V01X01";
        private const string LowerEnvironmentTemplate = VersionPrefix + "_{{JobId}}.zip";
        private const string HigherEnvironmentTemplate = $"{VersionPrefix}.zip";
        private static readonly string[] _lowerEnvironments = ["Local", "Development", "Dev"];
        private static readonly HandlebarsTemplate<object, object> _lowerEnvTemplate;
        private static readonly HandlebarsTemplate<object, object> _higherEnvTemplate;

        static FileNameGenerator()
        {
            _lowerEnvTemplate = Handlebars.Compile(LowerEnvironmentTemplate);
            _higherEnvTemplate = Handlebars.Compile(HigherEnvironmentTemplate);
        }

        /// <summary>
        /// Gets the exchange set file name based on the current environment.
        /// </summary>
        /// <param name="jobId">The JobId to use in the file name template.</param>
        /// <returns>The appropriate file name for the current environment using Handlebars templates.</returns>
        public static string GetExchangeSetFileName(string jobId)
        {
            // Get the current environment name from environment variables, defaulting to "Development" if not set.
            var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

            // Create an anonymous object to hold the JobId for the Handlebars template.
            var templateData = new { JobId = jobId };

            // Determine if the current environment is considered a "lower" environment (e.g., Local, Development, Dev).
            var isLowerEnvironment = _lowerEnvironments.Contains(environmentName, StringComparer.OrdinalIgnoreCase);

            // Check if the provided jobId is not null or empty.
            var hasValidJobId = !string.IsNullOrEmpty(jobId);

            // Use the lower environment template if in a lower environment and jobId is valid; otherwise, use the higher environment template.
            return (isLowerEnvironment && hasValidJobId) ? _lowerEnvTemplate(templateData) : _higherEnvTemplate(templateData);
        }
    }
}
