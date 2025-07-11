﻿using Azure.Storage.Queues;
using UKHO.ADDS.EFS.Builds;
using UKHO.ADDS.EFS.Configuration.Namespaces;
using UKHO.ADDS.EFS.Jobs;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Completion;

namespace UKHO.ADDS.EFS.Orchestrator.Monitors
{
    internal class S100BuildResponseMonitor : QueueMonitor<BuildResponse>
    {
        private readonly CompletionPipelineFactory _pipelineFactory;

        public S100BuildResponseMonitor(CompletionPipelineFactory pipelineFactory, QueueServiceClient queueServiceClient, IConfiguration configuration, ILogger<S100BuildResponseMonitor> logger)
            : base(StorageConfiguration.S100BuildResponseQueueName, "Queues:S100ResponseQueue:PollingIntervalSeconds", "Queues:S100ResponseQueue:BatchSize", queueServiceClient, configuration, logger) =>
            _pipelineFactory = pipelineFactory;

        protected override async Task ProcessMessageAsync(BuildResponse messageInstance, CancellationToken stoppingToken)
        {
            var pipelineContext = CompletionPipelineParameters.CreateFrom(messageInstance, DataStandard.S100);

            var pipeline = _pipelineFactory.CreateCompletionPipeline(pipelineContext);
            await pipeline.RunAsync(stoppingToken);
        }
    }
}
