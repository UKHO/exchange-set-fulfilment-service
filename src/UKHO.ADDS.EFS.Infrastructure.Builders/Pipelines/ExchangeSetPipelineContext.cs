using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UKHO.ADDS.EFS.Domain.Builds;
using UKHO.ADDS.EFS.Domain.Jobs;
using UKHO.ADDS.EFS.Infrastructure.Builders.Factories;
using UKHO.ADDS.EFS.Infrastructure.Builders.Logging;
using UKHO.ADDS.Infrastructure.Serialization.Json;

namespace UKHO.ADDS.EFS.Infrastructure.Builders.Pipelines
{
    public abstract class ExchangeSetPipelineContext<TBuild>(
        IConfiguration configuration,
        IQueueClientFactory queueClientFactory,
        BlobClientFactory blobClientFactory,
        ILoggerFactory loggerFactory) where TBuild : Build
    {
        private readonly List<BuildNodeStatus> _statuses = [];

        public IConfiguration Configuration { get; } = configuration;

        public ILoggerFactory LoggerFactory { get; } = loggerFactory;

        public IQueueClientFactory QueueClientFactory { get; } = queueClientFactory;

        public BlobClientFactory BlobClientFactory { get; } = blobClientFactory;

        public JobId JobId { get; set; }

        public BatchId BatchId { get; set; }

        public string? FileShareEndpoint { get; set; }

        public string? FileShareHealthEndpoint { get; set; }

        public TBuild? Build { get; set; }

        public string? ExchangeSetNameTemplate { get; set; }

        public void AddStatus(BuildNodeStatus status)
        {
            _statuses.Add(status);
        }

        public IEnumerable<BuildNodeStatus> Statuses => _statuses;

        public async Task CompleteBuild(IConfiguration configuration, JsonMemorySink sink, BuilderExitCode exitCode)
        {
            if (Build is null)
            {
                throw new InvalidOperationException("Build is null in CompleteBuild.");
            }

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
    }
}
