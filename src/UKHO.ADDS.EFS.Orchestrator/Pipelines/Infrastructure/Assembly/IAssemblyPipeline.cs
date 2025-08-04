namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Assembly
{
    /// <summary>
    ///     Contract for assembly pipelines that orchestrate exchange set generation workflows.
    /// </summary>
    public interface IAssemblyPipeline
    {
        /// <summary>
        ///     Executes the assembly pipeline workflow asynchronously.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token to cancel the pipeline execution.</param>
        /// <returns>Response containing job status, build state, and execution results.</returns>
        Task<AssemblyPipelineResponse> RunAsync(CancellationToken cancellationToken);
    }
}
