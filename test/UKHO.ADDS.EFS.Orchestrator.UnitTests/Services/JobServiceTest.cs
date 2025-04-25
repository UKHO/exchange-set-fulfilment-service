using System.Linq.Expressions;
using System.Net;
using Azure.Data.Tables;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using UKHO.ADDS.Clients.SalesCatalogueService;
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
        private ISalesCatalogueClient _salesCatalogueClient;
        private ILogger<JobService> _logger;
        private TableClient _fakeTableClient;

        [OneTimeSetUp]
        public void SetUp()
        {
            _jobTable = A.Fake<ExchangeSetJobTable>();
            _timestampTable = A.Fake<ExchangeSetTimestampTable>();
            _salesCatalogueClient = A.Fake<ISalesCatalogueClient>();
            _logger = A.Fake<ILogger<JobService>>();
            _fakeTableClient = A.Fake<TableClient>();

            _jobService = new JobService(
                _jobTable,
                _timestampTable,
                _salesCatalogueClient,
                _logger
            );
        }

        private void MockSalesCatalogueClientResponse(IResult<S100SalesCatalogueResponse> response)
        {
            A.CallTo(() => _salesCatalogueClient.GetS100ProductsFromSpecificDateAsync(
                    A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Returns(response);
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
                    new S100Products
                    {
                        ProductName = "TestProduct1", LatestEditionNumber = 1, LatestUpdateNumber = 0
                    },
                    new S100Products
                    {
                        ProductName = "TestProduct2", LatestEditionNumber = 1, LatestUpdateNumber = 0
                    }
                }
            });

            MockTableClientQuery();
            MockSalesCatalogueClientResponse(successResponse);

            var result = await _jobService.CreateJob(request);

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.State, Is.EqualTo(ExchangeSetJobState.InProgress));
                Assert.That(result.Products.Count, Is.EqualTo(2));
            });
        }

        [Test]
        public async Task WhenCreateJobIsCalledWithNotModified_ThenSetsJobStateToScsCatalogueUnchanged()
        {
            var request = CreateQueueMessage();
            var successResponse = Result.Success(new S100SalesCatalogueResponse
            {
                ResponseCode = HttpStatusCode.NotModified,
                ResponseBody = new List<S100Products>()
            });

            MockTableClientQuery();
            MockSalesCatalogueClientResponse(successResponse);

            var result = await _jobService.CreateJob(request);

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.State, Is.EqualTo(ExchangeSetJobState.ScsCatalogueUnchanged));
                Assert.That(result.Products, Is.Null);
            });
        }

        [Test]
        public async Task WhenCreateJobIsCalledWithNoProductsAreReturned_ThenSetsJobStateToCancelled()
        {
            var request = CreateQueueMessage();
            var failureResult = Result.Failure<S100SalesCatalogueResponse>(
                new Error
                {
                    Message = "Error Message",
                    Metadata = new Dictionary<string, object> { { "correlationId", request.CorrelationId } }
                });

            MockTableClientQuery();
            MockSalesCatalogueClientResponse(failureResult);

            var result = await _jobService.CreateJob(request);

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.State, Is.EqualTo(ExchangeSetJobState.Cancelled));
                Assert.That(result.Products, Is.Null);
            });
        }

        [Test]
        public async Task WhenCompleteJobAsyncIsCalledWithExitCodeIsFailed_ThenUpdatesJobStateToSucceeded()
        {
            var job = new ExchangeSetJob
            {
                Id = "test-job-id",
                DataStandard = ExchangeSetDataStandard.S100,
                SalesCatalogueTimestamp = DateTime.UtcNow,
                State = ExchangeSetJobState.Created
            };

            await _jobService.BuilderContainerCompletedAsync(BuilderExitCodes.Failed, job);

            Assert.Multiple(() => { Assert.That(job.State, Is.EqualTo(ExchangeSetJobState.Succeeded)); });
        }

        [Test]
        public async Task WhenCompleteJobAsyncIsCalledWithExitCodeSuccess_ThenUpdatesJobStateToFailed()
        {
            var job = new ExchangeSetJob
            {
                Id = "test-job-id",
                DataStandard = ExchangeSetDataStandard.S100,
                SalesCatalogueTimestamp = DateTime.UtcNow,
                State = ExchangeSetJobState.Created
            };

            await _jobService.BuilderContainerCompletedAsync(BuilderExitCodes.Success, job);

            Assert.Multiple(() => { Assert.That(job.State, Is.EqualTo(ExchangeSetJobState.Failed)); });
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
    }
}
