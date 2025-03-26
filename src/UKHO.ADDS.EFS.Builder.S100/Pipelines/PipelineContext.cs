using UKHO.ADDS.EFS.Builder.S100.IIC;
using UKHO.ADDS.EFS.Common.Entities;

namespace UKHO.ADDS.EFS.Builder.S100.Pipelines
{
    internal class PipelineContext
    {
        private readonly IConfiguration _configuration;
        private readonly IToolClient _toolClient;

        public PipelineContext(IConfiguration configuration, IToolClient toolClient)
        {
            _configuration = configuration;
            _toolClient = toolClient;
        }

        public IConfiguration Configuration => _configuration;

        public IToolClient ToolClient => _toolClient;

        public string RequestId { get; set; }
        public string FileShareEndpoint { get; set; }
        public string SalesCatalogueEndpoint { get; set; }
        public string BuildServiceEndpoint { get; set; }
        public string WorkspaceRootPath { get; set; }
        public ExchangeSetRequest Request { get; set; }
    }
}
