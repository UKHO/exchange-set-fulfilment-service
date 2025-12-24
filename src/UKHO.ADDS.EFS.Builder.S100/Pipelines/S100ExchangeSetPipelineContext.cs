using System.Diagnostics.CodeAnalysis;
using UKHO.ADDS.Clients.FileShareService.ReadOnly.Models;
using UKHO.ADDS.EFS.Builder.S100.IIC;
using UKHO.ADDS.EFS.Domain.Builds.S100;
using UKHO.ADDS.EFS.Infrastructure.Builders.Factories;
using UKHO.ADDS.EFS.Infrastructure.Builders.Pipelines;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace UKHO.ADDS.EFS.Builder.S100.Pipelines
{
    [ExcludeFromCodeCoverage]
    internal class S100ExchangeSetPipelineContext : ExchangeSetPipelineContext<S100Build>
    {
        
        private readonly IToolClient _toolClient;

        public S100ExchangeSetPipelineContext(
            IConfiguration configuration,
            IToolClient toolClient,
            IQueueClientFactory queueClientFactory,
            BlobClientFactory blobClientFactory,
            ILoggerFactory loggerFactory)
            : base(configuration, queueClientFactory, blobClientFactory, loggerFactory)
        {
            _toolClient = toolClient;
        }

        public IToolClient ToolClient => _toolClient;

        public string WorkspaceAuthenticationKey { get; set; }
        public IEnumerable<BatchDetails> BatchDetails { get; set; }
        public IEnumerable<string> BatchFileNameDetails { get; set; }

        public string WorkSpaceRootPath { get; set; } = "/usr/local/tomcat/ROOT";
        public string WorkSpaceSpoolPath { get; } = "spool";
        public string ExchangeSetFilePath { get; set; } = "/usr/local/tomcat/ROOT/xchg";
        public string ExchangeSetArchiveFolderName { get; set; } = "ExchangeSetArchive";
    }
}
