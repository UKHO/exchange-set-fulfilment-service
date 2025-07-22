using System.IO.Compression;
using FakeItEasy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UKHO.ADDS.Clients.FileShareService.ReadWrite.Models.Response;
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
        private readonly CancellationToken _cancellationToken = CancellationToken.None;

        [SetUp]
        public void SetUp()
        {
            _fileShareClient = A.Fake<IOrchestratorFileShareClient>();
            _logger = A.Fake<ILogger<CreateErrorFileNode>>();
            var configuration = A.Fake<IConfiguration>();
            var environmentLogger = A.Fake<ILogger>();

            _nodeEnvironment = new CompletionNodeEnvironment(configuration, _cancellationToken, environmentLogger, BuilderExitCode.Failed);
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
            Assert.Throws<ArgumentNullException>(() => new CreateErrorFileNode(_nodeEnvironment, null, _logger));
        }

        [Test]
        public void WhenLoggerIsNull_ThenThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new CreateErrorFileNode(_nodeEnvironment, _fileShareClient, null));
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
            var successEnvironment = new CompletionNodeEnvironment(A.Fake<IConfiguration>(), _cancellationToken, A.Fake<ILogger>(), BuilderExitCode.Success);
            var node = new CreateErrorFileNode(successEnvironment, _fileShareClient, _logger);

            var shouldExecute = await node.ShouldExecuteAsync(_executionContext);

            Assert.That(shouldExecute, Is.False);
        }

        [Test]
        public async Task WhenShouldExecuteAsyncCalledWithNoBatchId_ThenReturnsFalse()
        {
            _pipelineContext.Job.BatchId = null;

            var shouldExecute = await _createErrorFileNode.ShouldExecuteAsync(_executionContext);

            Assert.That(shouldExecute, Is.False);
        }

        [Test]
        public async Task WhenPerformExecuteAsyncCalledAndAddFileSucceeds_ThenReturnsSucceeded()
        {
            var addFileResponse = new AddFileToBatchResponse();
            A.CallTo(() => _fileShareClient.AddFileToBatchAsync(
                A<string>.That.IsEqualTo("test-batch-id"),
                A<Stream>._,
                A<string>.That.IsEqualTo("V01X01_test-job-id.zip"),
                A<string>.That.IsEqualTo(ApiHeaderKeys.ContentTypeOctetStream),
                A<string>.That.IsEqualTo("test-job-id"),
                A<CancellationToken>._))
                .Returns(Result.Success(addFileResponse));

            var result = await _createErrorFileNode.ExecuteAsync(_executionContext);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Succeeded));
        }

        [Test]
        public async Task WhenPerformExecuteAsyncCalledAndAddFileFails_ThenReturnsFailed()
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
        }

        [Test]
        public async Task WhenPerformExecuteAsyncCalled_ThenCreatesZipFileWithCorrectErrorMessage()
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

            // Verify it's a valid zip file and contains error.txt with correct content
            capturedStream.Position = 0;
            using var archive = new ZipArchive(capturedStream, ZipArchiveMode.Read);

            var errorEntry = archive.GetEntry("error.txt");
            Assert.That(errorEntry, Is.Not.Null, "error.txt should exist in the zip file");

            using var entryStream = errorEntry.Open();
            using var reader = new StreamReader(entryStream);
            var content = await reader.ReadToEndAsync();

            var expectedMessage = "There has been a problem in creating your exchange set, so we are unable to fulfil your request at this time. Please contact UKHO Customer Services quoting correlation ID test-job-id";
            Assert.That(content, Is.EqualTo(expectedMessage));
        }

        [Test]
        public async Task WhenPerformExecuteAsyncCalled_ThenUploadsCorrectZipFileName()
        {
            var addFileResponse = new AddFileToBatchResponse();
            string capturedFileName = null;

            A.CallTo(() => _fileShareClient.AddFileToBatchAsync(
                A<string>._,
                A<Stream>._,
                A<string>._,
                A<string>._,
                A<string>._,
                A<CancellationToken>._))
                .Invokes((string batchId, Stream stream, string fileName, string contentType, string correlationId, CancellationToken ct) =>
                {
                    capturedFileName = fileName;
                })
                .Returns(Result.Success(addFileResponse));

            await _createErrorFileNode.ExecuteAsync(_executionContext);

            Assert.That(capturedFileName, Is.EqualTo("V01X01_test-job-id.zip"));
        }

        [Test]
        public async Task WhenPerformExecuteAsyncCalledAndExceptionThrown_ThenReturnsFailed()
        {
            A.CallTo(() => _fileShareClient.AddFileToBatchAsync(
                A<string>._,
                A<Stream>._,
                A<string>._,
                A<string>._,
                A<string>._,
                A<CancellationToken>._))
                .Throws(new Exception("Unexpected error"));

            var result = await _createErrorFileNode.ExecuteAsync(_executionContext);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Failed));
        }
    }
}
