using System.IO.Compression;
using System.Text;
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
    /// Node responsible for creating an error.txt file inside a V01X01_[jobid].zip when the builder process fails.
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
            // Execute only when there's a batch ID and the builder process failed
            return Task.FromResult(!string.IsNullOrEmpty(context.Subject.Job.BatchId) && Environment.BuilderExitCode == BuilderExitCode.Failed);
        }

        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<PipelineContext<S100Build>> context)
        {
            var job = context.Subject.Job!;
            var correlationId = job.GetCorrelationId();

            try
            {
                // Create error.txt content with correlation ID
                var errorMessage = $"There has been a problem in creating your exchange set, so we are unable to fulfil your request at this time. Please contact UKHO Customer Services quoting correlation ID {correlationId}";
                var errorFileContent = Encoding.UTF8.GetBytes(errorMessage);

                // Create zip file containing error.txt
                using var zipStream = new MemoryStream();
                using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, true))
                {
                    var errorEntry = archive.CreateEntry("error.txt", CompressionLevel.Optimal);
                    using var entryStream = errorEntry.Open();
                    await entryStream.WriteAsync(errorFileContent, Environment.CancellationToken);
                }

                zipStream.Position = 0; // Reset position for reading

                // Generate the zip file name in V01X01_[jobid].zip format
                var zipFileName = $"V01X01_{job.Id}.zip";

                // Upload the zip file to the batch
                var addFileResult = await _fileShareClient.AddFileToBatchAsync(
                    job.BatchId!,
                    zipStream,
                    zipFileName,
                    ApiHeaderKeys.ContentTypeOctetStream,
                    correlationId,
                    Environment.CancellationToken);

                if (!addFileResult.IsSuccess(out _, out var error))
                {
                    _logger.LogCreateErrorFileAddFileFailed(correlationId, job.BatchId!, error);
                    return NodeResultStatus.Failed;
                }

                _logger.LogCreateErrorFileSuccess(correlationId, job.BatchId!);
                return NodeResultStatus.Succeeded;
            }
            catch (Exception ex)
            {
                _logger.LogCreateErrorFileNodeFailed(correlationId, job.BatchId!, ex);
                return NodeResultStatus.Failed;
            }
        }
    }
}
