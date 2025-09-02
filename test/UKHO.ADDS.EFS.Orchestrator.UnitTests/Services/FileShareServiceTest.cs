using FakeItEasy;
using Microsoft.Extensions.Logging;
using UKHO.ADDS.Clients.FileShareService.ReadOnly.Models;
using UKHO.ADDS.Clients.FileShareService.ReadWrite;
using UKHO.ADDS.Clients.FileShareService.ReadWrite.Models;
using UKHO.ADDS.Clients.FileShareService.ReadWrite.Models.Response;
using UKHO.ADDS.EFS.Orchestrator.Services.Infrastructure;
using UKHO.ADDS.Infrastructure.Results;

namespace UKHO.ADDS.EFS.Orchestrator.UnitTests.Services
{
    [TestFixture]
    internal class FileShareServiceTest
    {
        private IFileShareReadWriteClient _fakeFileShareReadWriteClient;
        private OrchestratorFileShareClient _fileShareService;
        private ILogger<OrchestratorFileShareClient> _logger;
        private const string CorrelationId = "TestCorrelationId";
        private const string BatchId = "TestBatchId";

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _fakeFileShareReadWriteClient = A.Fake<IFileShareReadWriteClient>();
            _logger = A.Fake<ILogger<OrchestratorFileShareClient>>();
            _fileShareService = new OrchestratorFileShareClient(_fakeFileShareReadWriteClient, _logger);
        }

        [Test]
        
        public void WhenConstructorIsCalledWithNullLogger_ThenThrowsArgumentNullException()
        {
            var mockClient = A.Fake<IFileShareReadWriteClient>();

            Assert.Throws<ArgumentNullException>(() => new OrchestratorFileShareClient(mockClient, null));
        }

        // TODO Rewrite (in unit test uplift PR)

        //[Test]
        //public async Task WhenCreateBatchAsyncIsCalled_ThenReturnsResultFromClient()
        //{
        //    var queueMessage = new JobRequestQueueMessage
        //    {
        //        Version = 1,
        //        Timestamp = DateTime.UtcNow,

        //        CorrelationId = "corr-1",
        //        DataStandard = DataStandard.S100,
        //        Products = "prod",
        //        Filter = "filter"
        //    };
        //    var expectedResult = A.Fake<IResult<IBatchHandle>>();

        //    A.CallTo(() => _fakeFileShareReadWriteClient.CreateBatchAsync(A<BatchModel>._, queueMessage.CorrelationId, CancellationToken.None))
        //        .Returns(Task.FromResult(expectedResult));

        //    var result = await _fileShareService.CreateBatchAsync(queueMessage.CorrelationId, CancellationToken.None);

        //    A.CallTo(() => _fakeFileShareReadWriteClient.CreateBatchAsync(A<BatchModel>._, queueMessage.CorrelationId, CancellationToken.None)).MustHaveHappenedOnceExactly();
        //    Assert.Multiple(() =>
        //    {
        //        Assert.That(result, Is.SameAs(expectedResult));
        //        Assert.That(result, Is.EqualTo(expectedResult));
        //    });
        //}

        [Test]
        public async Task WhenCommitBatchAsyncIsCalled_ThenReturnsResultFromClient()
        {
            var expectedResult = A.Fake<IResult<CommitBatchResponse>>();
            var batchHandle = new BatchHandle(BatchId);
            A.CallTo(() => _fakeFileShareReadWriteClient.CommitBatchAsync(A<BatchHandle>._, CorrelationId, CancellationToken.None))
                .Returns(Task.FromResult(expectedResult));

            var result = await _fileShareService.CommitBatchAsync(batchHandle, CorrelationId, CancellationToken.None);

            A.CallTo(() => _fakeFileShareReadWriteClient.CommitBatchAsync(A<BatchHandle>.That.Matches(b => b.BatchId == BatchId), CorrelationId, CancellationToken.None)).MustHaveHappenedOnceExactly();
            Assert.That(result, Is.SameAs(expectedResult));

        }

        [Test]
        public async Task WhenSearchCommittedBatchesExcludingCurrentAsyncIsCalled_ThenReturnsResultFromClient()
        {
            var expectedResult = A.Fake<IResult<BatchSearchResponse>>();
            var expectedFilter = $"BusinessUnit eq 'ADDS-S100' and $batch(ProductType) eq 'S-100' and $batch(BatchId) ne '{BatchId}'";

            A.CallTo(() => _fakeFileShareReadWriteClient.SearchAsync(expectedFilter, 100, 0, CorrelationId, CancellationToken.None))
                .Returns(Task.FromResult(expectedResult));

            var result = await _fileShareService.SearchCommittedBatchesExcludingCurrentAsync(BatchId, CorrelationId, CancellationToken.None);

            A.CallTo(() => _fakeFileShareReadWriteClient.SearchAsync(expectedFilter, 100, 0, CorrelationId, CancellationToken.None)).MustHaveHappenedOnceExactly();

            Assert.That(result, Is.SameAs(expectedResult));
        }

        [Test]
        public async Task WhenSetExpiryDateAsyncIsCalledWithValidBatches_ThenCallsSetExpiryDateAsyncForEachBatch()
        {
            var batches = CreateBatchDetailsList();

            var expectedResult = A.Fake<IResult<SetExpiryDateResponse>>();

            Fake.ClearRecordedCalls(_fakeFileShareReadWriteClient);

            A.CallTo(() => _fakeFileShareReadWriteClient.SetExpiryDateAsync(A<string>._, A<BatchExpiryModel>._, CorrelationId, CancellationToken.None))
                .Returns(Task.FromResult(expectedResult));

            var result = await _fileShareService.SetExpiryDateAsync(batches, CorrelationId, CancellationToken.None);

            A.CallTo(() => _fakeFileShareReadWriteClient.SetExpiryDateAsync("Batch1", A<BatchExpiryModel>._, CorrelationId, CancellationToken.None)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _fakeFileShareReadWriteClient.SetExpiryDateAsync("Batch2", A<BatchExpiryModel>._, CorrelationId, CancellationToken.None)).MustHaveHappenedOnceExactly();

            Assert.That(result, Is.SameAs(expectedResult));
        }

        [Test]
        public async Task WhenSetExpiryDateAsyncIsCalledWithEmptyList_ThenDoesNotCallSetExpiryDateAsyncAndReturnsSuccess()
        {
            var batches = new List<BatchDetails>();

            Fake.ClearRecordedCalls(_fakeFileShareReadWriteClient);

            var result = await _fileShareService.SetExpiryDateAsync(batches, CorrelationId, CancellationToken.None);

            A.CallTo(() => _fakeFileShareReadWriteClient.SetExpiryDateAsync(A<string>._, A<BatchExpiryModel>._, CorrelationId, CancellationToken.None)).MustNotHaveHappened();
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.IsSuccess(out var _, out var _), Is.True);
            });
        }


        [Test]
        public async Task WhenCreateBatchAsyncFails_ThenIsFailureIsCalled()
        {
            var fakeResult = A.Fake<IResult<IBatchHandle>>();
            IError expectedError = A.Fake<IError>();
            IBatchHandle dummyHandle = null;

            A.CallTo(() => fakeResult.IsFailure(out expectedError, out dummyHandle))
                .Returns(true);

            A.CallTo(() => _fakeFileShareReadWriteClient.CreateBatchAsync(A<BatchModel>._, CorrelationId, CancellationToken.None))
                .Returns(Task.FromResult(fakeResult));

            var result = await _fileShareService.CreateBatchAsync(CorrelationId, CancellationToken.None);

            IError outError;
            var isFailureCalled = result.IsFailure(out outError, out _);

            Assert.Multiple(() =>
            {
                Assert.That(isFailureCalled, Is.True);
                Assert.That(outError, Is.EqualTo(expectedError));
            });
        }

        [Test]
        public async Task WhenCommitBatchAsyncIsFailure_ThenIsFailureIsCalled()
        {
            var fakeResult = A.Fake<IResult<CommitBatchResponse>>();
            IError expectedError = A.Fake<IError>();
            CommitBatchResponse dummyResponse = null;
            var batchHandle = new BatchHandle(BatchId);

            A.CallTo(() => fakeResult.IsFailure(out expectedError, out dummyResponse))
                .Returns(true);

            A.CallTo(() => _fakeFileShareReadWriteClient.CommitBatchAsync(A<BatchHandle>._, CorrelationId, CancellationToken.None))
                .Returns(Task.FromResult(fakeResult));

            var result = await _fileShareService.CommitBatchAsync(batchHandle, CorrelationId, CancellationToken.None);

            IError outError;
            var isFailureCalled = result.IsFailure(out outError, out _);

            Assert.Multiple(() =>
            {
                Assert.That(isFailureCalled, Is.True);
                Assert.That(outError, Is.EqualTo(expectedError));
            });
        }

        [Test]
        public async Task WhenSearchCommittedBatchesExcludingCurrentAsyncIsFailure_ThenIsFailureIsCalledAndLogsError()
        {
            var expectedFilter = $"BusinessUnit eq 'ADDS-S100' and $batch(ProductType) eq 'S-100' and $batch(BatchId) ne '{BatchId}'";
            var fakeResult = A.Fake<IResult<BatchSearchResponse>>();
            IError expectedError = A.Fake<IError>();
            BatchSearchResponse dummyResponse = null;
            
            A.CallTo(() => fakeResult.IsFailure(out expectedError, out dummyResponse))
                .Returns(true);

            A.CallTo(() => _fakeFileShareReadWriteClient.SearchAsync(expectedFilter, 100, 0, CorrelationId, CancellationToken.None))
                .Returns(Task.FromResult(fakeResult));

            var result = await _fileShareService.SearchCommittedBatchesExcludingCurrentAsync(BatchId, CorrelationId, CancellationToken.None);

            IError outError;
            var isFailureCalled = result.IsFailure(out outError, out _);

            Assert.Multiple(() =>
            {
                Assert.That(isFailureCalled, Is.True);
                Assert.That(outError, Is.EqualTo(expectedError));
            });

            A.CallTo(() => _logger.Log(A<LogLevel>._, A<EventId>._, A<LoggerMessageState>._, A<Exception>._, A<Func<LoggerMessageState, Exception?, string>>._)).MustHaveHappened();
        }

        [Test]

        public async Task WhenCreateBatchAsyncFailsAndFileShareServiceError_ThenLogsError()
        {
            var fakeResult = A.Fake<IResult<IBatchHandle>>();
            var fakeError = A.Fake<IError>();
            IBatchHandle dummyHandle = null;

            A.CallTo(() => fakeResult.IsFailure(out fakeError, out dummyHandle)).Returns(true);
            A.CallTo(() => _fakeFileShareReadWriteClient.CreateBatchAsync(A<BatchModel>._, CorrelationId, CancellationToken.None))
                .Returns(Task.FromResult(fakeResult));

            await _fileShareService.CreateBatchAsync(CorrelationId, CancellationToken.None);

            A.CallTo(() => _logger.Log(A<LogLevel>._, A<EventId>._, A<LoggerMessageState>._, A<Exception>._,A<Func<LoggerMessageState, Exception?, string>>._)).MustHaveHappened();
        }

        [Test]
        public async Task WhenCommitBatchAsyncIsFailure_ThenLogsError()
        {
            var fakeResult = A.Fake<IResult<CommitBatchResponse>>();
            var fakeError = A.Fake<IError>();
            CommitBatchResponse dummyResponse = null;
            var batchHandle = new BatchHandle(BatchId);

            A.CallTo(() => fakeResult.IsFailure(out fakeError, out dummyResponse)).Returns(true);
            A.CallTo(() => _fakeFileShareReadWriteClient.CommitBatchAsync(A<BatchHandle>._, CorrelationId, CancellationToken.None))
                .Returns(Task.FromResult(fakeResult));

            await _fileShareService.CommitBatchAsync(batchHandle, CorrelationId, CancellationToken.None);

            A.CallTo(() => _logger.Log(A<LogLevel>._, A<EventId>._, A<LoggerMessageState>._, A<Exception>._, A<Func<LoggerMessageState, Exception?, string>>._)).MustHaveHappened();
        }

        [Test]
        public async Task WhenSearchCommittedBatchesExcludingCurrentAsyncIsFailure_ThenLogsError()
        {
            var fakeResult = A.Fake<IResult<BatchSearchResponse>>();
            var fakeError = A.Fake<IError>();
            BatchSearchResponse dummyResponse = null;

            A.CallTo(() => fakeResult.IsFailure(out fakeError, out dummyResponse)).Returns(true);
            A.CallTo(() => _fakeFileShareReadWriteClient.SearchAsync(A<string>._, 100, 0, CorrelationId))
                .Returns(Task.FromResult(fakeResult));

            await _fileShareService.SearchCommittedBatchesExcludingCurrentAsync(BatchId, CorrelationId, CancellationToken.None);

            A.CallTo(() => _logger.Log(A<LogLevel>._, A<EventId>._, A<LoggerMessageState>._, A<Exception>._,A<Func<LoggerMessageState, Exception?, string>>._)).MustHaveHappened();
        }

        [Test]
        public async Task WhenSetExpiryDateAsyncFails_ThenLogsErrorAndReturnsFailure()
        {
            var batches = CreateBatchDetailsList();
            var fakeResult = A.Fake<IResult<SetExpiryDateResponse>>();
            var fakeError = A.Fake<IError>();
            SetExpiryDateResponse dummyResponse = null;

            A.CallTo(() => fakeResult.IsFailure(out fakeError, out dummyResponse)).Returns(true);

            A.CallTo(() => _fakeFileShareReadWriteClient.SetExpiryDateAsync("Batch1", A<BatchExpiryModel>._, CorrelationId, CancellationToken.None))
                .Returns(Task.FromResult(fakeResult));

            var result = await _fileShareService.SetExpiryDateAsync(batches, CorrelationId, CancellationToken.None);

            A.CallTo(() => _logger.Log(A<LogLevel>._, A<EventId>._, A<LoggerMessageState>._, A<Exception>._,A<Func<LoggerMessageState, Exception?, string>>._)).MustHaveHappened();
            Assert.That(result, Is.SameAs(fakeResult));
        }

        [Test]
        public async Task WhenAddFileToBatchAsyncIsCalled_ThenReturnsResultFromClient()
        {
            var expectedResult = A.Fake<IResult<AddFileToBatchResponse>>();
            var fileStream = new MemoryStream();
            const string fileName = "test.txt";
            const string contentType = "text/plain";

            A.CallTo(() => _fakeFileShareReadWriteClient.AddFileToBatchAsync(
                A<BatchHandle>.That.Matches(b => b.BatchId == BatchId),
                fileStream,
                fileName,
                contentType,
                CorrelationId,
                CancellationToken.None))
                .Returns(Task.FromResult(expectedResult));

            var result = await _fileShareService.AddFileToBatchAsync(BatchId, fileStream, fileName, contentType, CorrelationId, CancellationToken.None);

            A.CallTo(() => _fakeFileShareReadWriteClient.AddFileToBatchAsync(
                A<BatchHandle>.That.Matches(b => b.BatchId == BatchId),
                fileStream,
                fileName,
                contentType,
                CorrelationId,
                CancellationToken.None)).MustHaveHappenedOnceExactly();

            Assert.That(result, Is.SameAs(expectedResult));
        }

        [Test]
        public async Task WhenAddFileToBatchAsyncFails_ThenLogsErrorAndReturnsFailure()
        {
            var fakeResult = A.Fake<IResult<AddFileToBatchResponse>>();
            var fakeError = A.Fake<IError>();
            AddFileToBatchResponse dummyResponse = null;
            var fileStream = new MemoryStream();

            A.CallTo(() => fakeResult.IsFailure(out fakeError, out dummyResponse)).Returns(true);
            A.CallTo(() => _fakeFileShareReadWriteClient.AddFileToBatchAsync(
                A<BatchHandle>._,
                A<Stream>._,
                A<string>._,
                A<string>._,
                CorrelationId,
                CancellationToken.None))
                .Returns(Task.FromResult(fakeResult));

            var result = await _fileShareService.AddFileToBatchAsync(BatchId, fileStream, "test.txt", "text/plain", CorrelationId, CancellationToken.None);

            A.CallTo(() => _logger.Log(A<LogLevel>._, A<EventId>._, A<LoggerMessageState>._, A<Exception>._,A<Func<LoggerMessageState, Exception?, string>>._)).MustHaveHappened();
            Assert.That(result, Is.SameAs(fakeResult));
        }
        private List<BatchDetails> CreateBatchDetailsList()
        {
            return new List<BatchDetails>
            {
                new() { BatchId = "Batch1" },
                new() { BatchId = "Batch2" }
            };
        }
    }
}
