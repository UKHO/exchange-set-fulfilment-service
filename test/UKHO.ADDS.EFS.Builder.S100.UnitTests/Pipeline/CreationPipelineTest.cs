﻿using FakeItEasy;
using Microsoft.Extensions.Logging;
using UKHO.ADDS.EFS.Builder.S100.IIC;
using UKHO.ADDS.EFS.Builder.S100.IIC.Models;
using UKHO.ADDS.EFS.Builder.S100.Pipelines;
using UKHO.ADDS.EFS.Builder.S100.Services;
using UKHO.ADDS.EFS.Entities;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;
using UKHO.ADDS.Infrastructure.Results;

namespace UKHO.ADDS.EFS.Builder.S100.UnitTests.Pipeline
{
    [TestFixture]
    internal class CreationPipelineTest
    {
        private IToolClient _toolClient;
        private INodeStatusWriter _nodeStatusWriter;
        private CreationPipeline _creationPipeline;
        private IExecutionContext<ExchangeSetPipelineContext> _context;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _toolClient = A.Fake<IToolClient>();
            _nodeStatusWriter = A.Fake<INodeStatusWriter>();
            _context = A.Fake<IExecutionContext<ExchangeSetPipelineContext>>();
            _creationPipeline = new CreationPipeline();
        }

        [SetUp]
        public void Setup()
        {
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            var exchangeSetPipelineContext = new ExchangeSetPipelineContext(null, _nodeStatusWriter, _toolClient, loggerFactory)
            {
                Job = new ExchangeSetJob { CorrelationId = "TestCorrelationId" },
                JobId = "TestJobId",
                WorkspaceAuthenticationKey = "Test123"
            };

            A.CallTo(() => _context.Subject).Returns(exchangeSetPipelineContext);
        }

        [Test]
        public async Task WhenAllPipelineNodesSucceed_ThenReturnsSuccessNodeResult()
        {
            var fakeAddExchangeSetResult = A.Fake<IResult<OperationResponse>>();
            var opResponse = new OperationResponse { Code = 0, Type = "Success", Message = "OK" };
            IError? error = null;
            A.CallTo(() => fakeAddExchangeSetResult.IsSuccess(out opResponse, out error)).Returns(true);
            A.CallTo(() => _toolClient.AddExchangeSetAsync(_context.Subject.JobId, _context.Subject.WorkspaceAuthenticationKey, _context.Subject.Job.CorrelationId)).Returns(Task.FromResult(fakeAddExchangeSetResult));

            var fakeAddContentResult = A.Fake<IResult<OperationResponse>>();
            A.CallTo(() => fakeAddContentResult.IsSuccess(out opResponse, out error)).Returns(true);
            A.CallTo(() => _toolClient.AddContentAsync(A<string>._, A<string>._, A<string>._, A<string>._))
                .Returns(Task.FromResult(fakeAddContentResult));

            var fakeSignResult = A.Fake<IResult<SigningResponse>>();
            SigningResponse signingResponse = new() { Certificate = "cert", SigningKey = "key", Status = "Success" };
            IError? signError = null;
            A.CallTo(() => fakeSignResult.IsSuccess(out signingResponse, out signError)).Returns(true);
            A.CallTo(() => _toolClient.SignExchangeSetAsync(A<string>._, A<string>._, A<string>._)).Returns(Task.FromResult(fakeSignResult));

            var result = await _creationPipeline.ExecutePipeline(_context.Subject);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Succeeded));

            A.CallTo(() => _toolClient.AddExchangeSetAsync(A<string>._, A<string>._, A<string>._))
                .MustHaveHappened();

            A.CallTo(() => _toolClient.AddContentAsync(A<string>._, A<string>._, A<string>._, A<string>._))
               .MustHaveHappened();

            A.CallTo(() => _toolClient.SignExchangeSetAsync(A<string>._, A<string>._, A<string>._))
                .MustHaveHappened();
        }

        [Test]
        public void WhenContextIsNull_ThenThrowsArgumentException()
        {
            Assert.That(async () => await _creationPipeline.ExecutePipeline(null), Throws.ArgumentException);
        }

        [TestCase("", "Test123", "TestCorrelationId")]
        [TestCase("TestJobId", "", "TestCorrelationId")]
        [TestCase("TestJobId", "Test123", "")]
        public async Task WhenRequiredContextPropertiesAreNull_ThenReturnsFailedNodeResult(string jobId, string authKey, string correlationId)
        {
            _context.Subject.JobId = jobId;
            _context.Subject.WorkspaceAuthenticationKey = authKey;
            _context.Subject.Job = new ExchangeSetJob { CorrelationId = correlationId };
            var result = await _creationPipeline.ExecutePipeline(_context.Subject);
            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Failed));
        }

        [Test]
        public async Task WhenJobIsNull_ThenReturnsFailedNodeResult()
        {
            _context.Subject.Job = null;
            var result = await _creationPipeline.ExecutePipeline(_context.Subject);
            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Failed));
        }

        [Test]
        public async Task WhenCreateExchangeSetNodeFails_ThenReturnsFailedNodeResult()
        {
            A.CallTo(() => _toolClient.AddExchangeSetAsync("TestJobId", "Test123", "TestCorrelationId"))
                .Throws<Exception>();
            var result = await _creationPipeline.ExecutePipeline(_context.Subject);
            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Failed));
        }

        [Test]
        public async Task WhenAddContentExchangeSetNodeFails_ThenReturnsFailedNodeResult()
        {
            A.CallTo(() => _toolClient.AddContentAsync(A<string>._, A<string>._, A<string>._, A<string>._))
                .Throws<Exception>();
            var result = await _creationPipeline.ExecutePipeline(_context.Subject);
            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Failed));
        }

        [Test]
        public async Task WhenSignExchangeSetNodeFails_ThenReturnsFailedNodeResult()
        {
            A.CallTo(() => _toolClient.SignExchangeSetAsync("TestJobId", "Test123", "TestCorrelationId"))
                .Throws<Exception>();
            var result = await _creationPipeline.ExecutePipeline(_context.Subject);
            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Failed));
        }
    }
}
