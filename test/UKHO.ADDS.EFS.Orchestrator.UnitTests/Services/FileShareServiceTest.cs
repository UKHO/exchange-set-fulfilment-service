using FakeItEasy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UKHO.ADDS.Clients.FileShareService.ReadOnly.Models;
using UKHO.ADDS.Clients.FileShareService.ReadWrite;
using UKHO.ADDS.Clients.FileShareService.ReadWrite.Models;
using UKHO.ADDS.Clients.FileShareService.ReadWrite.Models.Response;
using UKHO.ADDS.EFS.Messages;
using UKHO.ADDS.EFS.Orchestrator.Services;
using UKHO.ADDS.EFS.RetryPolicy;
using UKHO.ADDS.Infrastructure.Results;
using Error = UKHO.ADDS.Infrastructure.Results.Error;

namespace UKHO.ADDS.EFS.Orchestrator.UnitTests.Services
{
    [TestFixture]
    internal class FileShareServiceTest
    {
        private IFileShareReadWriteClient _fakeFileShareReadWriteClient;
        private FileShareService _fileShareService;
        private ILogger<FileShareService> _logger;
        private IConfiguration _configuration;

        private const string CorrelationId = "TestCorrelationId";
        private const string BatchId = "TestBatchId";
        private const int RetryDelayInMilliseconds = 100;

        [SetUp]
        public void SetUp()
        {
            _fakeFileShareReadWriteClient = A.Fake<IFileShareReadWriteClient>();
            _logger = A.Fake<ILogger<FileShareService>>();
            _configuration = A.Fake<IConfiguration>();

            A.CallTo(() => _configuration["HttpRetry:RetryDelayInMilliseconds"]).Returns(RetryDelayInMilliseconds.ToString());
            HttpRetryPolicyFactory.SetConfiguration(_configuration);

            _fileShareService = new FileShareService(_fakeFileShareReadWriteClient, _logger);
        }

        [Test]
        public void WhenConstructorIsCalledWithNullLogger_ThenThrowsArgumentNullException()
        {
            var mockClient = A.Fake<IFileShareReadWriteClient>();

            Assert.Throws<ArgumentNullException>(() => new FileShareService(mockClient, null));
        }

        [Test]
        public async Task WhenCreateBatchAsyncIsCalled_ThenReturnsResultFromClient()
        {
            var queueMessage = new ExchangeSetRequestQueueMessage
            {
                CorrelationId = "corr-1",
                DataStandard = ExchangeSetDataStandard.S100,
                Products = "prod"
            };
            var expectedResult = A.Fake<IResult<IBatchHandle>>();

            A.CallTo(() => _fakeFileShareReadWriteClient.CreateBatchAsync(A<BatchModel>._, queueMessage.CorrelationId, CancellationToken.None))
                .Returns(Task.FromResult(expectedResult));

            var result = await _fileShareService.CreateBatchAsync(queueMessage.CorrelationId, CancellationToken.None);

            A.CallTo(() => _fakeFileShareReadWriteClient.CreateBatchAsync(A<BatchModel>._, queueMessage.CorrelationId, CancellationToken.None)).MustHaveHappenedOnceExactly();
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.SameAs(expectedResult));
                Assert.That(result, Is.EqualTo(expectedResult));
            });
        }

        [Test]
        public async Task WhenCommitBatchAsyncIsCalled_ThenReturnsResultFromClient()
        {
            var expectedResult = A.Fake<IResult<CommitBatchResponse>>();
            A.CallTo(() => _fakeFileShareReadWriteClient.CommitBatchAsync(A<BatchHandle>._, CorrelationId, CancellationToken.None))
                .Returns(Task.FromResult(expectedResult));

            var result = await _fileShareService.CommitBatchAsync(BatchId, CorrelationId, CancellationToken.None);

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

            A.CallTo(() => fakeResult.IsFailure(out expectedError, out dummyResponse))
                .Returns(true);

            A.CallTo(() => _fakeFileShareReadWriteClient.CommitBatchAsync(A<BatchHandle>._, CorrelationId, CancellationToken.None))
                .Returns(Task.FromResult(fakeResult));

            var result = await _fileShareService.CommitBatchAsync(BatchId, CorrelationId, CancellationToken.None);

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

            A.CallTo(() => _logger.Log(A<LogLevel>._, A<EventId>._, A<LoggerMessageState>._, A<Exception>._, A<Func<LoggerMessageState, Exception?, string>>._)).MustHaveHappened();
        }

        [Test]
        public async Task WhenCommitBatchAsyncIsFailure_ThenLogsError()
        {
            var fakeResult = A.Fake<IResult<CommitBatchResponse>>();
            var fakeError = A.Fake<IError>();
            CommitBatchResponse dummyResponse = null;

            A.CallTo(() => fakeResult.IsFailure(out fakeError, out dummyResponse)).Returns(true);
            A.CallTo(() => _fakeFileShareReadWriteClient.CommitBatchAsync(A<BatchHandle>._, CorrelationId, CancellationToken.None))
                .Returns(Task.FromResult(fakeResult));

            await _fileShareService.CommitBatchAsync(BatchId, CorrelationId, CancellationToken.None);

            A.CallTo(() => _logger.Log(A<LogLevel>._, A<EventId>._, A<LoggerMessageState>._, A<Exception>._, A<Func<LoggerMessageState, Exception?, string>>._)).MustHaveHappened();
        }

        [Test]
        public async Task WhenSearchCommittedBatchesExcludingCurrentAsyncIsFailure_ThenLogsError()
        {
            var fakeResult = A.Fake<IResult<BatchSearchResponse>>();
            var fakeError = A.Fake<IError>();
            BatchSearchResponse dummyResponse = null;

            A.CallTo(() => fakeResult.IsFailure(out fakeError, out dummyResponse)).Returns(true);
            A.CallTo(() => _fakeFileShareReadWriteClient.SearchAsync(A<string>._, 100, 0, CorrelationId, CancellationToken.None))
                .Returns(Task.FromResult(fakeResult));

            await _fileShareService.SearchCommittedBatchesExcludingCurrentAsync(BatchId, CorrelationId, CancellationToken.None);

            A.CallTo(() => _logger.Log(A<LogLevel>._, A<EventId>._, A<LoggerMessageState>._, A<Exception>._, A<Func<LoggerMessageState, Exception?, string>>._)).MustHaveHappened();
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

            A.CallTo(() => _logger.Log(A<LogLevel>._, A<EventId>._, A<LoggerMessageState>._, A<Exception>._, A<Func<LoggerMessageState, Exception?, string>>._)).MustHaveHappened();
            Assert.That(result, Is.SameAs(fakeResult));
        }

        [Test]
        public async Task WhenCreateBatchAsyncFailsWithRetriableStatusCode_ThenRetriesExpectedNumberOfTimes()
        {
            int callCount = 0;
            var startTime = DateTime.UtcNow;

            A.CallTo(() => _fakeFileShareReadWriteClient.CreateBatchAsync(A<BatchModel>._, CorrelationId, CancellationToken.None))
                .ReturnsLazily(() =>
                {
                    callCount++;
                    return Task.FromResult<IResult<IBatchHandle>>(
                        Result.Failure<IBatchHandle>(new Error("Retriable error", new Dictionary<string, object> { { "StatusCode", 503 } }))
                    );
                });

            var result = await _fileShareService.CreateBatchAsync(CorrelationId, CancellationToken.None);
            var elapsedMilliseconds = (DateTime.UtcNow - startTime).TotalMilliseconds;

            Assert.Multiple(() =>
            {
                Assert.That(callCount, Is.EqualTo(4), "Should retry 3 times plus the initial call (total 4)");
                Assert.That(result.IsFailure(out _, out _), Is.True);
                Assert.That(elapsedMilliseconds, Is.GreaterThan(RetryDelayInMilliseconds),
                    $"Retry delay should reflect TestRetryDelayMs of {RetryDelayInMilliseconds}ms");
            });
        }

        [Test]
        public async Task WhenCreateBatchAsyncFailsWithNonRetriableStatusCode_ThenDoesNotRetry()
        {
            int callCount = 0;
            var startTime = DateTime.UtcNow;

            A.CallTo(() => _fakeFileShareReadWriteClient.CreateBatchAsync(A<BatchModel>._, CorrelationId, CancellationToken.None))
                .ReturnsLazily(() =>
                {
                    callCount++;
                    return Task.FromResult<IResult<IBatchHandle>>(
                        Result.Failure<IBatchHandle>(new Error("Non-retriable error", new Dictionary<string, object> { { "StatusCode", 400 } }))
                    );
                });

            var result = await _fileShareService.CreateBatchAsync(CorrelationId, CancellationToken.None);
            var elapsedMilliseconds = (DateTime.UtcNow - startTime).TotalMilliseconds;

            Assert.Multiple(() =>
            {
                Assert.That(callCount, Is.EqualTo(1), "Should not retry for non-retriable status codes");
                Assert.That(result.IsFailure(out _, out _), Is.True);
                Assert.That(elapsedMilliseconds, Is.LessThan(RetryDelayInMilliseconds),
                    "Non-retryable errors should complete quickly without delay");
            });
        }

        [Test]
        public async Task WhenCommitBatchAsyncFailsWithRetriableStatusCode_ThenRetriesExpectedNumberOfTimes()
        {
            int callCount = 0;
            var startTime = DateTime.UtcNow;

            A.CallTo(() => _fakeFileShareReadWriteClient.CommitBatchAsync(A<BatchHandle>._, CorrelationId, CancellationToken.None))
                .ReturnsLazily(() =>
                {
                    callCount++;
                    return Task.FromResult<IResult<CommitBatchResponse>>(
                        Result.Failure<CommitBatchResponse>(new Error("Retriable error", new Dictionary<string, object> { { "StatusCode", 503 } }))
                    );
                });

            var result = await _fileShareService.CommitBatchAsync(BatchId, CorrelationId, CancellationToken.None);
            var elapsedMilliseconds = (DateTime.UtcNow - startTime).TotalMilliseconds;

            Assert.Multiple(() =>
            {
                Assert.That(callCount, Is.EqualTo(4), "Should retry 3 times plus the initial call (total 4)");
                Assert.That(result.IsFailure(out _, out _), Is.True);
                Assert.That(elapsedMilliseconds, Is.GreaterThan(RetryDelayInMilliseconds),
                    $"Retry delay should reflect TestRetryDelayMs of {RetryDelayInMilliseconds}ms");
            });
        }

        [Test]
        public async Task WhenSearchCommittedBatchesExcludingCurrentAsyncFailsWithRetriableStatusCode_ThenRetriesExpectedNumberOfTimes()
        {
            int callCount = 0;
            var startTime = DateTime.UtcNow;

            A.CallTo(() => _fakeFileShareReadWriteClient.SearchAsync(A<string>._, A<int>._, A<int>._, CorrelationId, CancellationToken.None))
                .ReturnsLazily(() =>
                {
                    callCount++;
                    return Task.FromResult<IResult<BatchSearchResponse>>(
                        Result.Failure<BatchSearchResponse>(new Error("Retriable error", new Dictionary<string, object> { { "StatusCode", 503 } }))
                    );
                });

            var result = await _fileShareService.SearchCommittedBatchesExcludingCurrentAsync(BatchId, CorrelationId, CancellationToken.None);
            var elapsedMilliseconds = (DateTime.UtcNow - startTime).TotalMilliseconds;

            Assert.Multiple(() =>
            {
                Assert.That(callCount, Is.EqualTo(4), "Should retry 3 times plus the initial call (total 4)");
                Assert.That(result.IsFailure(out _, out _), Is.True);
                Assert.That(elapsedMilliseconds, Is.GreaterThan(RetryDelayInMilliseconds),
                    $"Retry delay should reflect TestRetryDelayMs of {RetryDelayInMilliseconds}ms");
            });
        }

        [Test]
        public async Task WhenSetExpiryDateAsyncFailsWithRetriableStatusCode_ThenRetriesExpectedNumberOfTimes()
        {
            int callCount = 0;
            var startTime = DateTime.UtcNow;
            var batches = CreateBatchDetailsList();

            A.CallTo(() => _fakeFileShareReadWriteClient.SetExpiryDateAsync(A<string>._, A<BatchExpiryModel>._, CorrelationId, CancellationToken.None))
                .ReturnsLazily(() =>
                {
                    callCount++;
                    return Task.FromResult<IResult<SetExpiryDateResponse>>(
                        Result.Failure<SetExpiryDateResponse>(new Error("Retriable error", new Dictionary<string, object> { { "StatusCode", 503 } }))
                    );
                });

            var result = await _fileShareService.SetExpiryDateAsync(batches, CorrelationId, CancellationToken.None);
            var elapsedMilliseconds = (DateTime.UtcNow - startTime).TotalMilliseconds;

            Assert.Multiple(() =>
            {
                Assert.That(callCount, Is.EqualTo(4), "Should retry 3 times plus the initial call (total 4)");
                Assert.That(result.IsFailure(out _, out _), Is.True);
                Assert.That(elapsedMilliseconds, Is.GreaterThan(RetryDelayInMilliseconds),
                    $"Retry delay should reflect TestRetryDelayMs of {RetryDelayInMilliseconds}ms");
            });
        }

        [Test]
        public async Task WhenSetExpiryDateAsyncFailsWithRetriableStatusCodeForSecondBatch_ThenRetriesExpectedNumberOfTimesAndStops()
        {
            int callCount1 = 0;
            int callCount2 = 0;
            var startTime = DateTime.UtcNow;
            var batches = CreateBatchDetailsList();

            A.CallTo(() => _fakeFileShareReadWriteClient.SetExpiryDateAsync("Batch1", A<BatchExpiryModel>._, CorrelationId, CancellationToken.None))
                .ReturnsLazily(() =>
                {
                    callCount1++;
                    return Task.FromResult<IResult<SetExpiryDateResponse>>(
                        Result.Success(new SetExpiryDateResponse())
                    );
                });

            A.CallTo(() => _fakeFileShareReadWriteClient.SetExpiryDateAsync("Batch2", A<BatchExpiryModel>._, CorrelationId, CancellationToken.None))
                .ReturnsLazily(() =>
                {
                    callCount2++;
                    return Task.FromResult<IResult<SetExpiryDateResponse>>(
                        Result.Failure<SetExpiryDateResponse>(new Error("Retriable error", new Dictionary<string, object> { { "StatusCode", 503 } }))
                    );
                });

            var result = await _fileShareService.SetExpiryDateAsync(batches, CorrelationId, CancellationToken.None);
            var elapsedMilliseconds = (DateTime.UtcNow - startTime).TotalMilliseconds;

            Assert.Multiple(() =>
            {
                Assert.That(callCount1, Is.EqualTo(1), "First batch should succeed without retries");
                Assert.That(callCount2, Is.EqualTo(4), "Second batch should retry 3 times plus the initial call (total 4)");
                Assert.That(result.IsFailure(out _, out _), Is.True);
                Assert.That(elapsedMilliseconds, Is.GreaterThan(RetryDelayInMilliseconds),
                    $"Retry delay should reflect TestRetryDelayMs of {RetryDelayInMilliseconds}ms");
            });
        }

        private List<BatchDetails> CreateBatchDetailsList()
        {
            return new List<BatchDetails>
            {
                new() { BatchId = "Batch1" },
                new() { BatchId = "Batch2" }
            };
        }

        [TearDown]
        public void TearDown()
        {
            HttpRetryPolicyFactory.SetConfiguration(null);
        }
    }
}
