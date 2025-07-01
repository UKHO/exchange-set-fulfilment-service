using Serilog.Events;
using UKHO.ADDS.EFS.Builds;
using UKHO.ADDS.EFS.Messages;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging;
using UKHO.ADDS.EFS.Orchestrator.Tables;

namespace UKHO.ADDS.EFS.Orchestrator.Builders.S100
{
    internal class S100BuildResponseProcessor : BuildResponseProcessor
    {
        private readonly BuildStatusTable _statusTable;
        private readonly BuilderLogForwarder _logForwarder;

        public S100BuildResponseProcessor(BuildStatusTable statusTable, BuilderLogForwarder logForwarder)
        {
            _statusTable = statusTable;
            _logForwarder = logForwarder;
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
        }
    }
}
