using FakeItEasy;
using Microsoft.Extensions.Logging;
using UKHO.ADDS.Clients.FileShareService.ReadWrite;
using UKHO.ADDS.Clients.FileShareService.ReadWrite.Models;
using UKHO.ADDS.EFS.Builder.S100.IIC;
using UKHO.ADDS.EFS.Builder.S100.Pipelines;
using UKHO.ADDS.EFS.Builder.S100.Pipelines.Distribute;
using UKHO.ADDS.EFS.Domain.Builds.S100;
using UKHO.ADDS.EFS.Domain.Jobs;
using UKHO.ADDS.EFS.Domain.Products;
using UKHO.ADDS.EFS.Domain.Services;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;
using UKHO.ADDS.Infrastructure.Results;

[TestFixture]
public class DistributionPipelineTests
{
    private IFileShareReadWriteClient _fileShareReadWriteClient;
    private IFileNameGeneratorService _fileNameGeneratorService;

    private DistributionPipeline _distributionPipeline;
    private IExecutionContext<S100ExchangeSetPipelineContext> _executionContext;
    private S100ExchangeSetPipelineContext _pipelineContext;
    private IToolClient _toolClient;
    private ILoggerFactory _loggerFactory;
    private ILogger _logger;
    private string _tempFilePath;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _toolClient = A.Fake<IToolClient>();
        _executionContext = A.Fake<IExecutionContext<S100ExchangeSetPipelineContext>>();
        _loggerFactory = A.Fake<ILoggerFactory>();
        _logger = A.Fake<ILogger<ExtractExchangeSetNode>>();
    }

    [SetUp]
    public void SetUp()
    {
        var tempPath = Path.GetTempPath();
        _fileShareReadWriteClient = A.Fake<IFileShareReadWriteClient>();
        _fileNameGeneratorService = A.Fake<IFileNameGeneratorService>();

        _distributionPipeline = new DistributionPipeline(_fileShareReadWriteClient, _fileNameGeneratorService);
        _pipelineContext = new S100ExchangeSetPipelineContext(null, _toolClient, null, null, _loggerFactory)
        {
            Build = new S100Build()
            {
                JobId = JobId.From("testId"),
                BatchId = BatchId.From("a-batch-id"),
                DataStandard = DataStandard.S100
            },
            WorkspaceAuthenticationKey = "authKey",
            ExchangeSetFilePath = Directory.GetParent(tempPath.TrimEnd(Path.DirectorySeparatorChar))!.FullName!,
            ExchangeSetArchiveFolderName = new DirectoryInfo(tempPath.TrimEnd(Path.DirectorySeparatorChar)).Name
        };

        _tempFilePath = Path.Combine(_pipelineContext.ExchangeSetFilePath, _pipelineContext.ExchangeSetArchiveFolderName, _pipelineContext.Build.JobId + ".zip");
        File.WriteAllText(_tempFilePath, "Temporary test file content.");
        A.CallTo(() => _executionContext.Subject).Returns(_pipelineContext);
    }

    // TODO Reinstate

    //[Test]
    //public async Task WhenExecutePipelineAllNodesSucceed_ThenReturnsSucceededResult()
    //{
    //    A.CallTo(() => _fileShareReadWriteClient.AddFileToBatchAsync(
    //            A<BatchHandle>._,
    //            A<Stream>._,
    //            A<string>._,
    //            A<string>._,
    //            A<string>._,
    //            A<CancellationToken>._))
    //        .Returns(Result.Success(new AddFileToBatchResponse()));

    //    var fakeStream = new MemoryStream();
    //    var fakeResult = A.Fake<IResult<Stream>>();
    //    Stream outStream = fakeStream;
    //    IError outError = null;

    //    A.CallTo(() => fakeResult.IsFailure(out outError!, out outStream!)).Returns(false);
    //    A.CallTo(() => _executionContext.Subject.ToolClient.ExtractExchangeSetAsync(A<string>._, A<string>._, A<string>._, A<string>._))
    //        .Returns(Task.FromResult(fakeResult));

    //    var result = await _distributionPipeline.ExecutePipeline(_pipelineContext);

    //    Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Succeeded));
    //    A.CallTo(() => _fileShareReadWriteClient.AddFileToBatchAsync(
    //        A<BatchHandle>._,
    //        A<Stream>._,
    //        A<string>._,
    //        A<string>._,
    //        A<string>._,
    //        A<CancellationToken>._))
    //        .MustHaveHappenedOnceExactly();
    //    A.CallTo(() => _executionContext.Subject.ToolClient.ExtractExchangeSetAsync(
    //            A<string>._,
    //            A<string>._,
    //            A<string>._,
    //            A<string>._))
    //        .MustHaveHappenedOnceExactly();
    //}

    [Test]
    public void WhenFileShareReadWriteClientIsNull_ThenThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new DistributionPipeline(null, null));
    }

    [Test]
    public async Task WhenExecutePipelineUploadFilesNodeFails_ThenReturnsFailedResult()
    {
        A.CallTo(() => _fileShareReadWriteClient.AddFileToBatchAsync(
                A<BatchHandle>._,
                A<Stream>._,
                A<string>._,
                A<string>._,
                A<string>._,
                A<KeyValuePair<string, string>[]>.That.IsNull()))
            .Throws<Exception>();

        var result = await _distributionPipeline.ExecutePipeline(_pipelineContext);

        Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Failed));
    }

    [Test]
    public async Task WhenExecutePipelineExtractExchangeSetNodeFails_ThenReturnsFailedResult()
    {
        var fakeStream = new MemoryStream();
        var fakeResult = A.Fake<IResult<Stream>>();
        Stream outStream = fakeStream;
        var fakeError = A.Fake<IError>();

        A.CallTo(() => fakeResult.IsFailure(out fakeError!, out outStream!)).Returns(true);
        A.CallTo(() => _executionContext.Subject.ToolClient.ExtractExchangeSetAsync(A<JobId>._, A<string>._, A<string>._))
            .Returns(Task.FromResult(fakeResult));

        var result = await _distributionPipeline.ExecutePipeline(_pipelineContext);

        Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Failed));
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _loggerFactory?.Dispose();
    }

    [TearDown]
    public void TearDown()
    {
        if (!string.IsNullOrEmpty(_tempFilePath) && File.Exists(_tempFilePath))
        {
            File.Delete(_tempFilePath);
        }
    }
}

