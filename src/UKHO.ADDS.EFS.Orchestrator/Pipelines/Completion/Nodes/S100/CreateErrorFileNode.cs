using System.Text;
using UKHO.ADDS.EFS.Builds.S100;
using UKHO.ADDS.EFS.Configuration.Orchestrator;
using UKHO.ADDS.EFS.Constants;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Completion;
using UKHO.ADDS.EFS.Orchestrator.Services.Infrastructure;
using UKHO.ADDS.EFS.Utilities;
using UKHO.ADDS.EFS.VOS;
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
        private const string S100ErrorFileNameTemplate = "orchestrator:Errors:FileNameTemplate";
        private const string S100ErrorFileMessageTemplate = "orchestrator:Errors:Message";

        public CreateErrorFileNode(CompletionNodeEnvironment nodeEnvironment, IOrchestratorFileShareClient fileShareClient, ILogger<CreateErrorFileNode> logger)
            : base(nodeEnvironment)
        {
            _fileShareClient = fileShareClient ?? throw new ArgumentNullException(nameof(fileShareClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public override Task<bool> ShouldExecuteAsync(IExecutionContext<PipelineContext<S100Build>> context)
        {
            return Task.FromResult(context.Subject.Job.BatchId != BatchId.None && Environment.BuilderExitCode == BuilderExitCode.Failed);
        }

        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<PipelineContext<S100Build>> context)
        {
            var job = context.Subject.Job!;

            try
            {
                var errorMessage = GetErrorFileMessage(job.Id);
                await using var errorFileStream = new MemoryStream(Encoding.UTF8.GetBytes(errorMessage));

                var fileName = GetErrorFileName(job.Id);

                var addFileResult = await _fileShareClient.AddFileToBatchAsync(
                    (string)job.BatchId,
                    errorFileStream,
                    fileName,
                    ApiHeaderKeys.ContentTypeTextPlain,
                    (string)job.GetCorrelationId(),
                    Environment.CancellationToken);

                if (!addFileResult.IsSuccess(out _, out var error))
                {
                    context.Subject.IsErrorFileCreated = false;
                    _logger.LogCreateErrorFileAddFileFailed(job.GetCorrelationId(), DateTimeOffset.UtcNow, error);
                    return NodeResultStatus.Failed;
                }

                context.Subject.IsErrorFileCreated = true;
                _logger.LogCreateErrorFile(job.GetCorrelationId(), DateTimeOffset.UtcNow);
                return NodeResultStatus.Succeeded;
            }
            catch (Exception ex)
            {
                context.Subject.IsErrorFileCreated = false;
                _logger.LogCreateErrorFileNodeFailed(job.GetCorrelationId(), DateTimeOffset.UtcNow, ex);
                return NodeResultStatus.Failed;
            }
        }

        private string GetErrorFileName(JobId jobId)
        {
            return new FileNameGenerator(Environment.Configuration[S100ErrorFileNameTemplate]!).GenerateFileName(jobId);
        }
         
        private string GetErrorFileMessage(JobId jobId)
        {
            return Environment.Configuration[S100ErrorFileMessageTemplate]!.Replace("[jobid]", (string)jobId);
        }
    }
}
