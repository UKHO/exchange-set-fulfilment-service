using UKHO.ADDS.Clients.FileShareService.ReadOnly;
using UKHO.ADDS.EFS.Builder.S100.Pipelines.Assemble;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Builder.S100.Pipelines
{
    internal class AssemblyPipeline : IBuilderPipeline<ExchangeSetPipelineContext>
    {
        private readonly IFileShareReadOnlyClient _fileShareReadOnlyClient;
        private readonly IConfiguration _configuration;

        public AssemblyPipeline(IFileShareReadOnlyClient fileShareReadOnlyClient, IConfiguration configuration)
        {
            _fileShareReadOnlyClient = fileShareReadOnlyClient ?? throw new ArgumentNullException(nameof(fileShareReadOnlyClient));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public async Task<NodeResult> ExecutePipeline(ExchangeSetPipelineContext context)
        {
            var pipeline = new PipelineNode<ExchangeSetPipelineContext>();
            
            pipeline.AddChild(new ProductSearchNode(_fileShareReadOnlyClient));
            pipeline.AddChild(new DownloadFilesNode(_fileShareReadOnlyClient,_configuration));

            var result = await pipeline.ExecuteAsync(context);

            return result;
        }
    }
}
