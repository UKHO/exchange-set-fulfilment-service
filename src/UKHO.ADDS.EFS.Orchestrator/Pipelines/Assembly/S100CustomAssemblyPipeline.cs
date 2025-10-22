using UKHO.ADDS.EFS.Domain.Builds.S100;
using UKHO.ADDS.EFS.Domain.Products;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Assembly.Nodes.S100;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Factories;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Assembly;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Assembly
{
    internal class S100CustomAssemblyPipeline : AssemblyPipeline<S100Build>
    {
        private readonly IExchangeSetResponseFactory _exchangeSetResponseFactory;

        public S100CustomAssemblyPipeline(
            AssemblyPipelineParameters parameters,
            IAssemblyPipelineNodeFactory nodeFactory,
            IPipelineContextFactory<S100Build> contextFactory,
            ILogger<S100CustomAssemblyPipeline> logger,
            IExchangeSetResponseFactory exchangeSetResponseFactory)
            : base(parameters, nodeFactory, contextFactory, logger)
        {
            _exchangeSetResponseFactory = exchangeSetResponseFactory;
        }

        public override async Task<AssemblyPipelineResponse> RunAsync(CancellationToken cancellationToken)
        {
            var context = await CreateContext();

            AddPipelineNode<CreateJobNode>(cancellationToken);
            AddPipelineNode<GetDataStandardTimestampNode>(cancellationToken);
            AddPipelineNode<GetS100ProductNamesNode>(cancellationToken);
            AddPipelineNode<GetS100ProductUpdatesSinceNode>(cancellationToken);
            AddPipelineNode<GetS100ProductVersionsNode>(cancellationToken);
            AddPipelineNode<CheckExchangeSetSizeExceededNode>(cancellationToken);

            AddPipelineNode<CheckFingerprintNode>(cancellationToken);
            AddPipelineNode<CreateFileShareBatchNode>(cancellationToken);
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
                Response = _exchangeSetResponseFactory.CreateResponse(context.Job),
                ExternalApiServiceName = context.ExternalServiceError.ServiceName,
                ExternalApiResponseCode = context.ExternalServiceError.ServiceName != ServiceNameType.NotDefined ? context.ExternalServiceError.ErrorResponseCode : System.Net.HttpStatusCode.OK,
                ProductsLastModified = context.Job.ProductsLastModified
            };
        }

        protected override async Task<PipelineContext<S100Build>> CreateContext()
        {
            return await ContextFactory.CreatePipelineContext(Parameters);
        }
    }
}
