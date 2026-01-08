using FakeItEasy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UKHO.ADDS.EFS.Domain.Builds;
using UKHO.ADDS.EFS.Domain.Builds.S100;
using UKHO.ADDS.EFS.Domain.Files;
using UKHO.ADDS.EFS.Domain.Jobs;
using UKHO.ADDS.EFS.Domain.Products;
using UKHO.ADDS.EFS.Domain.Services;
using UKHO.ADDS.EFS.Orchestrator.Api.Messages;
using UKHO.ADDS.EFS.Orchestrator.Api.Models;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Completion.Nodes.S100;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Factories;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Completion;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Orchestrator.UnitTests.Pipelines.Completion.Nodes.S100
{
    [TestFixture]
    internal class SendCallbackNotificationNodeTests
    {
        private ICallbackNotificationService _callbackNotificationService;
        private IExchangeSetResponseFactory _exchangeSetResponseFactory;
        private SendCallbackNotificationNode _sendCallbackNotificationNode;
        private ILogger _logger;
        private CompletionNodeEnvironment _nodeEnvironment;
        private IExecutionContext<PipelineContext<S100Build>> _executionContext;
        private PipelineContext<S100Build> _pipelineContext;
        private IConfiguration _configuration;
        private IStorageService _storageService;
        private readonly CancellationToken _cancellationToken = CancellationToken.None;

        private static readonly JobId TestJobId = JobId.From("test-job-id");
        private static readonly BatchId TestBatchId = BatchId.From("test-batch-id");
        private static readonly CallbackUri TestCallbackUri = CallbackUri.From(new Uri("https://example.com/callback"));
        private static readonly Link TestStatusLink = new() { Uri = new Uri("https://example.com/status") };
        private static readonly Link TestDetailsLink = new() { Uri = new Uri("https://example.com/details") };

        [SetUp]
        public void SetUp()
        {
            _callbackNotificationService = A.Fake<ICallbackNotificationService>();
            _exchangeSetResponseFactory = A.Fake<IExchangeSetResponseFactory>();
            _logger = A.Fake<ILogger>();
            _configuration = A.Fake<IConfiguration>();
            _storageService = A.Fake<IStorageService>();

            _nodeEnvironment = new CompletionNodeEnvironment(_configuration, _cancellationToken, _logger, BuilderExitCode.Success);
            _sendCallbackNotificationNode = new SendCallbackNotificationNode(_nodeEnvironment, _callbackNotificationService, _exchangeSetResponseFactory);

            var job = CreateJob(TestCallbackUri, TestBatchId);
            var build = new S100Build();
            _pipelineContext = new PipelineContext<S100Build>(job, build, _storageService);
            _executionContext = A.Fake<IExecutionContext<PipelineContext<S100Build>>>();
            A.CallTo(() => _executionContext.Subject).Returns(_pipelineContext);
        }

        [Test]
        public void WhenConstructorCalledWithNullCallbackNotificationService_ThenThrowsArgumentNullException()
        {
            Assert.That(() => new SendCallbackNotificationNode(_nodeEnvironment, null!, _exchangeSetResponseFactory),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("callbackNotificationService"));
        }

        [Test]
        public void WhenConstructorCalledWithNullExchangeSetResponseFactory_ThenThrowsArgumentNullException()
        {
            Assert.That(() => new SendCallbackNotificationNode(_nodeEnvironment, _callbackNotificationService, null!),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("exchangeSetResponseFactory"));
        }

        [Test]
        public void WhenConstructorCalledWithValidParameters_ThenInstanceIsCreated()
        {
            var node = new SendCallbackNotificationNode(_nodeEnvironment, _callbackNotificationService, _exchangeSetResponseFactory);
            Assert.That(node, Is.Not.Null);
            Assert.That(node, Is.InstanceOf<SendCallbackNotificationNode>());
        }

        [Test]
        public async Task WhenShouldExecuteAsyncCalledWithCallbackUriAndBatchIdAndSuccessfulBuild_ThenReturnsTrue()
        {
            var result = await _sendCallbackNotificationNode.ShouldExecuteAsync(_executionContext);
            Assert.That(result, Is.True);
        }

        [Test]
        public async Task WhenShouldExecuteAsyncCalledWithCallbackUriNone_ThenReturnsFalse()
        {
            var jobWithNoCallback = CreateJobWithNoCallback();
            var contextWithNoCallback = new PipelineContext<S100Build>(jobWithNoCallback, new S100Build(), _storageService);

            A.CallTo(() => _executionContext.Subject).Returns(contextWithNoCallback);

            var result = await _sendCallbackNotificationNode.ShouldExecuteAsync(_executionContext);

            Assert.That(result, Is.False);
        }

        [Test]
        public async Task WhenShouldExecuteAsyncCalledWithBatchIdNone_ThenReturnsFalse()
        {
            var jobWithNoBatch = CreateJobWithNoBatch();
            var contextWithNoBatch = new PipelineContext<S100Build>(jobWithNoBatch, new S100Build(), _storageService);

            A.CallTo(() => _executionContext.Subject).Returns(contextWithNoBatch);

            var result = await _sendCallbackNotificationNode.ShouldExecuteAsync(_executionContext);

            Assert.That(result, Is.False);
        }

        [Test]
        public async Task WhenShouldExecuteAsyncCalledWithBuilderExitCodeFailed_ThenReturnsFalse()
        {
            var failedEnvironment = new CompletionNodeEnvironment(_configuration, _cancellationToken, _logger, BuilderExitCode.Failed);
            var node = new SendCallbackNotificationNode(failedEnvironment, _callbackNotificationService, _exchangeSetResponseFactory);
            var result = await node.ShouldExecuteAsync(_executionContext);

            Assert.That(result, Is.False);
        }

        [Test]
        public async Task WhenShouldExecuteAsyncCalledWithBuilderExitCodeNotRun_ThenReturnsFalse()
        {
            var notRunEnvironment = new CompletionNodeEnvironment(_configuration, _cancellationToken, _logger, BuilderExitCode.NotRun);
            var node = new SendCallbackNotificationNode(notRunEnvironment, _callbackNotificationService, _exchangeSetResponseFactory);
            var result = await node.ShouldExecuteAsync(_executionContext);

            Assert.That(result, Is.False);
        }

        [Test]
        public async Task WhenShouldExecuteAsyncCalledWithAllConditionsMetExceptCallbackUri_ThenReturnsFalse()
        {
            var jobWithNoCallback = CreateJobWithNoCallback();
            var contextWithNoCallback = new PipelineContext<S100Build>(jobWithNoCallback, new S100Build(), _storageService);

            A.CallTo(() => _executionContext.Subject).Returns(contextWithNoCallback);

            var result = await _sendCallbackNotificationNode.ShouldExecuteAsync(_executionContext);

            Assert.That(result, Is.False);
        }

        [Test]
        public async Task WhenShouldExecuteAsyncCalledWithAllConditionsMetExceptBatchId_ThenReturnsFalse()
        {
            var jobWithNoBatch = CreateJobWithNoBatch();
            var contextWithNoBatch = new PipelineContext<S100Build>(jobWithNoBatch, new S100Build(), _storageService);

            A.CallTo(() => _executionContext.Subject).Returns(contextWithNoBatch);

            var result = await _sendCallbackNotificationNode.ShouldExecuteAsync(_executionContext);

            Assert.That(result, Is.False);
        }

        [Test]
        public async Task WhenShouldExecuteAsyncCalledWithAllConditionsMetExceptBuilderExitCode_ThenReturnsFalse()
        {
            var failedEnvironment = new CompletionNodeEnvironment(_configuration, _cancellationToken, _logger, BuilderExitCode.Failed);
            var node = new SendCallbackNotificationNode(failedEnvironment, _callbackNotificationService, _exchangeSetResponseFactory);
            var result = await node.ShouldExecuteAsync(_executionContext);

            Assert.That(result, Is.False);
        }

        [Test]
        public async Task WhenPerformExecuteAsyncCalledSuccessfully_ThenReturnsSucceeded()
        {
            var exchangeSetResponse = CreateExchangeSetResponse();

            A.CallTo(() => _exchangeSetResponseFactory.CreateResponse(_pipelineContext.Job))
                .Returns(exchangeSetResponse);
            A.CallTo(() => _callbackNotificationService.SendCallbackNotificationAsync(
                _pipelineContext.Job, exchangeSetResponse, _cancellationToken))
                .Returns(Task.CompletedTask);

            var result = await _sendCallbackNotificationNode.ExecuteAsync(_executionContext);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Succeeded));
        }

        [Test]
        public async Task WhenPerformExecuteAsyncCalledSuccessfully_ThenCallsExchangeSetResponseFactoryWithCorrectJob()
        {
            var exchangeSetResponse = CreateExchangeSetResponse();

            A.CallTo(() => _exchangeSetResponseFactory.CreateResponse(A<Job>._))
                .Returns(exchangeSetResponse);

            await _sendCallbackNotificationNode.ExecuteAsync(_executionContext);

            A.CallTo(() => _exchangeSetResponseFactory.CreateResponse(_pipelineContext.Job))
                .MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task WhenPerformExecuteAsyncCalledSuccessfully_ThenCallsCallbackNotificationServiceWithCorrectParameters()
        {
            var exchangeSetResponse = CreateExchangeSetResponse();

            A.CallTo(() => _exchangeSetResponseFactory.CreateResponse(_pipelineContext.Job))
                .Returns(exchangeSetResponse);

            await _sendCallbackNotificationNode.ExecuteAsync(_executionContext);

            A.CallTo(() => _callbackNotificationService.SendCallbackNotificationAsync(
                _pipelineContext.Job, exchangeSetResponse, _cancellationToken))
                .MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task WhenPerformExecuteAsyncCalledAndExchangeSetResponseFactoryThrowsException_ThenReturnsSucceededWithErrors()
        {
            A.CallTo(() => _exchangeSetResponseFactory.CreateResponse(_pipelineContext.Job))
                .Throws<InvalidOperationException>();

            var result = await _sendCallbackNotificationNode.ExecuteAsync(_executionContext);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.SucceededWithErrors));
        }

        [Test]
        public async Task WhenPerformExecuteAsyncCalledAndCallbackNotificationServiceThrowsException_ThenReturnsSucceededWithErrors()
        {
            var exchangeSetResponse = CreateExchangeSetResponse();

            A.CallTo(() => _exchangeSetResponseFactory.CreateResponse(_pipelineContext.Job))
                .Returns(exchangeSetResponse);
            A.CallTo(() => _callbackNotificationService.SendCallbackNotificationAsync(
                _pipelineContext.Job, exchangeSetResponse, _cancellationToken))
                .Throws<HttpRequestException>();

            var result = await _sendCallbackNotificationNode.ExecuteAsync(_executionContext);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.SucceededWithErrors));
        }

        [Test]
        public async Task WhenPerformExecuteAsyncCalledAndExchangeSetResponseFactoryThrowsArgumentException_ThenReturnsSucceededWithErrors()
        {
            A.CallTo(() => _exchangeSetResponseFactory.CreateResponse(_pipelineContext.Job)).Throws<ArgumentException>();

            var result = await _sendCallbackNotificationNode.ExecuteAsync(_executionContext);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.SucceededWithErrors));
        }

        [Test]
        public async Task WhenPerformExecuteAsyncCalledAndCallbackNotificationServiceThrowsTimeoutException_ThenReturnsSucceededWithErrors()
        {
            var exchangeSetResponse = CreateExchangeSetResponse();

            A.CallTo(() => _exchangeSetResponseFactory.CreateResponse(_pipelineContext.Job))
                .Returns(exchangeSetResponse);
            A.CallTo(() => _callbackNotificationService.SendCallbackNotificationAsync(
                _pipelineContext.Job, exchangeSetResponse, _cancellationToken))
                .Throws<TimeoutException>();

            var result = await _sendCallbackNotificationNode.ExecuteAsync(_executionContext);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.SucceededWithErrors));
        }

        [Test]
        public async Task WhenPerformExecuteAsyncCalledWithNullExchangeSetResponse_ThenReturnsSucceeded()
        {
            A.CallTo(() => _exchangeSetResponseFactory.CreateResponse(_pipelineContext.Job))
                .Returns(null!);

            var result = await _sendCallbackNotificationNode.ExecuteAsync(_executionContext);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Succeeded));
        }

        [Test]
        public async Task WhenPerformExecuteAsyncCalledSuccessfully_ThenUsesCancellationTokenFromEnvironment()
        {
            var customCancellationTokenSource = new CancellationTokenSource();
            var customToken = customCancellationTokenSource.Token;
            var customEnvironment = new CompletionNodeEnvironment(_configuration, customToken, _logger, BuilderExitCode.Success);
            var node = new SendCallbackNotificationNode(customEnvironment, _callbackNotificationService, _exchangeSetResponseFactory);
            var exchangeSetResponse = CreateExchangeSetResponse();

            A.CallTo(() => _exchangeSetResponseFactory.CreateResponse(_pipelineContext.Job))
                .Returns(exchangeSetResponse);

            await node.ExecuteAsync(_executionContext);

            A.CallTo(() => _callbackNotificationService.SendCallbackNotificationAsync(
                _pipelineContext.Job, exchangeSetResponse, customToken))
                .MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task WhenNodeExecutesEndToEndSuccessfully_ThenCompleteWorkflowExecutes()
        {
            var exchangeSetResponse = CreateExchangeSetResponse();

            A.CallTo(() => _exchangeSetResponseFactory.CreateResponse(_pipelineContext.Job))
                .Returns(exchangeSetResponse);
            A.CallTo(() => _callbackNotificationService.SendCallbackNotificationAsync(
                _pipelineContext.Job, exchangeSetResponse, _cancellationToken))
                .Returns(Task.CompletedTask);

            var shouldExecute = await _sendCallbackNotificationNode.ShouldExecuteAsync(_executionContext);

            NodeResult? result = null;

            if (shouldExecute)
            {
                result = await _sendCallbackNotificationNode.ExecuteAsync(_executionContext);
            }

            Assert.That(shouldExecute, Is.True);
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Status, Is.EqualTo(NodeResultStatus.Succeeded));

            A.CallTo(() => _exchangeSetResponseFactory.CreateResponse(_pipelineContext.Job))
                .MustHaveHappenedOnceExactly();
            A.CallTo(() => _callbackNotificationService.SendCallbackNotificationAsync(
                _pipelineContext.Job, exchangeSetResponse, _cancellationToken))
                .MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task WhenNodeShouldNotExecute_ThenExecuteIsNotCalled()
        {
            var jobWithNoCallback = CreateJobWithNoCallback();
            var contextWithNoCallback = new PipelineContext<S100Build>(jobWithNoCallback, new S100Build(), _storageService);

            A.CallTo(() => _executionContext.Subject).Returns(contextWithNoCallback);

            var shouldExecute = await _sendCallbackNotificationNode.ShouldExecuteAsync(_executionContext);

            Assert.That(shouldExecute, Is.False);

            A.CallTo(() => _exchangeSetResponseFactory.CreateResponse(A<Job>._))
                .MustNotHaveHappened();
            A.CallTo(() => _callbackNotificationService.SendCallbackNotificationAsync(
                A<Job>._, A<object>._, A<CancellationToken>._))
                .MustNotHaveHappened();
        }

        [Test]
        public void WhenPerformExecuteAsyncCalledWithJobNull_ThenThrowsNullReferenceException()
        {
            _pipelineContext = new PipelineContext<S100Build>(null!, new S100Build(), _storageService);

            A.CallTo(() => _executionContext.Subject).Returns(_pipelineContext);

            Assert.ThrowsAsync<NullReferenceException>(
                async () => await _sendCallbackNotificationNode.ExecuteAsync(_executionContext));
        }

        [Test]
        public async Task WhenPerformExecuteAsyncCalledMultipleTimes_ThenEachCallCreatesNewResponse()
        {
            var firstResponse = CreateExchangeSetResponse(
                statusLink: new Link { Uri = new Uri("https://example.com/status1") });
            var secondResponse = CreateExchangeSetResponse(
                statusLink: new Link { Uri = new Uri("https://example.com/status2") },
                expiryDate: DateTime.UtcNow.AddDays(2),
                requestedCount: ProductCount.From(10),
                exchangeSetCount: ProductCount.From(8));

            A.CallTo(() => _exchangeSetResponseFactory.CreateResponse(_pipelineContext.Job))
                .ReturnsNextFromSequence(firstResponse, secondResponse);

            var firstResult = await _sendCallbackNotificationNode.ExecuteAsync(_executionContext);
            var secondResult = await _sendCallbackNotificationNode.ExecuteAsync(_executionContext);

            Assert.That(firstResult.Status, Is.EqualTo(NodeResultStatus.Succeeded));
            Assert.That(secondResult.Status, Is.EqualTo(NodeResultStatus.Succeeded));

            A.CallTo(() => _exchangeSetResponseFactory.CreateResponse(_pipelineContext.Job))
                .MustHaveHappened(2, Times.Exactly);
            A.CallTo(() => _callbackNotificationService.SendCallbackNotificationAsync(
                _pipelineContext.Job, firstResponse, _cancellationToken))
                .MustHaveHappenedOnceExactly();
            A.CallTo(() => _callbackNotificationService.SendCallbackNotificationAsync(
                _pipelineContext.Job, secondResponse, _cancellationToken))
                .MustHaveHappenedOnceExactly();
        }

        private static Job CreateJob(CallbackUri callbackUri, BatchId batchId) => new Job
        {
            Id = TestJobId,
            Timestamp = DateTime.UtcNow,
            DataStandard = DataStandard.S100,
            RequestedProducts = new ProductNameList(),
            RequestedFilter = "",
            BatchId = batchId,
            CallbackUri = callbackUri,
            ExchangeSetType = ExchangeSetType.ProductNames,
            ProductIdentifier = DataStandardProduct.S101
        };

        private static Job CreateJobWithNoCallback() => CreateJob(CallbackUri.None, TestBatchId);
        private static Job CreateJobWithNoBatch() => CreateJob(TestCallbackUri, BatchId.None);

        private static CustomExchangeSetResponse CreateExchangeSetResponse(
            Link? statusLink = null,
            Link? detailsLink = null,
            DateTime? expiryDate = null,
            ProductCount? requestedCount = null,
            ProductCount? exchangeSetCount = null,
            ProductCount? alreadyUpToDateCount = null,
            MissingProductList? missingProducts = null,
            BatchId? batchId = null)
        {
            return new CustomExchangeSetResponse
            {
                Links = new ExchangeSetLinks
                {
                    ExchangeSetBatchStatusUri = statusLink ?? TestStatusLink,
                    ExchangeSetBatchDetailsUri = detailsLink ?? TestDetailsLink
                },
                ExchangeSetUrlExpiryDateTime = expiryDate ?? DateTime.UtcNow.AddDays(1),
                RequestedProductCount = requestedCount ?? ProductCount.From(5),
                ExchangeSetProductCount = exchangeSetCount ?? ProductCount.From(3),
                RequestedProductsAlreadyUpToDateCount = alreadyUpToDateCount ?? ProductCount.From(2),
                RequestedProductsNotInExchangeSet = missingProducts ?? new MissingProductList(),
                FssBatchId = batchId ?? TestBatchId
            };
        }
    }
}
