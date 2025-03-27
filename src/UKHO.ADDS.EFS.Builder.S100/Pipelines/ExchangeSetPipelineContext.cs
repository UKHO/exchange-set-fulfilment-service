using UKHO.ADDS.EFS.Builder.S100.IIC;
using UKHO.ADDS.EFS.Builder.S100.Services;
using UKHO.ADDS.EFS.Common.Entities;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace UKHO.ADDS.EFS.Builder.S100.Pipelines
{
    internal class ExchangeSetPipelineContext
    {
        private readonly IConfiguration _configuration;
        private readonly INodeStatusWriter _nodeStatusWriter;
        private readonly IToolClient _toolClient;

        public ExchangeSetPipelineContext(IConfiguration configuration, INodeStatusWriter nodeStatusWriter, IToolClient toolClient)
        {
            _configuration = configuration;
            _nodeStatusWriter = nodeStatusWriter;
            _toolClient = toolClient;
        }

        public IConfiguration Configuration => _configuration;

        public IToolClient ToolClient => _toolClient;

        public INodeStatusWriter NodeStatusWriter => _nodeStatusWriter;

        public string RequestId { get; set; }
        public bool IsDebugSession { get; set; }
        public string FileShareEndpoint { get; set; }
        public string SalesCatalogueEndpoint { get; set; }
        public string BuildServiceEndpoint { get; set; }
        public string WorkspaceRootPath { get; set; }
        public ExchangeSetRequest Request { get; set; }
    }
}
