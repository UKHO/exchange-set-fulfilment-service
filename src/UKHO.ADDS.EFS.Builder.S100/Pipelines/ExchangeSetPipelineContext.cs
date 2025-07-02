using System.Diagnostics.CodeAnalysis;
using UKHO.ADDS.Clients.FileShareService.ReadOnly.Models;
using UKHO.ADDS.EFS.Builder.S100.Factories;
using UKHO.ADDS.EFS.Builder.S100.IIC;
using UKHO.ADDS.EFS.Builder.S100.Services;
using UKHO.ADDS.EFS.Builds;
using UKHO.ADDS.EFS.Jobs.S100;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace UKHO.ADDS.EFS.Builder.S100.Pipelines
{
    [ExcludeFromCodeCoverage]
    internal class ExchangeSetPipelineContext
    {
        private readonly IConfiguration _configuration;
        private readonly IToolClient _toolClient;
        private readonly QueueClientFactory _queueClientFactory;
        private readonly BlobClientFactory _blobClientFactory;
        private readonly ILoggerFactory _loggerFactory;

        private readonly BuildSummary _summary;

        private string _jobId;
        private string _batchId;

        public ExchangeSetPipelineContext(
            IConfiguration configuration,
            IToolClient toolClient,
            QueueClientFactory queueClientFactory,
            BlobClientFactory blobClientFactory,
            ILoggerFactory loggerFactory)
        {
            _configuration = configuration;
            _toolClient = toolClient;
            _queueClientFactory = queueClientFactory;
            _blobClientFactory = blobClientFactory;
            _loggerFactory = loggerFactory;

            _jobId = string.Empty;
            _batchId = string.Empty;

            _summary = new BuildSummary();
        }

        public IConfiguration Configuration => _configuration;

        public IToolClient ToolClient => _toolClient;

        public ILoggerFactory LoggerFactory => _loggerFactory;

        public BuildSummary Summary => _summary;

        public QueueClientFactory QueueClientFactory => _queueClientFactory;

        public BlobClientFactory BlobClientFactory => _blobClientFactory;

        public string JobId
        {
            get => _jobId;
            set
            {
                _jobId = value;
                _summary.JobId = value;
            }
        }

        public string BatchId
        {
            get => _batchId;
            set
            {
                _batchId = value;
                _summary.BatchId = value;
            }
        }

        public string FileShareEndpoint { get; set; }
        public string WorkspaceAuthenticationKey { get; set; }
        public S100ExchangeSetJob Job { get; set; }
        public IEnumerable<BatchDetails> BatchDetails { get; set; }
        
        public string WorkSpaceRootPath { get; set; } = "/usr/local/tomcat/ROOT";
        public string WorkSpaceSpoolPath { get; } = "spool";
        public string WorkSpaceSpoolDataSetFilesPath { get; } = "dataSet_files";
        public string WorkSpaceSpoolSupportFilesPath { get; } = "support_files";
        public string ExchangeSetNameTemplate { get; set; }
        public string ExchangeSetFilePath { get; set; } = "/usr/local/tomcat/ROOT/xchg";
        public string ExchangeSetArchiveFolderName { get; set; } = "ExchangeSetArchive";
    }
}
