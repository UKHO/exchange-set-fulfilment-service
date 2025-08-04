namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Assembly
{
    /// <summary>
    ///     Factory for creating assembly pipelines based on data standard parameters.
    /// </summary>
    public interface IAssemblyPipelineFactory
    {
        /// <summary>
        ///     Creates an assembly pipeline for the specified parameters and data standard.
        /// </summary>
        /// <param name="parameters">Configuration parameters containing data standard, job details, and pipeline settings.</param>
        /// <returns>Assembly pipeline instance configured for the specified data standard.</returns>
        IAssemblyPipeline CreateAssemblyPipeline(AssemblyPipelineParameters parameters);
    }
}
