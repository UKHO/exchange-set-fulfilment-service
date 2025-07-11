namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Assembly
{
    internal class AssemblyNodeEnvironment
    {
        private readonly CancellationToken _cancellationToken;
        private readonly IConfiguration _configuration;
        private readonly ILogger _logger;

        public AssemblyNodeEnvironment(IConfiguration configuration, CancellationToken cancellationToken, ILogger logger)
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
