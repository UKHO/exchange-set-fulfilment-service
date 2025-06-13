using System.Linq.Expressions;
using System.Net;
using Azure.Data.Tables;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using UKHO.ADDS.Clients.FileShareService.ReadOnly.Models;
using UKHO.ADDS.Clients.FileShareService.ReadWrite.Models;
using UKHO.ADDS.Clients.FileShareService.ReadWrite.Models.Response;
using UKHO.ADDS.Clients.SalesCatalogueService.Models;
using UKHO.ADDS.EFS.Configuration.Orchestrator;
using UKHO.ADDS.EFS.Entities;
using UKHO.ADDS.EFS.Messages;
using UKHO.ADDS.EFS.Orchestrator.Services;
using UKHO.ADDS.EFS.Orchestrator.Tables;
using UKHO.ADDS.EFS.Orchestrator.Tables.Infrastructure;
using UKHO.ADDS.EFS.Orchestrator.UnitTests.Extensions;
using UKHO.ADDS.Infrastructure.Results;

namespace UKHO.ADDS.EFS.Orchestrator.UnitTests.Services
{
    [TestFixture]
    public class JobServiceTest
    {
        private JobService _jobService;
        private ExchangeSetJobTable _jobTable;
        private ExchangeSetTimestampTable _timestampTable;
        private ISalesCatalogueService _salesCatalogueService;
        private IFileShareService _fileShareService;
        private ILogger<JobService> _logger;
        private TableClient _fakeTableClient;

        [OneTimeSetUp]
        public void SetUp()
        {
            _jobTable = A.Fake<ExchangeSetJobTable>();
            _timestampTable = A.Fake<ExchangeSetTimestampTable>();
            _salesCatalogueService = A.Fake<ISalesCatalogueService>();
            _fileShareService = A.Fake<IFileShareService>();
            _logger = A.Fake<ILogger<JobService>>();
            _fakeTableClient = A.Fake<TableClient>();

            _jobService = new JobService(
                _jobTable,
                _timestampTable,
                _salesCatalogueService,
                _logger,
                _fileShareService
            );
        }

        private void MockSalesCatalogueClientResponse(IResult<S100SalesCatalogueResponse> response)
        {
            A.CallTo(() => _salesCatalogueService.GetS100ProductsFromSpecificDateAsync(
                    A<string>.Ignored, A<string>.Ignored, A<DateTime?>.Ignored, A<ExchangeSetRequestQueueMessage>.Ignored))
                .Returns(Task.FromResult<(S100SalesCatalogueResponse, DateTime?)>((response.IsSuccess(out var value) ? value : null, DateTime.UtcNow)));
        }

        [Test]
        public void WhenFileShareServiceIsNullThenThrowsArgumentNullException()
        {
            Assert.That(() => new JobService(_jobTable, _timestampTable, _salesCatalogueService, _logger, null),
                Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public async Task WhenCreateJobIsCalledWithProductsAreReturned_ThenJobStateShouldBeInProgress()
        {
            var request = CreateQueueMessage();
            var successResponse = Result.Success(new S100SalesCatalogueResponse
            {
                ResponseCode = HttpStatusCode.OK,
                ResponseBody = new List<S100Products>
                {
                    new()
                    {
                        ProductName = "TestProduct1", LatestEditionNumber = 1, LatestUpdateNumber = 0
                    },
                    new()
                    {
                        ProductName = "TestProduct2", LatestEditionNumber = 1, LatestUpdateNumber = 0
                    }
                }
            });
            var batchHandle = A.Fake<IBatchHandle>();

            MockTableClientQuery();
            MockSalesCatalogueClientResponse(successResponse);

            A.CallTo(() => _fileShareService.CreateBatchAsync(A<string>.Ignored, A<CancellationToken>.Ignored))
                .WithAnyArguments()
                .Returns(Task.FromResult<IResult<IBatchHandle>>(Result.Success<IBatchHandle>(batchHandle)));

            var result = await _jobService.CreateJob(request);

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.State, Is.EqualTo(ExchangeSetJobState.InProgress));
                Assert.That(result.Products.Count, Is.EqualTo(2));
            });
        }

        [Test]
        public async Task WhenCreateJobIsCalledWithNotModified_ThenJobIsCancelledAndBatchIdSet()
        {
            var request = CreateQueueMessage();
            var successResponse = Result.Success(new S100SalesCatalogueResponse
            {
                ResponseCode = HttpStatusCode.NotModified,
                ResponseBody = new List<S100Products>()
            });
            var batchHandle = A.Fake<IBatchHandle>();

            MockTableClientQuery();
            MockSalesCatalogueClientResponse(successResponse);

            A.CallTo(() => _fileShareService.CreateBatchAsync(A<string>.Ignored, A<CancellationToken>.Ignored))
                .Returns(Task.FromResult<IResult<IBatchHandle>>(Result.Success(batchHandle)));


            var result = await _jobService.CreateJob(request);

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.State, Is.EqualTo(ExchangeSetJobState.Cancelled));
                Assert.That(result.Products, Is.Null);
            });
        }

        [Test]
        public async Task WhenCreateJobIsCalledWithNoProductsAreReturned_ThenSetsJobStateToCancelled()
        {
            var request = CreateQueueMessage();
            var successResponse = Result.Success(new S100SalesCatalogueResponse
            {
                ResponseCode = HttpStatusCode.BadRequest,
                ResponseBody = new List<S100Products>()
            });
            var batchHandle = A.Fake<IBatchHandle>();

            MockTableClientQuery();
            MockSalesCatalogueClientResponse(successResponse);

            A.CallTo(() => _fileShareService.CreateBatchAsync(A<string>.Ignored, A<CancellationToken>.Ignored))
                .Returns(Task.FromResult<IResult<IBatchHandle>>(Result.Success(batchHandle)));


            var result = await _jobService.CreateJob(request);

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.State, Is.EqualTo(ExchangeSetJobState.Cancelled));
                Assert.That(result.Products, Is.Null);
            });
        }

        [Test]
        public async Task WhenCreateJobIsCalledAndCreateBatchFails_ThenJobStateIsFailed()
        {
            var request = CreateQueueMessage();
            var successResponse = Result.Success(new S100SalesCatalogueResponse
            {
                ResponseCode = HttpStatusCode.OK,
                ResponseBody = new List<S100Products>
                {
                    new S100Products { ProductName = "TestProduct1", LatestEditionNumber = 1, LatestUpdateNumber = 0 }
                }
            });

            MockTableClientQuery();
            MockSalesCatalogueClientResponse(successResponse);

            A.CallTo(() => _fileShareService.CreateBatchAsync(A<string>.Ignored, A<CancellationToken>.Ignored))
                .Returns(Task.FromResult<IResult<IBatchHandle>>(Result.Failure<IBatchHandle>("error")));

            var result = await _jobService.CreateJob(request);

            Assert.That(result.State, Is.EqualTo(ExchangeSetJobState.Failed));
        }

        [Test]
        public async Task WhenCompleteJobAsyncIsCalledWithExitCodeIsFailed_ThenUpdatesJobStateToFailed()
        {
            var job = new ExchangeSetJob
            {
                Id = "test-job-id",
                DataStandard = ExchangeSetDataStandard.S100,
                SalesCatalogueTimestamp = DateTime.UtcNow,
                State = ExchangeSetJobState.Created
            };

            await _jobService.BuilderContainerCompletedAsync(BuilderExitCodes.Failed, job);

            Assert.Multiple(() => { Assert.That(job.State, Is.EqualTo(ExchangeSetJobState.Failed)); });
        }

        [Test]
        public async Task WhenCompleteJobAsyncIsCalledWithExitCodeSuccess_ThenUpdatesJobStateToSucceeded()
        {
            var job = new ExchangeSetJob
            {
                BatchId = "batchId",
                CorrelationId = "correlationId",
                Id = "test-job-id",
                State = ExchangeSetJobState.Created
            };
            var batchDetails = new List<BatchDetails> { new BatchDetails() };
            var batchSearchResponse = new BatchSearchResponse { Entries = batchDetails };

            A.CallTo(() => _fileShareService.CommitBatchAsync(job.BatchId, job.CorrelationId, CancellationToken.None))
                .Returns(Task.FromResult<IResult<CommitBatchResponse>>(Result.Success(new CommitBatchResponse())));

            A.CallTo(() => _fileShareService.SearchCommittedBatchesExcludingCurrentAsync(job.BatchId, job.CorrelationId, A<CancellationToken>.Ignored))
                .Returns(Task.FromResult<IResult<BatchSearchResponse>>(Result.Success(batchSearchResponse)));

            A.CallTo(() => _fileShareService.SetExpiryDateAsync(batchDetails, job.CorrelationId, CancellationToken.None))
                .Returns(Task.FromResult<IResult<SetExpiryDateResponse>>(Result.Success(new SetExpiryDateResponse())));


            await _jobService.BuilderContainerCompletedAsync(BuilderExitCodes.Success, job);

            Assert.Multiple(() => { Assert.That(job.State, Is.EqualTo(ExchangeSetJobState.Succeeded)); });
        }

        [Test]
        public async Task WhenBuilderContainerCompletedWithSuccessAndCommitBatchFails_ThenJobStateIsFailed()
        {
            var job = CreateTestJob();

            A.CallTo(() => _fileShareService.CommitBatchAsync(job.BatchId, job.CorrelationId, CancellationToken.None))
                .Returns(Task.FromResult<IResult<CommitBatchResponse>>(Result.Failure<CommitBatchResponse>("error")));

            await _jobService.BuilderContainerCompletedAsync(BuilderExitCodes.Success, job);

            Assert.Multiple(() => { Assert.That(job.State, Is.EqualTo(ExchangeSetJobState.Failed)); });
        }

        [Test]
        public async Task WhenBuilderContainerCompletedWithSuccessAndSearchAllCommitBatchesFails_ThenJobStateIsFailed()
        {
            var job = CreateTestJob();

            A.CallTo(() => _fileShareService.CommitBatchAsync(job.BatchId, job.CorrelationId, CancellationToken.None))
                .Returns(Task.FromResult<IResult<CommitBatchResponse>>(Result.Success(new CommitBatchResponse())));
            A.CallTo(() => _fileShareService.SearchCommittedBatchesExcludingCurrentAsync(job.BatchId, job.CorrelationId, A<CancellationToken>.Ignored))
                .Returns(Task.FromResult<IResult<BatchSearchResponse>>(Result.Failure<BatchSearchResponse>("error")));

            await _jobService.BuilderContainerCompletedAsync(BuilderExitCodes.Success, job);

            Assert.Multiple(() => { Assert.That(job.State, Is.EqualTo(ExchangeSetJobState.Failed)); });
        }

        [Test]
        public async Task WhenBuilderContainerCompletedWithSuccessAndSetExpiryDateFails_ThenJobStateIsFailed()
        {
            var job = CreateTestJob();

            A.CallTo(() => _fileShareService.CommitBatchAsync(job.BatchId, job.CorrelationId, CancellationToken.None))
                .Returns(Task.FromResult<IResult<CommitBatchResponse>>(Result.Success(new CommitBatchResponse())));

            var batchDetails = new List<BatchDetails> { new BatchDetails() };
            var batchSearchResponse = new BatchSearchResponse { Entries = batchDetails };

            A.CallTo(() => _fileShareService.SearchCommittedBatchesExcludingCurrentAsync(job.BatchId, job.CorrelationId, A<CancellationToken>.Ignored))
                .Returns(Task.FromResult<IResult<BatchSearchResponse>>(Result.Success(batchSearchResponse)));
            A.CallTo(() => _fileShareService.SetExpiryDateAsync(batchDetails, job.CorrelationId, CancellationToken.None))
                .Returns(Task.FromResult<IResult<SetExpiryDateResponse>>(Result.Failure<SetExpiryDateResponse>("error")));

            await _jobService.BuilderContainerCompletedAsync(BuilderExitCodes.Success, job);

            Assert.Multiple(() => { Assert.That(job.State, Is.EqualTo(ExchangeSetJobState.Failed)); });
        }

        [Test]
        public async Task WhenBuilderContainerCompletedWithNonSuccessExitCode_ThenJobStateIsFailed()
        {
            var job = CreateTestJob();

            await _jobService.BuilderContainerCompletedAsync(-1, job);

            Assert.Multiple(() => { Assert.That(job.State, Is.EqualTo(ExchangeSetJobState.Failed)); });
        }

        [Test]
        public async Task WhenSearchAllCommitBatchesReturnsNoEntries_ThenJobStateIsSucceeded()
        {
            var job = CreateTestJob();

            A.CallTo(() => _fileShareService.CommitBatchAsync(job.BatchId, job.CorrelationId, CancellationToken.None))
                .Returns(Task.FromResult<IResult<CommitBatchResponse>>(Result.Success(new CommitBatchResponse())));

            var batchSearchResponse = new BatchSearchResponse { Entries = new List<BatchDetails>() };

            A.CallTo(() => _fileShareService.SearchCommittedBatchesExcludingCurrentAsync(job.BatchId, job.CorrelationId, A<CancellationToken>.Ignored))
                .Returns(Task.FromResult<IResult<BatchSearchResponse>>(Result.Success(batchSearchResponse)));

            await _jobService.BuilderContainerCompletedAsync(BuilderExitCodes.Success, job);

            Assert.Multiple(() => { Assert.That(job.State, Is.EqualTo(ExchangeSetJobState.Succeeded)); });
        }

        private static ExchangeSetRequestQueueMessage CreateQueueMessage(string correlationId = "test-correlation-id",
            string products = "TestProduct")
        {
            return new ExchangeSetRequestQueueMessage
            {
                DataStandard = ExchangeSetDataStandard.S100,
                CorrelationId = correlationId,
                Products = products
            };
        }

        private void MockTableClientQuery()
        {
            A.CallTo(() => _fakeTableClient.QueryAsync(
                    A<Expression<Func<JsonEntity, bool>>>.Ignored,
                    A<int?>.Ignored,
                    A<List<string>>.Ignored,
                    A<CancellationToken>.Ignored))
                .Returns(new List<JsonEntity>().CreateAsyncPageable());
        }

        private ExchangeSetJob CreateTestJob(
            string batchId = "batchId",
            string correlationId = "correlationId",
            string id = "test-job-id",
            ExchangeSetJobState state = ExchangeSetJobState.InProgress)
        {
            return new ExchangeSetJob
            {
                BatchId = batchId,
                CorrelationId = correlationId,
                Id = id,
                State = state
            };
        }
    }
}
