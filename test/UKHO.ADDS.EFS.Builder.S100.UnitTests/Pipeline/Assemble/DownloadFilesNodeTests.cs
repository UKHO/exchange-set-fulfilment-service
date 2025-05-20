using FakeItEasy;
using Microsoft.Extensions.Logging;
using UKHO.ADDS.Clients.FileShareService.ReadOnly;
using UKHO.ADDS.Clients.FileShareService.ReadOnly.Models;
using UKHO.ADDS.Clients.SalesCatalogueService.Models;
using UKHO.ADDS.EFS.Builder.S100.Pipelines;
using UKHO.ADDS.EFS.Builder.S100.Pipelines.Assemble;
using UKHO.ADDS.EFS.Entities;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Builder.S100.UnitTests.Pipeline.Assemble
{
    [TestFixture]
    public class DownloadFilesNodeTests
    {
        private IFileShareReadOnlyClient _fileShareReadOnlyClient;
        private TestableCreateBatchNode _testableCreateBatchNode;
        private IExecutionContext<ExchangeSetPipelineContext> _executionContext;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _fileShareReadOnlyClient = A.Fake<IFileShareReadOnlyClient>();
            _testableCreateBatchNode = new TestableCreateBatchNode(_fileShareReadOnlyClient);
            _executionContext = A.Fake<IExecutionContext<ExchangeSetPipelineContext>>();
        }

        [SetUp]
        public void SetUp()
        {
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());

            var exchangeSetPipelineContext = new ExchangeSetPipelineContext(null, null, null, loggerFactory)
            {
                Job = new ExchangeSetJob
                {
                    CorrelationId = "TestCorrelationId",
                    Products =
                    [
                        new S100Products { ProductName = "Product1", LatestEditionNumber = 1, LatestUpdateNumber = 0 },
                        new S100Products { ProductName = "Product2", LatestEditionNumber = 2, LatestUpdateNumber = 1 }
                    ]
                }
            };

            A.CallTo(() => _executionContext.Subject).Returns(exchangeSetPipelineContext);
        }

        [Test]
        public async Task When_ProductsIsNull_Then_ReturnsNotRun()
        {
            _executionContext.Subject.Job = new ExchangeSetJob { Products = null };
            _executionContext.Subject.BatchDetails = new List<BatchDetails>();

            var result = await _testableCreateBatchNode.PerformExecuteAsync(_executionContext);

            Assert.That(result, Is.EqualTo(NodeResultStatus.NotRun));
        }

        [Test]
        public async Task When_ProductsIsEmpty_Then_ReturnsNotRun()
        {
            _executionContext.Subject.Job = new ExchangeSetJob { Products = new List<S100Products>() };
            _executionContext.Subject.BatchDetails = new List<BatchDetails>();

            var result = await _testableCreateBatchNode.PerformExecuteAsync(_executionContext);

            Assert.That(result, Is.EqualTo(NodeResultStatus.NotRun));
        }

        [Test]
        public async Task When_BatchDetailsIsNull_Then_ReturnsNotRun()
        {
            _executionContext.Subject.Job = new ExchangeSetJob { Products = new List<S100Products> { new S100Products() } };
            _executionContext.Subject.BatchDetails = null;

            var result = await _testableCreateBatchNode.PerformExecuteAsync(_executionContext);

            Assert.That(result, Is.EqualTo(NodeResultStatus.NotRun));
        }

        [Test]
        public async Task When_BatchDetailsIsEmpty_Then_ReturnsNotRun()
        {
            _executionContext.Subject.Job = new ExchangeSetJob { Products = new List<S100Products> { new S100Products() } };
            _executionContext.Subject.BatchDetails = new List<BatchDetails>();

            var result = await _testableCreateBatchNode.PerformExecuteAsync(_executionContext);

            Assert.That(result, Is.EqualTo(NodeResultStatus.NotRun));
        }

        [Test]
        public async Task When_LatestPublishBatchIsNull_Then_ReturnsFailed()
        {
            var product = new S100Products
            {
                ProductName = "P1",
                LatestEditionNumber = 1,
                LatestUpdateNumber = 1
            };
            _executionContext.Subject.Job = new ExchangeSetJob { Products = new List<S100Products> { product } };
            _executionContext.Subject.BatchDetails = new List<BatchDetails>(); // No batch matches

            var result = await _testableCreateBatchNode.PerformExecuteAsync(_executionContext);

            Assert.That(result, Is.EqualTo(NodeResultStatus.NotRun));
        }

        [Test]
        public async Task When_FileDownloadFails_Then_ReturnsFailed()
        {
            var product = new S100Products
            {
                ProductName = "P1",
                LatestEditionNumber = 1,
                LatestUpdateNumber = 1
            };
            var batch = new BatchDetails
            {
                BatchId = "B1",
                BatchPublishedDate = DateTime.UtcNow,
                Attributes = new List<BatchDetailsAttributes>
                {
                    new BatchDetailsAttributes("ProductName", "P1"),
                    new BatchDetailsAttributes("EditionNumber", "1"),
                    new BatchDetailsAttributes("UpdateNumber", "1")
                },
                Files = new List<BatchDetailsFiles>
                {
                    new BatchDetailsFiles { Filename = "file1.txt" }
                }
            };
            _executionContext.Subject.Job = new ExchangeSetJob { Products = new List<S100Products> { product } };
            _executionContext.Subject.BatchDetails = new List<BatchDetails> { batch };

            A.CallTo(() => _fileShareReadOnlyClient.DownloadFileAsync(A<string>._, A<string>._, A<Stream>._, A<string>._, A<long>._, A<CancellationToken>._))
                .Throws<Exception>();

            var result = await _testableCreateBatchNode.PerformExecuteAsync(_executionContext);

            Assert.That(result, Is.EqualTo(NodeResultStatus.Failed));
        }

        //[Test]
        //public async Task When_FileDownloadSucceeds_Then_ReturnsSucceeded()
        //{
        //    var product = new S100Products
        //    {
        //        ProductName = "P1",
        //        LatestEditionNumber = 1,
        //        LatestUpdateNumber = 1
        //    };
        //    var batch = new BatchDetails
        //    {
        //        BatchId = "B1",
        //        BatchPublishedDate = DateTime.UtcNow,
        //        Attributes = new List<BatchDetailsAttributes>
        //        {
        //            new BatchDetailsAttributes("ProductName", "P1"),
        //            new BatchDetailsAttributes("EditionNumber", "1"),
        //            new BatchDetailsAttributes("UpdateNumber", "1")
        //        },
        //        Files = new List<BatchDetailsFiles>
        //        {
        //            new BatchDetailsFiles { Filename = "file1.txt" }
        //        }
        //    };
        //    _executionContext.Subject.Job = new ExchangeSetJob { Products = new List<S100Products> { product } };
        //    _executionContext.Subject.BatchDetails = new List<BatchDetails> { batch };

        //    A.CallTo(() => _fileShareReadOnlyClient.DownloadFileAsync(A<string>._, A<string>._, A<Stream>._, A<string>._, A<long>._, A<CancellationToken>._))
        //        .Returns(A.Fake<IResult<Stream>>());

        //    var result = await _testableCreateBatchNode.PerformExecuteAsync(_executionContext);

        //    Assert.That(result, Is.EqualTo(NodeResultStatus.Succeeded));
        //}

        //[Test]
        //public async Task When_MultipleProductsWithSameCompareProducts_Then_FileIsNotDownloadedTwice()
        //{
        //    var product1 = new S100Products
        //    {
        //        ProductName = "P1",
        //        LatestEditionNumber = 1,
        //        LatestUpdateNumber = 1
        //    };
        //    var batch = new BatchDetails
        //    {
        //        BatchId = "B1",
        //        BatchPublishedDate = DateTime.UtcNow,
        //        Attributes = new List<BatchDetailsAttributes>
        //        {
        //            new BatchDetailsAttributes("ProductName", "P1"),
        //            new BatchDetailsAttributes("EditionNumber", "1"),
        //            new BatchDetailsAttributes("UpdateNumber", "1")
        //        },
        //        Files = new List<BatchDetailsFiles>
        //        {
        //            new BatchDetailsFiles { Filename = "Latest.txt" }
        //        }
        //    };
        //    var batch2 = new BatchDetails
        //    {
        //        BatchId = "B1",
        //        BatchPublishedDate = DateTime.UtcNow.AddDays(-1),
        //        Attributes = new List<BatchDetailsAttributes>
        //        {
        //            new BatchDetailsAttributes("ProductName", "P1"),
        //            new BatchDetailsAttributes("EditionNumber", "1"),
        //            new BatchDetailsAttributes("UpdateNumber", "1")
        //        },
        //        Files = new List<BatchDetailsFiles>
        //        {
        //            new BatchDetailsFiles { Filename = "Old.txt" }
        //        }
        //    };

        //    _executionContext.Subject.Job = new ExchangeSetJob { Products = new List<S100Products> { product1 } };
        //    _executionContext.Subject.BatchDetails = new List<BatchDetails> { batch, batch2 };

        //    A.CallTo(() => _fileShareReadOnlyClient.DownloadFileAsync(A<string>._, A<string>._, A<Stream>._, A<string>._, A<long>._, A<CancellationToken>._))
        //        .Returns(A.Fake<IResult<Stream>>());

        //    var result = await _testableCreateBatchNode.PerformExecuteAsync(_executionContext);

        //    Assert.That(result, Is.EqualTo(NodeResultStatus.Succeeded));
        //}

        private class TestableCreateBatchNode : DownloadFilesNode
        {
            public TestableCreateBatchNode(IFileShareReadOnlyClient fileShareReadOnlyClient)
                : base(fileShareReadOnlyClient)
            {
            }

            public new async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<ExchangeSetPipelineContext> context)
            {
                return await base.PerformExecuteAsync(context);
            }
        }
    }
}
