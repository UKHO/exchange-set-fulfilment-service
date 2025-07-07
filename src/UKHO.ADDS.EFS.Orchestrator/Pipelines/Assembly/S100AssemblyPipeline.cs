using UKHO.ADDS.EFS.Jobs.S100;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Assembly.Common;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Assembly.S100;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Assembly
{
    internal class S100AssemblyPipeline : AssemblyPipeline
    {
        public S100AssemblyPipeline(AssemblyPipelineParameters parameters, AssemblyPipelineNodeFactory nodeFactory, ILogger<S100AssemblyPipeline> logger)
            : base(parameters, nodeFactory, logger)
        {
        }

        public override async Task<AssemblyPipelineResponse> RunAsync(CancellationToken cancellationToken)
        {
            var job = CreateJob<S100ExchangeSetJob>();

            var pipeline = new PipelineNode<S100ExchangeSetJob>();

            pipeline.AddChild(NodeFactory.CreateNode<GetExistingTimestampNode>(cancellationToken));
            pipeline.AddChild(NodeFactory.CreateNode<GetS100ProductsFromExistingTimestampNode>(cancellationToken));
            pipeline.AddChild(NodeFactory.CreateNode<CreateFileShareBatchNode>(cancellationToken));
            pipeline.AddChild(NodeFactory.CreateNode<PersistS100JobNode>(cancellationToken));
            pipeline.AddChild(NodeFactory.CreateNode<SetJobTypeNode>(cancellationToken));
            pipeline.AddChild(NodeFactory.CreateNode<RequestS100BuildNode>(cancellationToken));

            var result = await pipeline.ExecuteAsync(job);

            return new AssemblyPipelineResponse { JobId = Parameters.JobId, Status = result.Status, DataStandard = Parameters.DataStandard, BatchId = job.BatchId };
        }
    }
}
