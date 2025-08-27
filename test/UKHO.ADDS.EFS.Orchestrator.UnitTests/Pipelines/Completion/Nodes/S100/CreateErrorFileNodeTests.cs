using System.Text;
using FakeItEasy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UKHO.ADDS.Clients.FileShareService.ReadWrite.Models.Response;
using UKHO.ADDS.EFS.Builds.S100;
using UKHO.ADDS.EFS.Configuration.Orchestrator;
using UKHO.ADDS.EFS.Constants;
using UKHO.ADDS.EFS.Exceptions;
using UKHO.ADDS.EFS.Jobs;
using UKHO.ADDS.EFS.Orchestrator.Jobs;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Completion.Nodes.S100;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Completion;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Services;
using UKHO.ADDS.EFS.Orchestrator.Services.Infrastructure;
using UKHO.ADDS.EFS.VOS;
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
        private const string S100ErrorFileNameTemplate = "orchestrator:Errors:FileNameTemplate";

        private static BatchId TestBatchId = BatchId.From("test-batch-id");
        private static JobId TestJobId = JobId.From("test-job-id");

        [SetUp]
        public void SetUp()
        {
            _fileShareClient = A.Fake<IOrchestratorFileShareClient>();
            _logger = A.Fake<ILogger<CreateErrorFileNode>>();
            _configuration = A.Fake<IConfiguration>();
            var environmentLogger = A.Fake<ILogger>();

            A.CallTo(() => _configuration[S100ErrorFileNameTemplate]).Returns("error.txt");

            _nodeEnvironment = new CompletionNodeEnvironment(_configuration, _cancellationToken, environmentLogger, BuilderExitCode.Failed);
            _createErrorFileNode = new CreateErrorFileNode(_nodeEnvironment, _fileShareClient, _logger);

            var job = new Job
            {
                Id = TestJobId,
                Timestamp = DateTime.UtcNow,
                DataStandard = DataStandard.S100,
                RequestedProducts = "",
                RequestedFilter = "",
                BatchId = TestBatchId
            };

            var build = new S100Build
            {
                JobId = TestJobId,
                DataStandard = DataStandard.S100,
                BatchId = TestBatchId
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
        public async Task WhenExecuteAsyncCalledWithFailedBuilderExitCodeAndBatchId_ThenReturnsTrue()
        {
            var result = await _createErrorFileNode.ShouldExecuteAsync(_executionContext);

            Assert.That(result, Is.True);
        }

        [Test]
        public async Task WhenShouldExecuteAsyncCalledWithSuccessBuilderExitCode_ThenReturnsFalse()
        {
            var successEnvironment = new CompletionNodeEnvironment(_configuration, _cancellationToken, A.Fake<ILogger>(), BuilderExitCode.Success);
            var node = new CreateErrorFileNode(successEnvironment, _fileShareClient, _logger);

            var result = await node.ShouldExecuteAsync(_executionContext);

            Assert.That(result, Is.False);
        }

        [Test]
        public async Task WhenShouldExecuteAsyncCalledWithNotRunBuilderExitCode_ThenReturnsFalse()
        {
            var notRunEnvironment = new CompletionNodeEnvironment(_configuration, _cancellationToken, A.Fake<ILogger>(), BuilderExitCode.NotRun);
            var node = new CreateErrorFileNode(notRunEnvironment, _fileShareClient, _logger);

            var result = await node.ShouldExecuteAsync(_executionContext);

            Assert.That(result, Is.False);
        }

        [Test]
        public async Task WhenShouldExecuteAsyncCalledWithBatchIdNone_ThenReturnsFalse()
        {
            _pipelineContext.Job.BatchId = BatchId.None;

            var result = await _createErrorFileNode.ShouldExecuteAsync(_executionContext);

            Assert.That(result, Is.False);
        }

        [Test]
        public async Task WhenShouldExecuteAsyncCalledWithWhitespaceBatchId_ThenReturnsFalse()
        {
            Assert.Throws<ValidationException>(CreateWhitespaceBatchId);
        }

        private void CreateWhitespaceBatchId()
        {
            _pipelineContext.Job.BatchId = BatchId.From("   ");
        }

        [Test]
        public async Task WhenExecuteAsyncCalledAndAddFileSucceeds_ThenReturnsSucceededAndLogsSuccess()
        {
            var addFileResponse = new AddFileToBatchResponse();
            A.CallTo(() => _fileShareClient.AddFileToBatchAsync(
                A<string>.That.IsEqualTo((string)TestBatchId),
                A<Stream>._,
                A<string>.That.IsEqualTo("error.txt"),
                A<string>.That.IsEqualTo(ApiHeaderKeys.ContentTypeTextPlain),
                A<string>.That.IsEqualTo((string)TestJobId),
                A<CancellationToken>._))
                .Returns(Result.Success(addFileResponse));

            var result = await _createErrorFileNode.ExecuteAsync(_executionContext);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Succeeded));

            A.CallTo(_logger).Where(call => call.Method.Name == "Log" && call.GetArgument<LogLevel>(0) == LogLevel.Error).MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task WhenExecuteAsyncCalledAndAddFileSucceeds_ErrorFileCreatedIsTrue()
        {
            var addFileResponse = new AddFileToBatchResponse();
            A.CallTo(() => _fileShareClient.AddFileToBatchAsync(
                A<string>.That.IsEqualTo((string)TestBatchId),
                A<Stream>._,
                A<string>.That.IsEqualTo("error.txt"),
                A<string>.That.IsEqualTo(ApiHeaderKeys.ContentTypeTextPlain),
                A<string>.That.IsEqualTo((string)TestJobId),
                A<CancellationToken>._))
                .Returns(Result.Success(addFileResponse));

            var result = await _createErrorFileNode.ExecuteAsync(_executionContext);

            Assert.That(_pipelineContext.IsErrorFileCreated, Is.True);
        }

        [Test]
        public async Task WhenExecuteAsyncCalledAndAddFileFails_ErrorFileCreatedIsFalse()
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

            Assert.That(_pipelineContext.IsErrorFileCreated, Is.False);
        }

        [Test]
        public async Task WhenExecuteAsyncCalledWithJobIdPlaceholderInTemplate_ThenReplacesJobIdInFileName()
        {
            A.CallTo(() => _configuration[S100ErrorFileNameTemplate]).Returns("error_[jobid].txt");
            var addFileResponse = new AddFileToBatchResponse();

            A.CallTo(() => _fileShareClient.AddFileToBatchAsync(
                A<string>.That.IsEqualTo((string)TestBatchId),
                A<Stream>._,
                A<string>.That.IsEqualTo("error_test-job-id.txt"),
                A<string>.That.IsEqualTo(ApiHeaderKeys.ContentTypeTextPlain),
                A<string>.That.IsEqualTo((string)TestJobId),
                A<CancellationToken>._))
                .Returns(Result.Success(addFileResponse));

            var result = await _createErrorFileNode.ExecuteAsync(_executionContext);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Succeeded));
        }

        [Test]
        public async Task WhenExecuteAsyncCalledWithNoJobIdPlaceholder_ThenUsesTemplateAsIs()
        {
            A.CallTo(() => _configuration[S100ErrorFileNameTemplate]).Returns("error.txt");
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
        public async Task WhenExecuteAsyncCalledWithEmptyErrorFileNameTemplate_ThenUsesEmptyFileName()
        {
            A.CallTo(() => _configuration[S100ErrorFileNameTemplate]).Returns(string.Empty);
            var addFileResponse = new AddFileToBatchResponse();

            A.CallTo(() => _fileShareClient.AddFileToBatchAsync(
                A<string>._,
                A<Stream>._,
                A<string>.That.IsEqualTo(string.Empty),
                A<string>._,
                A<string>._,
                A<CancellationToken>._))
                .Returns(Result.Success(addFileResponse));

            var result = await _createErrorFileNode.ExecuteAsync(_executionContext);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Succeeded));
        }

        [Test]
        public async Task WhenExecuteAsyncCalled_ThenErrorFileContainsCorrectMessage()
        {
            var capturedStream = new MemoryStream();
            var addFileResponse = new AddFileToBatchResponse();

            A.CallTo(() => _configuration["orchestrator:Errors:Message"])
                .Returns("There has been a problem in creating your exchange set, so we are unable to fulfill your request at this time. Please contact UKHO Customer Services quoting correlation ID [jobid]");

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
            var expectedMessage = "There has been a problem in creating your exchange set, so we are unable to fulfill your request at this time. Please contact UKHO Customer Services quoting correlation ID test-job-id";

            Assert.That(content, Is.EqualTo(expectedMessage));
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
                (string)TestBatchId,
                A<Stream>.That.Not.IsNull(),
                "error.txt",
                ApiHeaderKeys.ContentTypeTextPlain,
                (string)TestJobId,
                A<CancellationToken>._))
                .MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task WhenExecuteAsyncCalledWithStreamDisposal_ThenStreamIsProperlyDisposed()
        {
            var addFileResponse = new AddFileToBatchResponse();
            var streamWasDisposed = false;

            A.CallTo(() => _fileShareClient.AddFileToBatchAsync(
                A<string>._,
                A<Stream>._,
                A<string>._,
                A<string>._,
                A<string>._,
                A<CancellationToken>._))
                .Invokes((string batchId, Stream stream, string fileName, string contentType, string correlationId, CancellationToken ct) =>
                {
                    Task.Run(async () =>
                    {
                        await Task.Delay(100);
                        try
                        {
                            _ = stream.ReadByte();
                        }
                        catch (ObjectDisposedException)
                        {
                            streamWasDisposed = true;
                        }
                    });
                })
                .Returns(Result.Success(addFileResponse));

            var result = await _createErrorFileNode.ExecuteAsync(_executionContext);

            await Task.Delay(200);
            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Succeeded));
            Assert.That(streamWasDisposed, Is.True, "Stream should be disposed after use");
        }
    }
}
