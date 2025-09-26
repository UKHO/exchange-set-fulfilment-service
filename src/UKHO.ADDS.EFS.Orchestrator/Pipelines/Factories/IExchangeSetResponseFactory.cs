using UKHO.ADDS.EFS.Domain.Jobs;
using UKHO.ADDS.EFS.Orchestrator.Api.Messages;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Factories
{
    /// <summary>
    /// Factory for creating exchange set response objects
    /// </summary>
    internal interface IExchangeSetResponseFactory
    {
        /// <summary>
        /// Creates a custom exchange set response from job data
        /// </summary>
        /// <param name="job">The job containing exchange set information</param>
        /// <returns>A configured CustomExchangeSetResponse</returns>
        CustomExchangeSetResponse CreateResponse(Job job);
    }
}
