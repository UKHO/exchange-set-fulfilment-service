using System.Text;
using UKHO.ADDS.Clients.FileShareService.ReadWrite.Models;
using UKHO.ADDS.EFS.Domain.Builds;
using UKHO.ADDS.EFS.Domain.Builds.S100;
using UKHO.ADDS.EFS.Domain.Constants;
using UKHO.ADDS.EFS.Domain.Jobs;
using UKHO.ADDS.EFS.Domain.Services;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Completion;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Completion.Nodes.S100
{
    /// <summary>
    /// Node responsible for creating an error.txt file when the builder process fails.
    /// </summary>
    internal class CreateErrorFileNode : CompletionPipelineNode<S100Build>
    {
        private readonly IFileService _fileService;
        private readonly IFileNameGeneratorService _fileNameGeneratorService;
        private readonly ILogger<CreateErrorFileNode> _logger;
        private const string S100ErrorFileNameTemplate = "orchestrator:Errors:FileNameTemplate";
        private const string S100ErrorFileMessageTemplate = "orchestrator:Errors:Message";

        public CreateErrorFileNode(CompletionNodeEnvironment nodeEnvironment, IFileService fileService, IFileNameGeneratorService fileNameGeneratorService, ILogger<CreateErrorFileNode> logger)
            : base(nodeEnvironment)
        {
            _fileService = fileService;
            _fileNameGeneratorService = fileNameGeneratorService;
            _logger = logger;
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

                try
                {
                    var batchHandle = new BatchHandle((string)job.BatchId!);
                    var attributeList = await _fileService.AddFileToBatchAsync(
                        batchHandle,
                        errorFileStream,
                        fileName,
                        ApiHeaderKeys.ContentTypeTextPlain,
                        job.GetCorrelationId(),
                        Environment.CancellationToken);

                    context.Subject.IsErrorFileCreated = true;

                    context.Subject.Build.BuildCommitInfo = new BuildCommitInfo();
                    context.Subject.Build.BuildCommitInfo!.AddFileDetail(batchHandle.FileDetails.First().FileName, batchHandle.FileDetails.First().Hash);

                    _logger.LogCreateErrorFile(job.GetCorrelationId(), DateTimeOffset.UtcNow);
                }
                catch (Exception e)
                {
                    context.Subject.IsErrorFileCreated = false;
                    _logger.LogCreateErrorFileAddFileFailed(job.GetCorrelationId(), DateTimeOffset.UtcNow, e.Message);
                    return NodeResultStatus.Failed;

                }

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
            return _fileNameGeneratorService.GenerateFileName(Environment.Configuration[S100ErrorFileNameTemplate]!, jobId);
        }
         
        private string GetErrorFileMessage(JobId jobId)
        {
            return Environment.Configuration[S100ErrorFileMessageTemplate]!.Replace("[jobid]", (string)jobId);
        }
    }
}
