using UKHO.ADDS.EFS.Builder.S100.IIC;
using UKHO.ADDS.EFS.Builder.S100.Pipelines.Create;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Builder.S100.Pipelines
{
    internal class CreationPipeline : IBuilderPipeline<ExchangeSetPipelineContext>
    {
        private readonly IToolClient _toolClient;

        public CreationPipeline(IToolClient toolClient)
        {
            _toolClient = toolClient ?? throw new ArgumentNullException(nameof(toolClient));
        }

        public async Task<NodeResult> ExecutePipeline(ExchangeSetPipelineContext context)
        {
            var pipeline = new PipelineNode<ExchangeSetPipelineContext>();

            pipeline.AddChild(new AddExchangeSetNode(_toolClient));
            pipeline.AddChild(new AddContentExchangeSetNode(_toolClient));
            pipeline.AddChild(new SignExchangeSetNode(_toolClient));

            var result = await pipeline.ExecuteAsync(context);

            return result;
        }
    }
}
