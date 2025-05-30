using UKHO.ADDS.Clients.FileShareService.ReadOnly.Models;
using UKHO.ADDS.EFS.Builder.S100.IIC;
using UKHO.ADDS.EFS.Builder.S100.Services;
using UKHO.ADDS.EFS.Entities;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace UKHO.ADDS.EFS.Builder.S100.Pipelines
{
    internal class ExchangeSetPipelineContext
    {
        private readonly IConfiguration _configuration;
        private readonly INodeStatusWriter _nodeStatusWriter;
        private readonly IToolClient _toolClient;
        private readonly ILoggerFactory _loggerFactory;

        public ExchangeSetPipelineContext(IConfiguration configuration, INodeStatusWriter nodeStatusWriter, IToolClient toolClient, ILoggerFactory loggerFactory)
        {
            _configuration = configuration;
            _nodeStatusWriter = nodeStatusWriter;
            _toolClient = toolClient;
            _loggerFactory = loggerFactory;
        }

        public IConfiguration Configuration => _configuration;

        public IToolClient ToolClient => _toolClient;

        public INodeStatusWriter NodeStatusWriter => _nodeStatusWriter;

        public ILoggerFactory LoggerFactory => _loggerFactory;

        public string JobId { get; set; }
        public bool IsDebugSession { get; set; }
        public string FileShareEndpoint { get; set; }
        public string BuildServiceEndpoint { get; set; }
        public string WorkspaceAuthenticationKey { get; set; }
        public string WorkSpaceRootPath { get; set; } = @"/usr/local/tomcat/ROOT";
        public ExchangeSetJob Job { get; set; }
        public IEnumerable<BatchDetails> BatchDetails { get; set; }
        public string BatchId { get; set; }
        public string WorkSpaceFssDataPath { get; } = "fssdata";
        public string WorkSpaceSpoolPath { get; } = "spool";
    }
}
