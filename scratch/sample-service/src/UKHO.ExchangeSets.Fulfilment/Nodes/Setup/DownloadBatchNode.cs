﻿using UKHO.Infrastructure.Pipelines;
using UKHO.Infrastructure.Pipelines.Nodes;

namespace UKHO.ExchangeSets.Fulfilment.Nodes.Setup
{
    internal class DownloadBatchNode : Node<ExchangeSetBuilderContext>
    {
        public override Task<bool> ShouldExecuteAsync(IExecutionContext<ExchangeSetBuilderContext> context)
        {
            return Task.FromResult(true);
        }

        protected override Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<ExchangeSetBuilderContext> context)
        {
            return Task.FromResult(NodeResultStatus.Succeeded);
        }
    }
}
