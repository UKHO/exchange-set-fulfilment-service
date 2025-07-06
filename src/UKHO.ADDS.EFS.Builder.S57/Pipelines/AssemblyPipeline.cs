using UKHO.ADDS.Clients.FileShareService.ReadOnly;
using UKHO.ADDS.EFS.Builder.S57.Pipelines.Assemble;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Builder.S57.Pipelines
{
    internal class AssemblyPipeline : IBuilderPipeline<S57ExchangeSetPipelineContext>
    {
        private readonly IFileShareReadOnlyClient _fileShareReadOnlyClient;

        public AssemblyPipeline(IFileShareReadOnlyClient fileShareReadOnlyClient) => _fileShareReadOnlyClient = fileShareReadOnlyClient ?? throw new ArgumentNullException(nameof(fileShareReadOnlyClient));

        public async Task<NodeResult> ExecutePipeline(S57ExchangeSetPipelineContext context)
        {
            var pipeline = new PipelineNode<S57ExchangeSetPipelineContext>();
            pipeline.AddChild(new TestAssembleNode());

            var result = await pipeline.ExecuteAsync(context);

            return result;
        }
    }
}
