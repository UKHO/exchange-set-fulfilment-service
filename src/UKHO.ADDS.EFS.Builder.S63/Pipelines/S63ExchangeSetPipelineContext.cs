using System.Diagnostics.CodeAnalysis;
using UKHO.ADDS.Clients.FileShareService.ReadOnly.Models;
using UKHO.ADDS.EFS.Builder.Common.Factories;
using UKHO.ADDS.EFS.Builder.Common.Pipelines;
using UKHO.ADDS.EFS.Jobs.S63;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace UKHO.ADDS.EFS.Builder.S63.Pipelines
{
    [ExcludeFromCodeCoverage]
    internal class S63ExchangeSetPipelineContext : ExchangeSetPipelineContext<S63ExchangeSetJob>
    {
        
        public S63ExchangeSetPipelineContext(
            IConfiguration configuration,
            QueueClientFactory queueClientFactory,
            BlobClientFactory blobClientFactory,
            ILoggerFactory loggerFactory)
            : base(configuration, queueClientFactory, blobClientFactory, loggerFactory)
        {
        }

        public string WorkspaceAuthenticationKey { get; set; }
        public IEnumerable<BatchDetails> BatchDetails { get; set; }
        
        public string WorkSpaceRootPath { get; set; } = "/usr/local/tomcat/ROOT";
        public string WorkSpaceSpoolPath { get; } = "spool";
        public string WorkSpaceSpoolDataSetFilesPath { get; } = "dataSet_files";
        public string WorkSpaceSpoolSupportFilesPath { get; } = "support_files";
        public string ExchangeSetFilePath { get; set; } = "/usr/local/tomcat/ROOT/xchg";
        public string ExchangeSetArchiveFolderName { get; set; } = "ExchangeSetArchive";
    }
}
