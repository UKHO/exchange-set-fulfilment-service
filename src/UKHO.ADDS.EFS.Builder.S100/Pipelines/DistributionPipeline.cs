﻿using UKHO.ADDS.Clients.FileShareService.ReadWrite;
using UKHO.ADDS.EFS.Builder.S100.Pipelines.Distribute;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Builder.S100.Pipelines
{
    internal class DistributionPipeline : IBuilderPipeline<S100ExchangeSetPipelineContext>
    {
        private readonly IFileShareReadWriteClient _fileShareReadWriteClient;
        public DistributionPipeline(IFileShareReadWriteClient fileShareReadWriteClient)
        {
            _fileShareReadWriteClient = fileShareReadWriteClient ?? throw new ArgumentNullException(nameof(fileShareReadWriteClient));
        }

        public async Task<NodeResult> ExecutePipeline(S100ExchangeSetPipelineContext context)
        {
            var pipeline = new PipelineNode<S100ExchangeSetPipelineContext>();

            pipeline.AddChild(new ExtractExchangeSetNode());
            pipeline.AddChild(new UploadFilesNode(_fileShareReadWriteClient));

            var result = await pipeline.ExecuteAsync(context);

            return result;
        }
    }
}
