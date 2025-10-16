using Microsoft.Azure.Amqp.Sasl;
using OpenTelemetry;
using UKHO.ADDS.EFS.Domain.Builds.S100;
using UKHO.ADDS.EFS.Domain.Files;
using UKHO.ADDS.EFS.Domain.Jobs;
using UKHO.ADDS.EFS.Domain.Products;
using UKHO.ADDS.EFS.Domain.Services;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Assembly;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;
using UKHO.ADDS.Clients.Common.Constants;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Assembly.Nodes.S100
{
    internal class CreateFileShareBatchNode : AssemblyPipelineNode<S100Build>
    {
        private readonly IFileService _fileService;

        public CreateFileShareBatchNode(AssemblyNodeEnvironment nodeEnvironment, IFileService fileService)
            : base(nodeEnvironment)
        {
            _fileService = fileService;
        }

        public override Task<bool> ShouldExecuteAsync(IExecutionContext<PipelineContext<S100Build>> context)
        {
            return Task.FromResult(context.Subject.Job.JobState == JobState.Created);
        }

        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<PipelineContext<S100Build>> context)
        {
            var job = context.Subject.Job;
            var build = context.Subject.Build;
            var fssResponse = context.Subject.ResponseInfo;
            try
            {
                var batch = await _fileService.CreateBatchAsync(job.GetCorrelationId(), job.ExchangeSetType, Environment.CancellationToken);
                job.BatchId = batch.BatchId;
                job.ExchangeSetUrlExpiryDateTime = batch.BatchExpiryDateTime;
                build.BatchId = batch.BatchId;
                if (batch.ResponseCode != System.Net.HttpStatusCode.OK)
                {
                    fssResponse.ResponseCode = batch.ResponseCode;
                    fssResponse.ServiceName = ServiceNameType.FSS;
                    return NodeResultStatus.Failed;
                }
                return NodeResultStatus.Succeeded;
            }
            catch (Exception)
            {
                await context.Subject.SignalAssemblyError();
                return NodeResultStatus.Failed;
            }
        }
    }
}
