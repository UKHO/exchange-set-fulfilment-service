using FakeItEasy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UKHO.ADDS.EFS.Builder.S100.IIC;
using UKHO.ADDS.EFS.Builder.S100.Pipelines;
using UKHO.ADDS.EFS.Builder.S100.Pipelines.Startup;
using UKHO.ADDS.EFS.Builder.S100.Services;
using UKHO.ADDS.EFS.Configuration.Orchestrator;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Builder.S100.UnitTests.Pipeline.Startup
{
    internal class ReadConfigurationNodeTests
    {
        private ReadConfigurationNode _node;
        private IExecutionContext<ExchangeSetPipelineContext> _context;
        private ExchangeSetPipelineContext _subject;
        private IConfiguration _configuration;
        private IToolClient _toolClient;
        private ILoggerFactory _loggerFactory;

        [SetUp]
        public void SetUp()
        {
            _node = new ReadConfigurationNode();

            var inMemorySettings = new List<KeyValuePair<string, string?>>
                {
                    new("Endpoints:FileShareService", "https://default-fileshare"),
                    new("Endpoints:BuildService", "https://default-buildservice")
                };
            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            _toolClient = A.Fake<IToolClient>();
            _loggerFactory = LoggerFactory.Create(builder => builder.AddDebug());

            _subject = new ExchangeSetPipelineContext(_configuration, _toolClient, null, null, _loggerFactory);
            _context = A.Fake<IExecutionContext<ExchangeSetPipelineContext>>();
            A.CallTo(() => _context.Subject).Returns(_subject);
        }

        [TearDown]
        public void TearDown()
        {
            if (_loggerFactory is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        [Test]
        public async Task WhenPerformExecuteAsyncUsesValidEnvironmentVariables_ThenReturnsSucceeded()
        {
            Environment.SetEnvironmentVariable(BuilderEnvironmentVariables.JobId, "TestJobId");
            Environment.SetEnvironmentVariable(BuilderEnvironmentVariables.WorkspaceKey, "TestWorkspaceKey");
            Environment.SetEnvironmentVariable(BuilderEnvironmentVariables.FileShareEndpoint, "https://env-fileshare");
            Environment.SetEnvironmentVariable(BuilderEnvironmentVariables.BuildServiceEndpoint, "https://env-buildservice");
            Environment.SetEnvironmentVariable(BuilderEnvironmentVariables.BatchId, "TestBatchId");

            var result = await _node.ExecuteAsync(_context);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Succeeded));
            Assert.That(_subject.JobId, Is.EqualTo("TestJobId"));
            Assert.That(_subject.WorkspaceAuthenticationKey, Is.EqualTo("TestWorkspaceKey"));
            Assert.That(_subject.FileShareEndpoint, Is.EqualTo("https://env-fileshare"));
            Assert.That(_subject.BatchId, Is.EqualTo("TestBatchId"));

            // Clean up
            Environment.SetEnvironmentVariable(BuilderEnvironmentVariables.JobId, null);
            Environment.SetEnvironmentVariable(BuilderEnvironmentVariables.WorkspaceKey, null);
            Environment.SetEnvironmentVariable(BuilderEnvironmentVariables.FileShareEndpoint, null);
            Environment.SetEnvironmentVariable(BuilderEnvironmentVariables.BuildServiceEndpoint, null);
            Environment.SetEnvironmentVariable(BuilderEnvironmentVariables.BatchId, null);
        }

        [Test]
        public async Task WhenPerformExecuteAsyncEnvVarsMissing_ThenUsesDefaultsAndReturnSucceeded()
        {
            var result = await _node.ExecuteAsync(_context);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Succeeded));
            Assert.That(_subject.JobId, Is.Not.Null.And.Not.Empty);
            Assert.That(_subject.FileShareEndpoint, Is.EqualTo("https://default-fileshare"));
            Assert.That(_subject.BatchId, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public async Task WhenPerformExecuteAsyncSetsDebugSessionAndGeneratesJobId_ThenReturnSucceeded()
        {
            Environment.SetEnvironmentVariable(BuilderEnvironmentVariables.JobId, "DebugJobId");

            var result = await _node.ExecuteAsync(_context);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Succeeded));
            Assert.That(_subject.JobId, Is.Not.Null.And.Not.Empty);
            Assert.That(Guid.TryParse(_subject.JobId, out _), Is.True);

            Environment.SetEnvironmentVariable(BuilderEnvironmentVariables.JobId, null);
        }
    }
}

