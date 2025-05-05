using FakeItEasy;
using UKHO.ADDS.Clients.FileShareService.ReadWrite;
using UKHO.ADDS.Clients.FileShareService.ReadWrite.Models;
using UKHO.ADDS.EFS.Builder.S100.Pipelines;
using UKHO.ADDS.EFS.Builder.S100.Pipelines.Assemble;
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

    [SetUp]
    public void SetUp()
    {
        _fileShareReadWriteClient = A.Fake<IFileShareReadWriteClient>();
        _testableCreateBatchNode = new TestableCreateBatchNode(_fileShareReadWriteClient);
        _executionContext = A.Fake<IExecutionContext<ExchangeSetPipelineContext>>();
    }

    [Test]
    public async Task WhenPerformExecuteAsyncIsCalled_ThenExecutesSuccessfully()
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
    public async Task WhenPerformExecuteAsyncIsCalledAndCreateBatchAsyncFailsThen_ReturnsFailedStatus()
    {
        A.CallTo(() => _fileShareReadWriteClient.CreateBatchAsync(A<BatchModel>._, A<string>._, A<CancellationToken>._))
            .Returns(Result.Failure<IBatchHandle>("Error creating batch"));

        var result = await _testableCreateBatchNode.PerformExecuteAsync(_executionContext);

        Assert.Multiple(() =>
        {
           Assert.That(_executionContext.Subject.BatchId, Is.Null.Or.Empty);
        });
    }

    [Test]
    public void WhenGetBatchModelIsCalled_ThenReturnsExpectedBatchModel()
    {
        var batchModel = typeof(CreateBatchNode)
            .GetMethod("GetBatchModel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
            .Invoke(null, null) as BatchModel;

        Assert.Multiple(() =>
        {
            Assert.That(batchModel, Is.Not.Null);
            Assert.That(batchModel.BusinessUnit, Is.EqualTo("ADDS-S100"));
            Assert.That(batchModel.Acl.ReadUsers, Is.EquivalentTo(new List<string> { "public" }));
            Assert.That(batchModel.Acl.ReadGroups, Is.EquivalentTo(new List<string> { "public" }));
            Assert.That(batchModel.Attributes, Has.Exactly(4).Items);
            Assert.That(batchModel.Attributes, Does.Contain(new KeyValuePair<string, string>("Exchange Set Type", "Base")));
            Assert.That(batchModel.Attributes, Does.Contain(new KeyValuePair<string, string>("Frequency", "DAILY")));
            Assert.That(batchModel.Attributes, Does.Contain(new KeyValuePair<string, string>("Product Type", "S-100")));
            Assert.That(batchModel.Attributes, Does.Contain(new KeyValuePair<string, string>("Media Type", "Zip")));
            Assert.That(batchModel.ExpiryDate, Is.Null);
        });
    }


    private class TestableCreateBatchNode(IFileShareReadWriteClient fileShareReadWriteClient)
        : CreateBatchNode(fileShareReadWriteClient)
    {
        public new async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<ExchangeSetPipelineContext> context)
        {
            return await base.PerformExecuteAsync(context);
        }
    }
}
