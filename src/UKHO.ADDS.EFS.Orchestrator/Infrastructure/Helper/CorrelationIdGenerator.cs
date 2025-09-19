using UKHO.ADDS.Aspire.Configuration;
using UKHO.ADDS.EFS.Domain.External;

namespace UKHO.ADDS.EFS.Orchestrator.Infrastructure.Helper
{
    /// <summary>
    /// Provides methods to generate correlation IDs for jobs and scheduled tasks,
    /// using environment-based prefixes for easier identification in logs.
    /// </summary>
    internal static class CorrelationIdGenerator
    {
        private const string JobPrefix = "job-";
        private const string SchedPrefix = "sched-";

        /// <summary>
        /// Generates a correlation ID for a job, with a prefix in non-local/non-dev environments.
        /// </summary>
        /// <returns>A new CorrelationId value object.</returns>
        public static CorrelationId CreateForJob()
        {
            return CreateCorrelationId(JobPrefix);
        }

        /// <summary>
        /// Generates a correlation ID for a scheduler, with a prefix in all environments.
        /// </summary>
        /// <returns>A new CorrelationId value object.</returns>
        public static CorrelationId CreateForScheduler()
        {
            return CreateCorrelationId(SchedPrefix);
        }

        /// <summary>
        /// Generates a correlation ID with the specified prefix and environment logic.
        /// </summary>
        /// <param name="prefix">The prefix to use for the correlation ID.</param>
        /// <param name="usePrefixForLocal">If true, always use the prefix; otherwise, only use it for non-local/non-dev.</param>
        /// <returns>A new CorrelationId value object.</returns>
        private static CorrelationId CreateCorrelationId(string prefix)
        {
            var env = AddsEnvironment.GetEnvironment();
            var isLocalOrDev = env.IsLocal() || env.IsDev();
            var guid = Guid.NewGuid();

            string value;
            if ((isLocalOrDev))
            {
                value = $"{prefix}{guid:N}";
            }
            else
            {
                value = guid.ToString();
            }

            return CorrelationId.From(value);
        }
    }
}
