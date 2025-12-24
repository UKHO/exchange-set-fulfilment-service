using Azure;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using FakeItEasy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UKHO.ADDS.EFS.Builder.S100.IIC;
using UKHO.ADDS.EFS.Builder.S100.Pipelines;
using UKHO.ADDS.EFS.Builder.S100.Pipelines.Startup;
using UKHO.ADDS.EFS.Infrastructure.Builders.Factories;
using UKHO.ADDS.EFS.Infrastructure.Configuration.Orchestrator;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Builder.S100.UnitTests.Pipeline.Startup
{
    internal class ReadConfigurationNodeTests
    {
        private ReadConfigurationNode _readConfigurationNode;
        private IExecutionContext<S100ExchangeSetPipelineContext> _context;
        private S100ExchangeSetPipelineContext _subject;
        private IConfiguration _configuration;
        private IToolClient _toolClient;
        private ILoggerFactory _loggerFactory;
        private IQueueClientFactory _queueClientFactory;

        [SetUp]
        public void SetUp()
        {
            var inMemorySettings = new List<KeyValuePair<string, string?>>
                {
                    new(BuilderEnvironmentVariables.RequestQueueName, "s100buildrequest"),
                    new(BuilderEnvironmentVariables.AddsEnvironment, "test"),
                    new(BuilderEnvironmentVariables.QueueEndpoint, "https://efsstoragetest.queue.core.windows.net/")
                };
            _configuration = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();
            _toolClient = A.Fake<IToolClient>();
            _loggerFactory = LoggerFactory.Create(builder => builder.AddDebug());
            _queueClientFactory = A.Fake<IQueueClientFactory>();

            _subject = new S100ExchangeSetPipelineContext(_configuration, _toolClient, _queueClientFactory, null!, _loggerFactory);
            _context = A.Fake<IExecutionContext<S100ExchangeSetPipelineContext>>();
            A.CallTo(() => _context.Subject).Returns(_subject);

            _readConfigurationNode = new ReadConfigurationNode();
        }

        [TearDown]
        public void TearDown()
        {
            if (_loggerFactory is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        private void SetUpMessage(string? messageText = null)
        {
            var queueClient = A.Fake<QueueClient>();
            var queueMessage = QueuesModelFactory.QueueMessage(
                messageId: "TestMessageId",
                popReceipt: "TestPopReceipt",
                messageText: messageText ?? "{\"Timestamp\":\"2025-12-24T14:02:02Z\",\"JobId\":\"TestJobId\",\"BatchId\":\"TestBatchId\",\"DataStandard\":0,\"ExchangeSetNameTemplate\":\"TestExchangeSetNameTemplate\",\"WorkspaceKey\":\"TestWorkspaceKey\"}",
                insertedOn: DateTimeOffset.UtcNow,
                expiresOn: DateTimeOffset.UtcNow.AddDays(7),
                dequeueCount: 1);
            var queueMessageResponse = Response.FromValue(queueMessage, A.Fake<Response>());
            A.CallTo(() => _queueClientFactory.CreateRequestQueueClient(_configuration)).Returns(queueClient);
            A.CallTo(() => queueClient.ReceiveMessageAsync(A<TimeSpan?>.Ignored, A<CancellationToken>.Ignored)).Returns(Task.FromResult(queueMessageResponse));
        }

        [Test]
        public async Task WhenPerformExecuteAsyncUsesValidEnvironmentVariables_ThenReturnsSucceeded()
        {
            SetUpMessage();
            _configuration[BuilderEnvironmentVariables.FileShareEndpoint] = "https://env-fileshare";
            _configuration[BuilderEnvironmentVariables.FileShareHealthEndpoint] = "https://env-fileshare/health";

            var result = await _readConfigurationNode.ExecuteAsync(_context);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Succeeded));
                Assert.That(_subject.JobId, Is.EqualTo("TestJobId"));
                Assert.That(_subject.BatchId, Is.EqualTo("TestBatchId"));
                Assert.That(_subject.WorkspaceAuthenticationKey, Is.EqualTo("TestWorkspaceKey"));
                Assert.That(_subject.ExchangeSetNameTemplate, Is.EqualTo("TestExchangeSetNameTemplate"));
                Assert.That(_subject.FileShareEndpoint, Is.EqualTo("https://env-fileshare"));
                Assert.That(_subject.FileShareHealthEndpoint, Is.EqualTo("https://env-fileshare/health"));
            }
        }

        [Test]
        public async Task WhenPerformExecuteAsyncUsesValidDebugVariables_ThenReturnsSucceeded()
        {
            SetUpMessage();
            _configuration["DebugEndpoints:FileShareService"] = "https://debug-fileshare";
            _configuration["DebugEndpoints:FileShareServiceHealth"] = "https://debug-fileshare/health";

            var result = await _readConfigurationNode.ExecuteAsync(_context);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Succeeded));
                Assert.That(_subject.JobId, Is.EqualTo("TestJobId"));
                Assert.That(_subject.BatchId, Is.EqualTo("TestBatchId"));
                Assert.That(_subject.WorkspaceAuthenticationKey, Is.EqualTo("TestWorkspaceKey"));
                Assert.That(_subject.ExchangeSetNameTemplate, Is.EqualTo("TestExchangeSetNameTemplate"));
                Assert.That(_subject.FileShareEndpoint, Is.EqualTo("https://debug-fileshare"));
                Assert.That(_subject.FileShareHealthEndpoint, Is.EqualTo("https://debug-fileshare/health"));
            }
        }

        [Test]
        public async Task WhenPerformExecuteAsyncHasInvalidMessage_ThenReturnsFailed()
        {
            SetUpMessage("{\"Invalid\":\"Message\"}");

            var result = await _readConfigurationNode.ExecuteAsync(_context);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Failed));
        }
    }
}

