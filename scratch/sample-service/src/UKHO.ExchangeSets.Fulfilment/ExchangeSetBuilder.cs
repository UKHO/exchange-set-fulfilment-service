using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using UKHO.ExchangeSets.Fulfilment.Nodes.Builder;
using UKHO.ExchangeSets.Fulfilment.Nodes.Distributor;
using UKHO.ExchangeSets.Fulfilment.Nodes.Setup;
using UKHO.Infrastructure.Pipelines.Contexts;
using UKHO.Infrastructure.Pipelines.Nodes;

namespace UKHO.ExchangeSets.Fulfilment
{
    internal class ExchangeSetBuilder : IIExchangeSetBuilder
    {
        private readonly PipelineNode<ExchangeSetBuilderContext> _builderPipeline;
        private readonly PipelineNode<ExchangeSetBuilderContext> _distributorPipeline;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger _logger;

        private readonly PipelineNode<ExchangeSetBuilderContext> _setupPipeline;

        public ExchangeSetBuilder(IServiceProvider serviceProvider, ILogger<ExchangeSetBuilder> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;

            var executionOptions = new ExecutionOptions { ContinueOnFailure = false, ThrowOnError = false };

            _setupPipeline = new PipelineNode<ExchangeSetBuilderContext> { LocalOptions = executionOptions };

            _setupPipeline.AddChild(GetNode<GetProductsNode>());
            _setupPipeline.AddChild(GetNode<GetCatalogNode>());
            _setupPipeline.AddChild(GetNode<DownloadBatchNode>());

            _builderPipeline = new PipelineNode<ExchangeSetBuilderContext> { LocalOptions = executionOptions };

            _builderPipeline.AddChild(GetNode<CreateExchangeSetContainerNode>());
            _builderPipeline.AddChild(GetNode<AddExchangeSetContentNode>());
            _builderPipeline.AddChild(GetNode<SignExchangeSetNode>());
            _builderPipeline.AddChild(GetNode<DownloadExchangeSetNode>());

            _distributorPipeline = new PipelineNode<ExchangeSetBuilderContext> { LocalOptions = executionOptions };

            _distributorPipeline.AddChild(GetNode<FileShareUploadNode>());
            _distributorPipeline.AddChild(GetNode<FileShareCommitNode>());
        }

        public async Task<ExchangeSetBuilderResult> BuildExchangeSet()
        {
            var context = new ExchangeSetBuilderContext();

            var setupResult = await _setupPipeline.ExecuteAsync(context);

            if (setupResult.Status != NodeResultStatus.Succeeded)
            {
                LogPipelineError(setupResult);
                return ExchangeSetBuilderResult.SetupPipelineFailed;
            }

            var builderPipeline = await _builderPipeline.ExecuteAsync(context);

            if (builderPipeline.Status != NodeResultStatus.Succeeded)
            {
                LogPipelineError(builderPipeline);
                return ExchangeSetBuilderResult.BuilderPipelineFailed;
            }

            var distributorResult = await _distributorPipeline.ExecuteAsync(context);

            if (distributorResult.Status != NodeResultStatus.Succeeded)
            {
                LogPipelineError(distributorResult);
                return ExchangeSetBuilderResult.DistributorPipelineFailed;
            }

            return ExchangeSetBuilderResult.Succeeded;
        }

        private void LogPipelineError(NodeResult result)
        {
            if (result.Exception != null)
            {
                _logger.LogError(result.Exception, "Pipeline failed with exception");
            }
            else
            {
                _logger.LogError($"Pipeline failed");
            }
        }

        public T GetNode<T>() where T : Node<ExchangeSetBuilderContext> => _serviceProvider.GetRequiredService<T>();
    }
}
