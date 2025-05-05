using FakeItEasy;
using UKHO.ADDS.Clients.FileShareService.ReadWrite;
using UKHO.ADDS.Clients.FileShareService.ReadWrite.Models;
using UKHO.ADDS.EFS.Builder.S100.Pipelines;
using UKHO.ADDS.EFS.Builder.S100.Pipelines.Assemble;
using UKHO.ADDS.EFS.Entities;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;
using UKHO.ADDS.Infrastructure.Results;

namespace UKHO.ADDS.EFS.Builder.S100.UnitTests.Pipeline.Assemble;

[TestFixture]
internal class CreateBatchNodeTest
{
    private IFileShareReadWriteClient _fileShareReadWriteClient;
    private TestableCreateBatchNode _testableCreateBatchNode;
    private IExecutionContext<ExchangeSetPipelineContext> _executionContext;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _fileShareReadWriteClient = A.Fake<IFileShareReadWriteClient>();
        _testableCreateBatchNode = new TestableCreateBatchNode(_fileShareReadWriteClient);
        _executionContext = A.Fake<IExecutionContext<ExchangeSetPipelineContext>>();

        var exchangeSetPipelineContext = new ExchangeSetPipelineContext(
            null,
            null,
            null,
            null)
        {
            Job = new ExchangeSetJob { CorrelationId = "TestCorrelationId" }
        };

        A.CallTo(() => _executionContext.Subject).Returns(exchangeSetPipelineContext);
    }

    [Test]
    public async Task WhenPerformExecuteAsyncIsCalled_ThenReturnsSucceededAndSetsBatchId()
    {
        var batchHandle = A.Fake<IBatchHandle>();
        A.CallTo(() => batchHandle.BatchId).Returns("ValidBatchId");
        A.CallTo(() => _fileShareReadWriteClient.CreateBatchAsync(A<BatchModel>._, A<string>._, A<CancellationToken>._))
            .Returns(Result.Success(batchHandle));

        var result = await _testableCreateBatchNode.PerformExecuteAsync(_executionContext);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(NodeResultStatus.Succeeded));
            Assert.That(_executionContext.Subject.BatchId, Is.EqualTo("ValidBatchId"));
        });
    }

    [Test]
    public async Task WhenPerformExecuteAsyncIsCalledAndCreateBatchFails_ThenReturnsSucceededWithEmptyBatchId()
    {
        A.CallTo(() => _fileShareReadWriteClient.CreateBatchAsync(A<BatchModel>._, A<string>._, A<CancellationToken>._))
            .Returns(Result.Failure<IBatchHandle>("Error creating batch"));

        var result = await _testableCreateBatchNode.PerformExecuteAsync(_executionContext);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(NodeResultStatus.Succeeded));
            Assert.That(_executionContext.Subject.BatchId, Is.EqualTo(string.Empty));
        });
    }

    [Test]
    public async Task When_PerformExecuteAsyncIsCalledAndBatchIdIsNull_ThenReturnsSucceededWithEmptyBatchId()
    {
        var batchHandle = A.Fake<IBatchHandle>();
        A.CallTo(() => batchHandle.BatchId)!.Returns(null);
        A.CallTo(() => _fileShareReadWriteClient.CreateBatchAsync(A<BatchModel>._, A<string>._, A<CancellationToken>._))
            .Returns(Result.Success(batchHandle));

        var result = await _testableCreateBatchNode.PerformExecuteAsync(_executionContext);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(NodeResultStatus.Succeeded));
            Assert.That(_executionContext.Subject.BatchId, Is.EqualTo(null));
        });
    }

    [Test]
    public async Task WhenCreateBatchAsyncIsCalled_ThenBatchModelIsCorrectlyConfigured()
    {
        BatchModel capturedBatchModel = null;
        A.CallTo(() => _fileShareReadWriteClient.CreateBatchAsync(A<BatchModel>._, A<string>._, A<CancellationToken>._))
            .Invokes((BatchModel batchModel, string correlationId, CancellationToken _) =>
            {
                capturedBatchModel = batchModel;
            })
            .Returns(Result.Failure<IBatchHandle>("Error"));

        await _testableCreateBatchNode.PerformExecuteAsync(_executionContext);

        Assert.Multiple(() =>
        {
            Assert.That(capturedBatchModel, Is.Not.Null);
            Assert.That(capturedBatchModel.BusinessUnit, Is.EqualTo("ADDS-S100"));
            Assert.That(capturedBatchModel.Acl.ReadUsers, Contains.Item("public"));
            Assert.That(capturedBatchModel.Acl.ReadGroups, Contains.Item("public"));
            Assert.That(capturedBatchModel.Attributes,
                Contains.Item(new KeyValuePair<string, string>("Exchange Set Type", "Base")));
            Assert.That(capturedBatchModel.Attributes,
                Contains.Item(new KeyValuePair<string, string>("Frequency", "DAILY")));
            Assert.That(capturedBatchModel.Attributes,
                Contains.Item(new KeyValuePair<string, string>("Product Type", "S-100")));
            Assert.That(capturedBatchModel.Attributes,
                Contains.Item(new KeyValuePair<string, string>("Media Type", "Zip")));
        });
    }

    [Test]
    public void WhenFileShareReadWriteClientIsNull_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new CreateBatchNode(null));
    }

    private class TestableCreateBatchNode : CreateBatchNode
    {
        public TestableCreateBatchNode(IFileShareReadWriteClient fileShareReadWriteClient)
            : base(fileShareReadWriteClient)
        {
        }

        public new async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<ExchangeSetPipelineContext> context)
        {
            return await base.PerformExecuteAsync(context);
        }
    }
}
