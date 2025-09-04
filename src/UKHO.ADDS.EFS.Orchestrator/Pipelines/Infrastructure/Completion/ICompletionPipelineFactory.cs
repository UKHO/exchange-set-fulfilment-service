namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Completion
{
    internal interface ICompletionPipelineFactory
    {
        CompletionPipeline CreateCompletionPipeline(CompletionPipelineParameters parameters);
    }
}
