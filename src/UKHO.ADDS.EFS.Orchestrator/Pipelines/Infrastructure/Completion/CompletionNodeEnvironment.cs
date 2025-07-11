using UKHO.ADDS.EFS.Configuration.Orchestrator;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Completion
{
    internal class CompletionNodeEnvironment
    {
        private readonly CancellationToken _cancellationToken;
        private readonly IConfiguration _configuration;
        private readonly ILogger _logger;

        private readonly BuilderExitCode _exitCode;

        public CompletionNodeEnvironment(IConfiguration configuration, CancellationToken cancellationToken, ILogger logger, BuilderExitCode exitCode)
        {
            _configuration = configuration;
            _cancellationToken = cancellationToken;
            _logger = logger;

            _exitCode = exitCode;
        }


        public IConfiguration Configuration => _configuration;

        public CancellationToken CancellationToken => _cancellationToken;

        public ILogger Logger => _logger;

        public BuilderExitCode BuilderExitCode => _exitCode;
    }
}
