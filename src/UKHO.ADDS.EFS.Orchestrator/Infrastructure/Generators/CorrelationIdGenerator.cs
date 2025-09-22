using UKHO.ADDS.Aspire.Configuration;
using UKHO.ADDS.EFS.Domain.External;

namespace UKHO.ADDS.EFS.Orchestrator.Infrastructure.Generators
{
    /// <summary>
    /// Provides methods to generate correlation IDs for jobs and scheduled tasks,
    /// using environment-based prefixes for easier identification in logs.
    /// </summary>
    internal class CorrelationIdGenerator:ICorrelationIdGenerator
    {
        private const string JobPrefix = "job-";
        private const string SchedPrefix = "sched-";

        /// <summary>
        /// Creates a correlation ID for a job, applying a prefix in local or development environments for easier log identification.
        /// </summary>
        /// <returns>A new CorrelationId value object.</returns>
        public CorrelationId CreateForJob()
        {
            return CreateCorrelationId(JobPrefix);
        }

        /// <summary>
        /// Generates a correlation ID for a scheduler, always including a prefix for consistent identification across all environments.
        /// </summary>
        /// <returns>A new CorrelationId value object.</returns>
        public CorrelationId CreateForScheduler()
        {
            return CreateCorrelationId(SchedPrefix);
        }

        /// <summary>
        /// Generates a correlation ID using the specified prefix, applying environment-specific logic to include the prefix in local or development environments for easier identification.
        /// </summary>
        /// <param name="prefix">The prefix to use for the correlation ID.</param>
        /// <returns>A new CorrelationId value object.</returns>
        private static CorrelationId CreateCorrelationId(string prefix)
        {
            var env = AddsEnvironment.GetEnvironment();
            var isLocalOrDev = env.IsLocal() || env.IsDev();
            var guid = Guid.NewGuid();

            var correlationId = isLocalOrDev ? $"{prefix}{guid:N}" : guid.ToString();

            return CorrelationId.From(correlationId);
        }
    }
}
