﻿using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UKHO.ADDS.EFS.Domain.Builds;
using UKHO.ADDS.EFS.Domain.Jobs;
using UKHO.ADDS.EFS.Infrastructure.Builders.Factories;
using UKHO.ADDS.EFS.Infrastructure.Builders.Logging;
using UKHO.ADDS.Infrastructure.Serialization.Json;

namespace UKHO.ADDS.EFS.Infrastructure.Builders.Pipelines
{
    public abstract class ExchangeSetPipelineContext<TBuild> where TBuild : Build
    {
        private readonly IConfiguration _configuration;

        private readonly QueueClientFactory _queueClientFactory;
        private readonly BlobClientFactory _blobClientFactory;
        private readonly ILoggerFactory _loggerFactory;
        private readonly List<BuildNodeStatus> _statuses;

        protected ExchangeSetPipelineContext(
            IConfiguration configuration,
            QueueClientFactory queueClientFactory,
            BlobClientFactory blobClientFactory,
            ILoggerFactory loggerFactory)
        {
            _configuration = configuration;
            _queueClientFactory = queueClientFactory;
            _blobClientFactory = blobClientFactory;
            _loggerFactory = loggerFactory;

            _statuses = [];
        }

        public IConfiguration Configuration => _configuration;

        public ILoggerFactory LoggerFactory => _loggerFactory;

        public QueueClientFactory QueueClientFactory => _queueClientFactory;

        public BlobClientFactory BlobClientFactory => _blobClientFactory;

        public JobId JobId { get; set; }

        public BatchId BatchId { get; set; }

        public string FileShareEndpoint { get; set; }

        public string FileShareHealthEndpoint { get; set; }

        public TBuild Build { get; set; }

        public string ExchangeSetNameTemplate { get; set; }

        public void AddStatus(BuildNodeStatus status)
        {
            _statuses.Add(status);
        }

        public IEnumerable<BuildNodeStatus> Statuses => _statuses;

        public async Task CompleteBuild(IConfiguration configuration, JsonMemorySink sink, BuilderExitCode exitCode)
        {
            var logger = LoggerFactory.CreateLogger<ExchangeSetPipelineContext<TBuild>>();
            
            try
            {
                Build.SetOutputs(Statuses, sink.GetLogLines());

                var queueClient = QueueClientFactory.CreateResponseQueueClient(configuration);
                var blobClient = BlobClientFactory.CreateBlobClient(configuration, $"{JobId}/{JobId}");

                using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(JsonCodec.Encode(Build))))
                {
                    await blobClient.UploadAsync(stream, overwrite: true);
                }

                var response = new BuildResponse() { JobId = JobId, ExitCode = exitCode };

                await queueClient.SendMessageAsync(JsonCodec.Encode(response));
            }
            catch (Exception ex)
            {
                // Set exit code to failed if the exception occurred and original exit code was success
                var failedExitCode = exitCode == BuilderExitCode.Success ? BuilderExitCode.Failed : exitCode;
                logger.LogError(ex, "An unhandled exception occurred during build completion for JobId: {JobId} and exit code:{failedExitCode}", JobId, failedExitCode);

                // Attempt to send failure notification
                throw;
            }
        }
    }
}
