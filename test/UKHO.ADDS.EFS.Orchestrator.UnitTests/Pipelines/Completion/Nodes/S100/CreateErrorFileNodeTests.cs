using System.Text;
using FakeItEasy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UKHO.ADDS.Clients.FileShareService.ReadWrite.Models.Response;
using UKHO.ADDS.Configuration.Schema;
using UKHO.ADDS.EFS.Builds.S100;
using UKHO.ADDS.EFS.Configuration.Orchestrator;
using UKHO.ADDS.EFS.Constants;
using UKHO.ADDS.EFS.Jobs;
using UKHO.ADDS.EFS.Orchestrator.Jobs;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Completion.Nodes.S100;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Completion;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Services;
using UKHO.ADDS.EFS.Orchestrator.Services.Infrastructure;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;
using UKHO.ADDS.Infrastructure.Results;

namespace UKHO.ADDS.EFS.Orchestrator.UnitTests.Pipelines.Completion.Nodes.S100
{
    [TestFixture]
    internal class CreateErrorFileNodeTests
    {
        private CreateErrorFileNode _createErrorFileNode;
        private IOrchestratorFileShareClient _fileShareClient;
        private ILogger<CreateErrorFileNode> _logger;
        private CompletionNodeEnvironment _nodeEnvironment;
        private IExecutionContext<PipelineContext<S100Build>> _executionContext;
        private PipelineContext<S100Build> _pipelineContext;
        private IConfiguration _configuration;
        private readonly CancellationToken _cancellationToken = CancellationToken.None;

        [SetUp]
        public void SetUp()
        {
            _fileShareClient = A.Fake<IOrchestratorFileShareClient>();
            _logger = A.Fake<ILogger<CreateErrorFileNode>>();
            _configuration = A.Fake<IConfiguration>();
            var environmentLogger = A.Fake<ILogger>();

            _nodeEnvironment = new CompletionNodeEnvironment(_configuration, _cancellationToken, environmentLogger, BuilderExitCode.Failed);
            _createErrorFileNode = new CreateErrorFileNode(_nodeEnvironment, _fileShareClient, _logger);

            var job = new Job
            {
                Id = "test-job-id",
                Timestamp = DateTime.UtcNow,
                DataStandard = DataStandard.S100,
                RequestedProducts = "",
                RequestedFilter = "",
                BatchId = "test-batch-id"
            };

            var build = new S100Build
            {
                JobId = "test-job-id",
                DataStandard = DataStandard.S100,
                BatchId = "test-batch-id"
            };

            _pipelineContext = new PipelineContext<S100Build>(job, build, A.Fake<IStorageService>());
            _executionContext = A.Fake<IExecutionContext<PipelineContext<S100Build>>>();
            A.CallTo(() => _executionContext.Subject).Returns(_pipelineContext);
        }

        [Test]
        public void WhenFileShareClientIsNull_ThenThrowsArgumentNullException()
        {
            var exception = Assert.Throws<ArgumentNullException>(() => new CreateErrorFileNode(_nodeEnvironment, null, _logger));
            Assert.That(exception!.ParamName, Is.EqualTo("fileShareClient"));
        }

        [Test]
        public void WhenLoggerIsNull_ThenThrowsArgumentNullException()
        {
            var exception = Assert.Throws<ArgumentNullException>(() => new CreateErrorFileNode(_nodeEnvironment, _fileShareClient, null));
            Assert.That(exception!.ParamName, Is.EqualTo("logger"));
        }

        [Test]
        public async Task WhenShouldExecuteAsyncCalledWithFailedBuilderExitCodeAndBatchId_ThenReturnsTrue()
        {
            var shouldExecute = await _createErrorFileNode.ShouldExecuteAsync(_executionContext);

            Assert.That(shouldExecute, Is.True);
        }

        [Test]
        public async Task WhenShouldExecuteAsyncCalledWithSuccessBuilderExitCode_ThenReturnsFalse()
        {
            var successEnvironment = new CompletionNodeEnvironment(_configuration, _cancellationToken, A.Fake<ILogger>(), BuilderExitCode.Success);
            var node = new CreateErrorFileNode(successEnvironment, _fileShareClient, _logger);

            var shouldExecute = await node.ShouldExecuteAsync(_executionContext);

            Assert.That(shouldExecute, Is.False);
        }

        [Test]
        public async Task WhenShouldExecuteAsyncCalledWithNotRunBuilderExitCode_ThenReturnsFalse()
        {
            var notRunEnvironment = new CompletionNodeEnvironment(_configuration, _cancellationToken, A.Fake<ILogger>(), BuilderExitCode.NotRun);
            var node = new CreateErrorFileNode(notRunEnvironment, _fileShareClient, _logger);

            var shouldExecute = await node.ShouldExecuteAsync(_executionContext);

            Assert.That(shouldExecute, Is.False);
        }

        [Test]
        public async Task WhenShouldExecuteAsyncCalledWithNullBatchId_ThenReturnsFalse()
        {
            _pipelineContext.Job.BatchId = null;

            var shouldExecute = await _createErrorFileNode.ShouldExecuteAsync(_executionContext);

            Assert.That(shouldExecute, Is.False);
        }

        [Test]
        public async Task WhenShouldExecuteAsyncCalledWithEmptyBatchId_ThenReturnsFalse()
        {
            _pipelineContext.Job.BatchId = string.Empty;

            var shouldExecute = await _createErrorFileNode.ShouldExecuteAsync(_executionContext);

            Assert.That(shouldExecute, Is.False);
        }

        [Test]
        public async Task WhenShouldExecuteAsyncCalledWithWhitespaceBatchId_ThenReturnsFalse()
        {
            _pipelineContext.Job.BatchId = "   ";

            var shouldExecute = await _createErrorFileNode.ShouldExecuteAsync(_executionContext);

            Assert.That(shouldExecute, Is.False);
        }

        [Test]
        public async Task WhenExecuteAsyncCalledAndAddFileSucceeds_ThenReturnsSucceededAndLogsSuccess()
        {
            var addFileResponse = new AddFileToBatchResponse();
            A.CallTo(() => _fileShareClient.AddFileToBatchAsync(
                A<string>.That.IsEqualTo("test-batch-id"),
                A<Stream>._,
                A<string>.That.IsEqualTo("error.txt"),
                A<string>.That.IsEqualTo(ApiHeaderKeys.ContentTypeTextPlain),
                A<string>.That.IsEqualTo("test-job-id"),
                A<CancellationToken>._))
                .Returns(Result.Success(addFileResponse));

            var result = await _createErrorFileNode.ExecuteAsync(_executionContext);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Succeeded));

            A.CallTo(_logger).Where(call => call.Method.Name == "Log" && call.GetArgument<LogLevel>(0) == LogLevel.Error).MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task WhenExecuteAsyncCalledInLocalEnvironment_ThenUsesJobIdInFileName()
        {
            A.CallTo(() => _configuration[WellKnownConfigurationName.AddsEnvironmentName]).Returns("local");
            var addFileResponse = new AddFileToBatchResponse();

            A.CallTo(() => _fileShareClient.AddFileToBatchAsync(
                A<string>.That.IsEqualTo("test-batch-id"),
                A<Stream>._,
                A<string>.That.IsEqualTo("error_test-job-id.txt"),
                A<string>.That.IsEqualTo(ApiHeaderKeys.ContentTypeTextPlain),
                A<string>.That.IsEqualTo("test-job-id"),
                A<CancellationToken>._))
                .Returns(Result.Success(addFileResponse));

            var result = await _createErrorFileNode.ExecuteAsync(_executionContext);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Succeeded));

            A.CallTo(_logger).Where(call => call.Method.Name == "Log" && call.GetArgument<LogLevel>(0) == LogLevel.Error).MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task WhenExecuteAsyncCalledInLocalEnvironmentCaseInsensitive_ThenUsesJobIdInFileName()
        {
            A.CallTo(() => _configuration[WellKnownConfigurationName.AddsEnvironmentName]).Returns("LOCAL");
            var addFileResponse = new AddFileToBatchResponse();

            A.CallTo(() => _fileShareClient.AddFileToBatchAsync(
                A<string>._,
                A<Stream>._,
                A<string>.That.IsEqualTo("error_test-job-id.txt"),
                A<string>._,
                A<string>._,
                A<CancellationToken>._))
                .Returns(Result.Success(addFileResponse));

            var result = await _createErrorFileNode.ExecuteAsync(_executionContext);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Succeeded));
        }

        [Test]
        public async Task WhenExecuteAsyncCalledInNonLocalEnvironment_ThenUsesStandardFileName()
        {
            A.CallTo(() => _configuration[WellKnownConfigurationName.AddsEnvironmentName]).Returns("production");
            var addFileResponse = new AddFileToBatchResponse();

            A.CallTo(() => _fileShareClient.AddFileToBatchAsync(
                A<string>._,
                A<Stream>._,
                A<string>.That.IsEqualTo("error.txt"),
                A<string>._,
                A<string>._,
                A<CancellationToken>._))
                .Returns(Result.Success(addFileResponse));

            var result = await _createErrorFileNode.ExecuteAsync(_executionContext);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Succeeded));

            A.CallTo(_logger).Where(call => call.Method.Name == "Log" && call.GetArgument<LogLevel>(0) == LogLevel.Error).MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task WhenExecuteAsyncCalled_ThenErrorFileContainsCorrectMessage()
        {
            var capturedStream = new MemoryStream();
            var addFileResponse = new AddFileToBatchResponse();

            A.CallTo(() => _fileShareClient.AddFileToBatchAsync(
                A<string>._,
                A<Stream>._,
                A<string>._,
                A<string>._,
                A<string>._,
                A<CancellationToken>._))
                .Invokes((string batchId, Stream stream, string fileName, string contentType, string correlationId, CancellationToken ct) =>
                {
                    stream.CopyTo(capturedStream);
                })
                .Returns(Result.Success(addFileResponse));

            await _createErrorFileNode.ExecuteAsync(_executionContext);

            var content = Encoding.UTF8.GetString(capturedStream.ToArray());
            var expectedMessage = "There has been a problem in creating your exchange set, so we are unable to fulfil your request at this time. Please contact UKHO Customer Services quoting correlation ID test-job-id";

            Assert.That(content, Is.EqualTo(expectedMessage));
            A.CallTo(_logger).Where(call => call.Method.Name == "Log" && call.GetArgument<LogLevel>(0) == LogLevel.Error).MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task WhenExecuteAsyncCalled_ThenUsesCancellationTokenFromEnvironment()
        {
            var addFileResponse = new AddFileToBatchResponse();

            A.CallTo(() => _fileShareClient.AddFileToBatchAsync(
                A<string>._,
                A<Stream>._,
                A<string>._,
                A<string>._,
                A<string>._,
                _cancellationToken))
                .Returns(Result.Success(addFileResponse));

            var result = await _createErrorFileNode.ExecuteAsync(_executionContext);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Succeeded));

            A.CallTo(() => _fileShareClient.AddFileToBatchAsync(
                A<string>._,
                A<Stream>._,
                A<string>._,
                A<string>._,
                A<string>._,
                _cancellationToken))
                .MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task WhenExecuteAsyncCalledAndAddFileFails_ThenReturnsFailedAndLogsError()
        {
            var error = new Error("Add file failed");
            A.CallTo(() => _fileShareClient.AddFileToBatchAsync(
                A<string>._,
                A<Stream>._,
                A<string>._,
                A<string>._,
                A<string>._,
                A<CancellationToken>._))
                .Returns(Result.Failure<AddFileToBatchResponse>(error));

            var result = await _createErrorFileNode.ExecuteAsync(_executionContext);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Failed));

            A.CallTo(_logger).Where(call => call.Method.Name == "Log" && call.GetArgument<LogLevel>(0) == LogLevel.Error).MustHaveHappenedOnceExactly();

            A.CallTo(() => _logger.Log(A<LogLevel>.That.IsEqualTo(LogLevel.Error), A<EventId>._, A<IReadOnlyList<KeyValuePair<string, object>>>._, A<Exception>._, A<Func<IReadOnlyList<KeyValuePair<string, object>>, Exception, string>>._))
                .MustNotHaveHappened();
        }

        [Test]
        public async Task WhenExecuteAsyncCalledAndExceptionThrown_ThenReturnsFailedAndLogsException()
        {
            var testException = new Exception("Unexpected error");
            A.CallTo(() => _fileShareClient.AddFileToBatchAsync(
                A<string>._,
                A<Stream>._,
                A<string>._,
                A<string>._,
                A<string>._,
                A<CancellationToken>._))
                .Throws(testException);

            var result = await _createErrorFileNode.ExecuteAsync(_executionContext);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Failed));

            A.CallTo(_logger).Where(call => call.Method.Name == "Log" && call.GetArgument<LogLevel>(0) == LogLevel.Error).MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task WhenExecuteAsyncCalledWithNullEnvironmentConfig_ThenUsesStandardFileName()
        {
            A.CallTo(() => _configuration[WellKnownConfigurationName.AddsEnvironmentName]).Returns(null);
            var addFileResponse = new AddFileToBatchResponse();

            A.CallTo(() => _fileShareClient.AddFileToBatchAsync(
                A<string>._,
                A<Stream>._,
                A<string>.That.IsEqualTo("error.txt"),
                A<string>._,
                A<string>._,
                A<CancellationToken>._))
                .Returns(Result.Success(addFileResponse));

            var result = await _createErrorFileNode.ExecuteAsync(_executionContext);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Succeeded));
        }

        [Test]
        public async Task WhenExecuteAsyncCalledWithEmptyEnvironmentConfig_ThenUsesStandardFileName()
        {
            A.CallTo(() => _configuration[WellKnownConfigurationName.AddsEnvironmentName]).Returns(string.Empty);
            var addFileResponse = new AddFileToBatchResponse();

            A.CallTo(() => _fileShareClient.AddFileToBatchAsync(
                A<string>._,
                A<Stream>._,
                A<string>.That.IsEqualTo("error.txt"),
                A<string>._,
                A<string>._,
                A<CancellationToken>._))
                .Returns(Result.Success(addFileResponse));

            var result = await _createErrorFileNode.ExecuteAsync(_executionContext);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Succeeded));
        }

        [Test]
        public async Task WhenExecuteAsyncCalledWithOtherThenLocalEnvironmentConfig_ThenUsesStandardFileName()
        {
            A.CallTo(() => _configuration[WellKnownConfigurationName.AddsEnvironmentName]).Returns("test-environment");
            var addFileResponse = new AddFileToBatchResponse();

            A.CallTo(() => _fileShareClient.AddFileToBatchAsync(
                A<string>._,
                A<Stream>._,
                A<string>.That.IsEqualTo("error.txt"),
                A<string>._,
                A<string>._,
                A<CancellationToken>._))
                .Returns(Result.Success(addFileResponse));

            var result = await _createErrorFileNode.ExecuteAsync(_executionContext);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Succeeded));
        }

        [Test]
        public async Task WhenExecuteAsyncCalledWithLocalEnvironmentConfig_ThenUsesError_JobIdFilename()
        {
            A.CallTo(() => _configuration[WellKnownConfigurationName.AddsEnvironmentName]).Returns("local");
            var addFileResponse = new AddFileToBatchResponse();

            A.CallTo(() => _fileShareClient.AddFileToBatchAsync(
                A<string>._,
                A<Stream>._,
                A<string>.That.IsEqualTo("error_test-job-id.txt"),
                A<string>._,
                A<string>._,
                A<CancellationToken>._))
                .Returns(Result.Success(addFileResponse));

            var result = await _createErrorFileNode.ExecuteAsync(_executionContext);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Succeeded));
        }

        [Test]
        public async Task WhenExecuteAsyncCalledMultipleTimes_ThenEachCallCreatesNewStream()
        {
            var addFileResponse = new AddFileToBatchResponse();
            var streamContents = new List<string>();

            A.CallTo(() => _fileShareClient.AddFileToBatchAsync(
                A<string>._,
                A<Stream>._,
                A<string>._,
                A<string>._,
                A<string>._,
                A<CancellationToken>._))
                .Invokes((string batchId, Stream stream, string fileName, string contentType, string correlationId, CancellationToken ct) =>
                {
                    using var reader = new StreamReader(stream);
                    streamContents.Add(reader.ReadToEnd());
                })
                .Returns(Result.Success(addFileResponse));

            await _createErrorFileNode.ExecuteAsync(_executionContext);
            await _createErrorFileNode.ExecuteAsync(_executionContext);

            Assert.That(streamContents, Has.Count.EqualTo(2));
            Assert.That(streamContents[0], Is.EqualTo(streamContents[1]));
        }

        [Test]
        public async Task WhenExecuteAsyncCalled_ThenCallsAddFileToBatchWithCorrectParameters()
        {
            var addFileResponse = new AddFileToBatchResponse();
            A.CallTo(() => _fileShareClient.AddFileToBatchAsync(
                A<string>._,
                A<Stream>._,
                A<string>._,
                A<string>._,
                A<string>._,
                A<CancellationToken>._))
                .Returns(Result.Success(addFileResponse));

            await _createErrorFileNode.ExecuteAsync(_executionContext);

            A.CallTo(() => _fileShareClient.AddFileToBatchAsync(
                "test-batch-id",
                A<Stream>.That.Not.IsNull(),
                "error.txt",
                ApiHeaderKeys.ContentTypeTextPlain,
                "test-job-id",
                A<CancellationToken>._))
                .MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task WhenExecuteAsyncCalledWithDifferentJobIds_ThenUsesCorrectCorrelationId()
        {
            var differentJob = new Job
            {
                Id = "different-job-id",
                Timestamp = DateTime.UtcNow,
                DataStandard = DataStandard.S100,
                RequestedProducts = "",
                RequestedFilter = "",
                BatchId = "test-batch-id"
            };

            var differentBuild = new S100Build
            {
                JobId = "different-job-id",
                DataStandard = DataStandard.S100,
                BatchId = "test-batch-id"
            };

            var differentPipelineContext = new PipelineContext<S100Build>(differentJob, differentBuild, A.Fake<IStorageService>());
            A.CallTo(() => _executionContext.Subject).Returns(differentPipelineContext);

            var addFileResponse = new AddFileToBatchResponse();

            A.CallTo(() => _fileShareClient.AddFileToBatchAsync(
                A<string>._,
                A<Stream>._,
                A<string>._,
                A<string>._,
                "different-job-id",
                A<CancellationToken>._))
                .Returns(Result.Success(addFileResponse));

            var result = await _createErrorFileNode.ExecuteAsync(_executionContext);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Succeeded));

            A.CallTo(_logger).Where(call => call.Method.Name == "Log" && call.GetArgument<LogLevel>(0) == LogLevel.Error).MustHaveHappenedOnceExactly();
        }
    }
}
