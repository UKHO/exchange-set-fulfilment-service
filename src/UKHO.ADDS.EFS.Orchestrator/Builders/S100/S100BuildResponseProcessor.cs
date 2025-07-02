using Serilog.Events;
using UKHO.ADDS.EFS.Builds;
using UKHO.ADDS.EFS.Configuration.Orchestrator;
using UKHO.ADDS.EFS.Jobs;
using UKHO.ADDS.EFS.Messages;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging.NewViews;
using UKHO.ADDS.EFS.Orchestrator.Services2.Infrastructure;
using UKHO.ADDS.EFS.Orchestrator.Tables;
using UKHO.ADDS.EFS.Orchestrator.Tables.S100;

namespace UKHO.ADDS.EFS.Orchestrator.Builders.S100
{
    internal class S100BuildResponseProcessor : BuildResponseProcessor
    {
        private readonly S100ExchangeSetJobTable _jobTable;
        private readonly ExchangeSetTimestampTable _timestampTable;
        private readonly BuildStatusTable _statusTable;
        private readonly BuilderLogForwarder _logForwarder;
        private readonly FileShareService _fileShareService;
        private readonly ILogger<S100BuildResponseProcessor> _logger;

        public S100BuildResponseProcessor(S100ExchangeSetJobTable jobTable, ExchangeSetTimestampTable timestampTable, BuildStatusTable statusTable, BuilderLogForwarder logForwarder, FileShareService fileShareService, ILogger<S100BuildResponseProcessor> logger)
        {
            _jobTable = jobTable;
            _timestampTable = timestampTable;
            _statusTable = statusTable;
            _logForwarder = logForwarder;
            _fileShareService = fileShareService;
            _logger = logger;
        }

        public override async Task ProcessBuildResponseAsync(BuildResponse buildResponse, BuildSummary buildSummary, CancellationToken stoppingToken)
        {
            var existingStatusResponse = await _statusTable.GetAsync(buildResponse.JobId, buildResponse.JobId);

            if (existingStatusResponse.IsSuccess(out var existingStatus))
            {
                existingStatus.ExitCode = buildResponse.ExitCode;
                existingStatus.EndTimestamp = DateTime.UtcNow;

                existingStatus.Nodes.AddRange(buildSummary.Statuses!);

                await _statusTable.UpdateAsync(existingStatus);
            }

            // Replay the logs
            _logForwarder.ForwardLogs(buildSummary.LogMessages!, ExchangeSetDataStandard.S100, buildResponse.JobId);

            if (buildResponse.ExitCode == BuilderExitCode.Success)
            {
                await CommitAndExpireBatches(buildSummary, stoppingToken);
            }
        }

        private async Task CommitAndExpireBatches(BuildSummary buildSummary, CancellationToken stoppingToken)
        {
            var jobResult = await _jobTable.GetAsync(buildSummary.JobId, buildSummary.JobId);

            if (jobResult.IsSuccess(out var job))
            {

                // Try to commit the batch
                var commitBatchResult = await _fileShareService.CommitBatchAsync(job.BatchId, job.GetCorrelationId(), stoppingToken);
                if (!commitBatchResult.IsSuccess(out _, out _))
                {
                    job.State = ExchangeSetJobState.Failed;
                    return;
                }

                // Search for other committed batches
                var searchResult = await _fileShareService.SearchCommittedBatchesExcludingCurrentAsync(job.BatchId, job.GetCorrelationId(), stoppingToken);
                if (!searchResult.IsSuccess(out var searchResponse, out _))
                {
                    job.State = ExchangeSetJobState.Failed;
                    return;
                }

                // No previous batches found, mark as succeeded
                if (searchResponse?.Entries == null || searchResponse.Entries.Count == 0)
                {
                    job.State = ExchangeSetJobState.Succeeded;
                    return;
                }

                // Try to set expiry date on previous batches
                var expiryResult = await _fileShareService.SetExpiryDateAsync(searchResponse.Entries, job.GetCorrelationId(), stoppingToken);
                job.State = expiryResult.IsSuccess(out _, out _)
                    ? ExchangeSetJobState.Succeeded
                    : ExchangeSetJobState.Failed;

                if (job.State == ExchangeSetJobState.Succeeded)
                {
                    var updateTimestampEntity = new ExchangeSetTimestamp()
                    {
                        DataStandard = job.DataStandard,
                        Timestamp = job.SalesCatalogueTimestamp
                    };

                    await _timestampTable.UpsertAsync(updateTimestampEntity);
                }

                await _jobTable.UpdateAsync(job);
                _logger.LogJobCompleted(ExchangeSetJobLogView.Create(job));
            }
            else
            {
                // TODO handle/log                
            }
        }
    }
}
