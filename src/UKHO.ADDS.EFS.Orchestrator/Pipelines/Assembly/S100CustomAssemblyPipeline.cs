using UKHO.ADDS.EFS.Domain.Builds.S100;
using UKHO.ADDS.EFS.Domain.Files;
using UKHO.ADDS.EFS.Orchestrator.Api.Messages;
using UKHO.ADDS.EFS.Orchestrator.Api.Models;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Assembly.Nodes.S100;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Assembly;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Assembly
{
    internal class S100CustomAssemblyPipeline : AssemblyPipeline<S100Build>
    {
        public S100CustomAssemblyPipeline(AssemblyPipelineParameters parameters, IAssemblyPipelineNodeFactory nodeFactory, IPipelineContextFactory<S100Build> contextFactory, ILogger<S100CustomAssemblyPipeline> logger)
            : base(parameters, nodeFactory, contextFactory, logger)
        {
        }

        public override async Task<AssemblyPipelineResponse> RunAsync(CancellationToken cancellationToken)
        {
            var context = await CreateContext();

            AddPipelineNode<CreateJobNode>(cancellationToken);
            AddPipelineNode<GetDataStandardTimestampNode>(cancellationToken);
            AddPipelineNode<ProductEditionRetrievalNode>(cancellationToken);

            AddPipelineNode<CheckFingerprintNode>(cancellationToken);
            AddPipelineNode<CreateFileShareBatchNode>(cancellationToken);
            AddPipelineNode<CreateResponseNode>(cancellationToken);
            AddPipelineNode<ScheduleBuildNode>(cancellationToken);
            AddPipelineNode<CreateFingerprintNode>(cancellationToken);

            var result = await Pipeline.ExecuteAsync(context);

            return new AssemblyPipelineResponse()
            {
                JobId = context.Job.Id,
                DataStandard = context.Job.DataStandard,
                JobStatus = context.Job.JobState,
                BuildStatus = context.Job.BuildState,
                BatchId = context.Job.BatchId,
                ErrorResponse = context.ErrorResponse?.Errors?.Count > 0 ? context.ErrorResponse : null,
                Response = CreateResponseFromContext(context) // Changed 'Response' to 'ResponseData'
            };
        }

        protected override async Task<PipelineContext<S100Build>> CreateContext()
        {
            return await ContextFactory.CreatePipelineContext(Parameters);
        }

        private static CustomExchangeSetResponse CreateResponseFromContext(
            PipelineContext<S100Build> context)
        {

            // Dummy links
            var links = new ExchangeSetLinks
            {
                ExchangeSetBatchStatusUri = new Link { Uri = new Uri($"http://fss.ukho.gov.uk/batch/{context.Job.BatchId}/status") },
                ExchangeSetBatchDetailsUri = new Link { Uri = new Uri($"http://fss.ukho.gov.uk/batch/{context.Job.BatchId}") },
                ExchangeSetFileUri = new Link { Uri = new Uri($"http://fss.ukho.gov.uk/batch/{context.Job.BatchId}/files/exchangeset.zip") }
            };

            // Dummy missing products list
            var missingProducts = context.Job.RequestedProductsNotInExchangeSet;

            return new CustomExchangeSetResponse
            {
                Links = links,
                ExchangeSetUrlExpiryDateTime = context.Job.ExchangeSetUrlExpiryDateTime,
                RequestedProductCount = context.Job.RequestedProductCount,
                ExchangeSetProductCount = context.Job.ExchangeSetProductCount,
                RequestedProductsAlreadyUpToDateCount = context.Job.RequestedProductsAlreadyUpToDateCount,
                RequestedProductsNotInExchangeSet = missingProducts,
                FssBatchId = context.Job.BatchId
            };
        }
    }
}
