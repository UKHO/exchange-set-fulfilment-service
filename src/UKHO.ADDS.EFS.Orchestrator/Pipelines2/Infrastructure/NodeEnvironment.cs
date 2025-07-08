namespace UKHO.ADDS.EFS.Orchestrator.Pipelines2.Infrastructure
{
    internal class NodeEnvironment
    {
        private readonly CancellationToken _cancellationToken;
        private readonly IConfiguration _configuration;
        private readonly ILogger _logger;

        public NodeEnvironment(IConfiguration configuration, CancellationToken cancellationToken, ILogger logger)
        {
            _configuration = configuration;
            _cancellationToken = cancellationToken;
            _logger = logger;
        }

        public IConfiguration Configuration => _configuration;

        public CancellationToken CancellationToken => _cancellationToken;

        public ILogger Logger => _logger;
    }
}
