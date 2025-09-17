using UKHO.ADDS.Aspire.Configuration.Remote;
using UKHO.ADDS.EFS.Domain.Builds.S100;
using UKHO.ADDS.EFS.Domain.Files;
using UKHO.ADDS.EFS.Domain.Services;
using UKHO.ADDS.EFS.Infrastructure.Configuration.Namespaces;
using UKHO.ADDS.EFS.Orchestrator.Api.Messages;
using UKHO.ADDS.EFS.Orchestrator.Api.Models;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Assembly.Nodes.S100;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Assembly;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Assembly
{
    internal class S100CustomAssemblyPipeline : AssemblyPipeline<S100Build>
    {
        private readonly IExternalServiceRegistry _externalServiceRegistry;
        private readonly IConfiguration _configuration;
        private readonly IFileNameGeneratorService _fileNameGeneratorService;

        public S100CustomAssemblyPipeline(
            AssemblyPipelineParameters parameters, 
            IAssemblyPipelineNodeFactory nodeFactory, 
            IPipelineContextFactory<S100Build> contextFactory, 
            ILogger<S100CustomAssemblyPipeline> logger,
            IExternalServiceRegistry externalServiceRegistry,
            IConfiguration configuration,
            IFileNameGeneratorService fileNameGeneratorService)
            : base(parameters, nodeFactory, contextFactory, logger)
        {
            _externalServiceRegistry = externalServiceRegistry;
            _configuration = configuration;
            _fileNameGeneratorService = fileNameGeneratorService;
        }

        public override async Task<AssemblyPipelineResponse> RunAsync(CancellationToken cancellationToken)
        {
            var context = await CreateContext();

            AddPipelineNode<CreateJobNode>(cancellationToken);
            AddPipelineNode<GetDataStandardTimestampNode>(cancellationToken);
            AddPipelineNode<ProductEditionRetrievalNode>(cancellationToken);

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
                Response = CreateResponseFromContext(context)
            };
        }

        protected override async Task<PipelineContext<S100Build>> CreateContext()
        {
            return await ContextFactory.CreatePipelineContext(Parameters);
        }

        private CustomExchangeSetResponse CreateResponseFromContext(
            PipelineContext<S100Build> context)
        {
            var fssEndpoint = _externalServiceRegistry.GetServiceEndpoint(ProcessNames.FileShareService);
            var baseUri = fssEndpoint.Uri.ToString().TrimEnd('/');
            
            // Get the exchange set name template from configuration
            var exchangeSetNameTemplate = _configuration["orchestrator:Builders:S100:ExchangeSetNameTemplate"]!;
            var fileName = _fileNameGeneratorService.GenerateFileName(exchangeSetNameTemplate, context.Job.Id);

            // Create links using the endpoint from the registry
            var links = new ExchangeSetLinks
            {
                ExchangeSetBatchStatusUri = new Link { Uri = new Uri($"{baseUri}/batch/{context.Job.BatchId}/status") },
                ExchangeSetBatchDetailsUri = new Link { Uri = new Uri($"{baseUri}/batch/{context.Job.BatchId}") },
                ExchangeSetFileUri = new Link { Uri = new Uri($"{baseUri}/batch/{context.Job.BatchId}/files/{fileName}") }
            };

            return new CustomExchangeSetResponse
            {
                Links = links,
                ExchangeSetUrlExpiryDateTime = context.Job.ExchangeSetUrlExpiryDateTime,
                RequestedProductCount = context.Job.RequestedProductCount,
                ExchangeSetProductCount = context.Job.ExchangeSetProductCount,
                RequestedProductsAlreadyUpToDateCount = context.Job.RequestedProductsAlreadyUpToDateCount,
                RequestedProductsNotInExchangeSet = context.Job.RequestedProductsNotInExchangeSet,
                FssBatchId = context.Job.BatchId
            };
        }
    }
}
