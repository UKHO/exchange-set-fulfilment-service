using System.Text;
using UKHO.ADDS.Configuration.Schema;
using UKHO.ADDS.EFS.Builds.S100;
using UKHO.ADDS.EFS.Configuration.Orchestrator;
using UKHO.ADDS.EFS.Constants;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Completion;
using UKHO.ADDS.EFS.Orchestrator.Services.Infrastructure;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Completion.Nodes.S100
{
    /// <summary>
    /// Node responsible for creating an error.txt file when the builder process fails.
    /// </summary>
    internal class CreateErrorFileNode : CompletionPipelineNode<S100Build>
    {
        private readonly IOrchestratorFileShareClient _fileShareClient;
        private readonly ILogger<CreateErrorFileNode> _logger;

        public CreateErrorFileNode(CompletionNodeEnvironment nodeEnvironment, IOrchestratorFileShareClient fileShareClient, ILogger<CreateErrorFileNode> logger)
            : base(nodeEnvironment)
        {
            _fileShareClient = fileShareClient ?? throw new ArgumentNullException(nameof(fileShareClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public override Task<bool> ShouldExecuteAsync(IExecutionContext<PipelineContext<S100Build>> context)
        {
            return Task.FromResult(!string.IsNullOrWhiteSpace(context.Subject.Job.BatchId) && Environment.BuilderExitCode == BuilderExitCode.Failed);
        }

        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<PipelineContext<S100Build>> context)
        {
            var job = context.Subject.Job!;
            var correlationId = job.GetCorrelationId();

            try
            {
                var errorMessage = $"There has been a problem in creating your exchange set, so we are unable to fulfil your request at this time. Please contact UKHO Customer Services quoting correlation ID {correlationId}";
                using var errorFileStream = new MemoryStream(Encoding.UTF8.GetBytes(errorMessage));

                var environmentName = Environment.Configuration[WellKnownConfigurationName.AddsEnvironmentName];
                var fileName = string.Equals(environmentName, "local", StringComparison.OrdinalIgnoreCase)
                    ? $"error_{job.Id}.txt" : "error.txt";

                var addFileResult = await _fileShareClient.AddFileToBatchAsync(
                    job.BatchId!,
                    errorFileStream,
                    fileName,
                    ApiHeaderKeys.ContentTypeTextPlain,
                    correlationId,
                    Environment.CancellationToken);

                if (!addFileResult.IsSuccess(out _, out var error))
                {
                    _logger.LogCreateErrorFileAddFileFailed(correlationId, DateTimeOffset.UtcNow, error);
                    return NodeResultStatus.Failed;
                }

                _logger.LogCreateErrorFileSuccess(correlationId, DateTimeOffset.UtcNow);
                return NodeResultStatus.Succeeded;
            }
            catch (Exception ex)
            {
                _logger.LogCreateErrorFileNodeFailed(correlationId, DateTimeOffset.UtcNow, ex);
                return NodeResultStatus.Failed;
            }
        }
    }
}
