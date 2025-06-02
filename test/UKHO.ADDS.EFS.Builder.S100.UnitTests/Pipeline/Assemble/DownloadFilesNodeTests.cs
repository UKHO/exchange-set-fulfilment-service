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
using UKHO.ADDS.Infrastructure.Results;

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
                WorkSpaceRootPath = Path.GetTempPath(),
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
        public async Task WhenDownloadFileAsyncFails_ThenReturnsFailed()
        {
            var batch = new BatchDetails(
                batchId: "b1",
                attributes: new List<BatchDetailsAttributes>
                {
                    new BatchDetailsAttributes("ProductName", "P1"),
                    new BatchDetailsAttributes("EditionNumber", "1"),
                    new BatchDetailsAttributes("UpdateNumber", "1")
                },
                batchPublishedDate: DateTime.UtcNow,
                files: new List<BatchDetailsFiles>
                {
                    new BatchDetailsFiles("file1.txt")
                }
            );
            _executionContext.Subject.BatchDetails = new List<BatchDetails> { batch };

            var fakeResult = A.Fake<IResult<Stream>>();
            IError outError = A.Fake<IError>();
            Stream outStream = null;

            A.CallTo(() => fakeResult.IsFailure(out outError, out outStream)).Returns(true);

            A.CallTo(() => _fileShareReadOnlyClient.DownloadFileAsync(A<string>._, A<string>._, A<Stream>._, A<string>._, A<long>._, A<CancellationToken>._))
                .Returns(Task.FromResult(fakeResult));

            var result = await _testableCreateBatchNode.PerformExecuteAsync(_executionContext);
            Assert.That(result, Is.EqualTo(NodeResultStatus.Failed));
        }

        [Test]
        public async Task WhenDownloadFileAsyncExceptionThrown_ThenReturnsFailed()
        {
            var batch = new BatchDetails(
                batchId: "b1",
                attributes: new List<BatchDetailsAttributes>
                {
                    new BatchDetailsAttributes("ProductName", "P1"),
                    new BatchDetailsAttributes("EditionNumber", "1"),
                    new BatchDetailsAttributes("UpdateNumber", "1")
                },
                batchPublishedDate: DateTime.UtcNow,
                files: new List<BatchDetailsFiles>
                {
                    new BatchDetailsFiles("file1.txt")
                }
            );
            _executionContext.Subject.BatchDetails = new List<BatchDetails> { batch };

            A.CallTo(() => _fileShareReadOnlyClient.DownloadFileAsync(A<string>._, A<string>._, A<Stream>._, A<string>._, A<long>._, A<CancellationToken>._))
                .Throws<Exception>();

            var result = await _testableCreateBatchNode.PerformExecuteAsync(_executionContext);
            Assert.That(result, Is.EqualTo(NodeResultStatus.Failed));
        }

        [Test]
        public async Task WhenBatchFilesIsEmpty_ThenReturnsFailed()
        {
            var batch = new BatchDetails(
                batchId: "b1",
                attributes: new List<BatchDetailsAttributes>
                {
                    new BatchDetailsAttributes("ProductName", "P1"),
                    new BatchDetailsAttributes("EditionNumber", "1"),
                    new BatchDetailsAttributes("UpdateNumber", "1")
                },
                batchPublishedDate: DateTime.UtcNow,
                files: new List<BatchDetailsFiles>()
            );
            _executionContext.Subject.BatchDetails = new List<BatchDetails> { batch };
            var result = await _testableCreateBatchNode.PerformExecuteAsync(_executionContext);
            Assert.That(result, Is.EqualTo(NodeResultStatus.Failed));
        }
        
        [Test]
        public async Task WhenDownloadFileAsyncSucceeds_ThenReturnsSucceeded()
        {
            var batch = new BatchDetails(
                batchId: "b1",
                attributes: new List<BatchDetailsAttributes>
                {
                    new BatchDetailsAttributes("ProductName", "P1"),
                    new BatchDetailsAttributes("EditionNumber", "1"),
                    new BatchDetailsAttributes("UpdateNumber", "1")
                },
                batchPublishedDate: DateTime.UtcNow,
                files: new List<BatchDetailsFiles>
                {
                    new BatchDetailsFiles("file1.txt"),
                    new BatchDetailsFiles("ABC1234.001"),
                    new BatchDetailsFiles("DEF5678.h5")
                }
            );
            _executionContext.Subject.BatchDetails = new List<BatchDetails> { batch };

            var fakeResult = A.Fake<IResult<Stream>>();
            IError outError = null;
            Stream outStream = new MemoryStream();
            A.CallTo(() => fakeResult.IsFailure(out outError, out outStream)).Returns(false);
            A.CallTo(() => _fileShareReadOnlyClient.DownloadFileAsync(A<string>._, A<string>._, A<Stream>._, A<string>._, A<long>._, A<CancellationToken>._))
                .Returns(Task.FromResult(fakeResult));

            var result = await _testableCreateBatchNode.PerformExecuteAsync(_executionContext);
            Assert.That(result, Is.EqualTo(NodeResultStatus.Succeeded));
        }

        [Test]
        public async Task WhenMultipleBatchesWithSameProductEditionUpdate_ThenOnlyLatestIsProcessed()
        {
            var now = DateTime.UtcNow;
            var batch1 = new BatchDetails(
                batchId: "b1",
                attributes: new List<BatchDetailsAttributes>
                {
                    new BatchDetailsAttributes("ProductName", "P1"),
                    new BatchDetailsAttributes("EditionNumber", "1"),
                    new BatchDetailsAttributes("UpdateNumber", "1")
                },
                batchPublishedDate: now.AddMinutes(-1),
                files: new List<BatchDetailsFiles>
                {
                    new BatchDetailsFiles("file1.txt")
                }
            );
            var batch2 = new BatchDetails(
                batchId: "b2",
                attributes: new List<BatchDetailsAttributes>
                {
                    new BatchDetailsAttributes("ProductName", "P1"),
                    new BatchDetailsAttributes("EditionNumber", "1"),
                    new BatchDetailsAttributes("UpdateNumber", "1")
                },
                batchPublishedDate: now,
                files: new List<BatchDetailsFiles>
                {
                    new BatchDetailsFiles("file2.txt")
                }
            );
            _executionContext.Subject.BatchDetails = new List<BatchDetails> { batch1, batch2 };

            var fakeResult = A.Fake<IResult<Stream>>();
            IError outError = null;
            Stream outStream = new MemoryStream();
            A.CallTo(() => fakeResult.IsFailure(out outError, out outStream)).Returns(false);
            A.CallTo(() => _fileShareReadOnlyClient.DownloadFileAsync(A<string>._, A<string>._, A<Stream>._, A<string>._, A<long>._, A<CancellationToken>._))
                .Returns(Task.FromResult(fakeResult));

            var result = await _testableCreateBatchNode.PerformExecuteAsync(_executionContext);
            Assert.That(result, Is.EqualTo(NodeResultStatus.Succeeded));
        }

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
