using UKHO.ADDS.EFS.Jobs.S63;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Assembly.Common;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Assembly.S63;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Assembly
{
    internal class S63AssemblyPipeline : AssemblyPipeline
    {
        public S63AssemblyPipeline(AssemblyPipelineParameters parameters, AssemblyPipelineNodeFactory nodeFactory, ILogger<S63AssemblyPipeline> logger)
            : base(parameters, nodeFactory, logger)
        {
        }

        public override async Task<AssemblyPipelineResponse> RunAsync(CancellationToken cancellationToken)
        {
            var job = CreateJob<S63ExchangeSetJob>();

            var pipeline = new PipelineNode<S63ExchangeSetJob>();

            pipeline.AddChild(NodeFactory.CreateNode<GetExistingTimestampNode>(cancellationToken));
            pipeline.AddChild(NodeFactory.CreateNode<GetS63ProductsFromExistingTimestampNode>(cancellationToken));
            pipeline.AddChild(NodeFactory.CreateNode<CreateFileShareBatchNode>(cancellationToken));
            pipeline.AddChild(NodeFactory.CreateNode<PersistS63JobNode>(cancellationToken));
            pipeline.AddChild(NodeFactory.CreateNode<SetJobTypeNode>(cancellationToken));
            pipeline.AddChild(NodeFactory.CreateNode<RequestS63BuildNode>(cancellationToken));

            var result = await pipeline.ExecuteAsync(job);

            return new AssemblyPipelineResponse { JobId = Parameters.JobId, Status = result.Status, DataStandard = Parameters.DataStandard, BatchId = job.BatchId };
        }
    }
}
