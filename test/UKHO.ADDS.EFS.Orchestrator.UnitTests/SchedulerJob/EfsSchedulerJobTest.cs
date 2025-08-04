using FakeItEasy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Quartz;
using UKHO.ADDS.EFS.Builds;
using UKHO.ADDS.EFS.Jobs;
using UKHO.ADDS.EFS.Orchestrator.Jobs;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Assembly;
using UKHO.ADDS.EFS.Orchestrator.SchedulerJob;

namespace UKHO.ADDS.EFS.Orchestrator.UnitTests.SchedulerJob
{
    [TestFixture]
    public class EfsSchedulerJobTest
    {
        private ILogger<EfsSchedulerJob> _logger;
        private IConfiguration _config;
        private EfsSchedulerJob _efsSchedulerJob;
        private IJobExecutionContext _jobExecutionContext;
        private IAssemblyPipelineFactory _assemblyPipelineFactory;
        private IAssemblyPipeline _assemblyPipeline;
        private ITrigger _trigger;
        private const string TestCorrelationId = "job-12345678-1234-1234-1234-123456789abc";
        private static AssemblyPipelineResponse CreateExpectedResponse() => new()
        {
            JobId = TestCorrelationId,
            JobStatus = JobState.Completed,
            BuildStatus = BuildState.Succeeded,
            DataStandard = DataStandard.S100,
            BatchId = "test-batch-id"
        };

        [SetUp]
        public void SetUp()
        {
            _logger = A.Fake<ILogger<EfsSchedulerJob>>();
            _config = A.Fake<IConfiguration>();
            _jobExecutionContext = A.Fake<IJobExecutionContext>();
            _assemblyPipelineFactory = A.Fake<IAssemblyPipelineFactory>();
            _assemblyPipeline = A.Fake<IAssemblyPipeline>();
            _trigger = A.Fake<ITrigger>();

            A.CallTo(() => _logger.IsEnabled(A<LogLevel>._)).Returns(true);

            A.CallTo(() => _jobExecutionContext.Trigger).Returns(_trigger);

            _efsSchedulerJob = new EfsSchedulerJob(_logger, _config, _assemblyPipelineFactory);
        }

        [Test]
        public void WhenParameterIsNull_ThenConstructorThrowsArgumentNullException()
        {
            var nullLoggerException =
                Assert.Throws<ArgumentNullException>(
                    () => new EfsSchedulerJob(null, _config, _assemblyPipelineFactory));
            var nullConfigException =
                Assert.Throws<ArgumentNullException>(
                    () => new EfsSchedulerJob(_logger, null, _assemblyPipelineFactory));
            var nullPipelineFactoryException =
                Assert.Throws<ArgumentNullException>(
                    () => new EfsSchedulerJob(_logger, _config, null));

            Assert.That(nullLoggerException.ParamName, Is.EqualTo("logger"));
            Assert.That(nullConfigException.ParamName, Is.EqualTo("config"));
            Assert.That(nullPipelineFactoryException.ParamName, Is.EqualTo("pipelineFactory"));
        }

        [Test]
        public void WhenEfsSchedulerJobExecutesSuccessfully_ThenShouldLogWithoutError()
        {
            var expectedResponse = CreateExpectedResponse();
            var nextFireTime = DateTimeOffset.UtcNow.AddHours(1);

            A.CallTo(() => _assemblyPipelineFactory.CreateAssemblyPipeline(A<AssemblyPipelineParameters>._))
                .Returns(_assemblyPipeline);

            A.CallTo(() => _assemblyPipeline.RunAsync(A<CancellationToken>._))
                .Returns(Task.FromResult(expectedResponse));

            A.CallTo(() => _trigger.GetNextFireTimeUtc()).Returns(nextFireTime);

            Assert.DoesNotThrowAsync(() => _efsSchedulerJob.Execute(_jobExecutionContext));

            A.CallTo(() => _assemblyPipelineFactory.CreateAssemblyPipeline(
                A<AssemblyPipelineParameters>.That.Matches(p =>
                    p.Version == 1 &&
                    p.DataStandard == DataStandard.S100 &&
                    p.Products == "" &&
                    p.Filter == "")))
                .MustHaveHappenedOnceExactly();

            A.CallTo(() => _assemblyPipeline.RunAsync(CancellationToken.None))
                .MustHaveHappenedOnceExactly();

            A.CallTo(() => _logger.Log<LoggerMessageState>(
                    LogLevel.Information,
                    A<EventId>.That.Matches(e => e.Name == "LogEfsSchedulerJobStarted"),
                    A<LoggerMessageState>._,
                    null,
                    A<Func<LoggerMessageState, Exception?, string>>._));

            A.CallTo(() => _logger.Log<LoggerMessageState>(
                    LogLevel.Information,
                    A<EventId>.That.Matches(e => e.Name == "LogEfsSchedulerJobCompleted"),
                    A<LoggerMessageState>._,
                    null,
                    A<Func<LoggerMessageState, Exception?, string>>._));

            A.CallTo(() => _logger.Log<LoggerMessageState>(
                    LogLevel.Information,
                    A<EventId>.That.Matches(e => e.Name == "LogEfsSchedulerJobNextRun"),
                    A<LoggerMessageState>._,
                    null,
                    A<Func<LoggerMessageState, Exception?, string>>._));
        }

        [Test]
        public void WhenEfsSchedulerJobExecutesHasExceptionInPipelineCreation_ThenThrowAndLogsException()
        {
            var expectedException = new NotSupportedException("Unsupported data standard");

            A.CallTo(() => _assemblyPipelineFactory.CreateAssemblyPipeline(A<AssemblyPipelineParameters>._))
                .Throws(expectedException);

            var actualException = Assert.ThrowsAsync<NotSupportedException>(
                () => _efsSchedulerJob.Execute(_jobExecutionContext));

            Assert.That(actualException, Is.Not.Null);

            A.CallTo(() => _logger.Log<LoggerMessageState>(
                    LogLevel.Information,
                    A<EventId>.That.Matches(e => e.Name == "LogEfsSchedulerJobStarted"),
                    A<LoggerMessageState>._,
                    null,
                    A<Func<LoggerMessageState, Exception?, string>>._));

            A.CallTo(() => _logger.Log<LoggerMessageState>(
                    LogLevel.Error,
                    A<EventId>.That.Matches(e => e.Name == "LogEfsSchedulerJobException"),
                    A<LoggerMessageState>._,
                    expectedException,
                    A<Func<LoggerMessageState, Exception?, string>>._))
                .MustHaveHappenedOnceExactly();
        }

        [Test]
        public void WhenEfsSchedulerJobExecutesHasExceptionInPipelineRun_ThenThrowAndLogsException()
        {
            var expectedException = new OperationCanceledException("Pipeline execution was cancelled");

            A.CallTo(() => _assemblyPipelineFactory.CreateAssemblyPipeline(A<AssemblyPipelineParameters>._))
                .Returns(_assemblyPipeline);

            A.CallTo(() => _assemblyPipeline.RunAsync(A<CancellationToken>._))
                .Throws(expectedException);

            var actualException = Assert.ThrowsAsync<OperationCanceledException>(
                () => _efsSchedulerJob.Execute(_jobExecutionContext));

            Assert.That(actualException, Is.Not.Null);

            A.CallTo(() => _logger.Log<LoggerMessageState>(
                    LogLevel.Information,
                    A<EventId>.That.Matches(e => e.Name == "LogEfsSchedulerJobStarted"),
                    A<LoggerMessageState>._,
                    null,
                    A<Func<LoggerMessageState, Exception?, string>>._));

            A.CallTo(() => _logger.Log<LoggerMessageState>(
                    LogLevel.Error,
                    A<EventId>.That.Matches(e => e.Name == "LogEfsSchedulerJobException"),
                    A<LoggerMessageState>._,
                    expectedException,
                    A<Func<LoggerMessageState, Exception?, string>>._))
                .MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task WhenExecuteAsyncIsCalledMultipleTimes_ThenGeneratesUniqueCorrelationIds()
        {
            var expectedResponse = CreateExpectedResponse();

            A.CallTo(() => _assemblyPipeline.RunAsync(A<CancellationToken>._))
                .Returns(Task.FromResult(expectedResponse));

            var capturedCorrelationIds = new List<string>();

            A.CallTo(() => _assemblyPipelineFactory.CreateAssemblyPipeline(A<AssemblyPipelineParameters>._))
                .Invokes((AssemblyPipelineParameters parameters) => capturedCorrelationIds.Add(parameters.JobId))
                .Returns(_assemblyPipeline);

            await _efsSchedulerJob.Execute(_jobExecutionContext);
            await _efsSchedulerJob.Execute(_jobExecutionContext);
            await _efsSchedulerJob.Execute(_jobExecutionContext);

            Assert.That(capturedCorrelationIds, Has.Count.EqualTo(3));
            Assert.That(capturedCorrelationIds[0], Is.Not.EqualTo(capturedCorrelationIds[1]));
            Assert.That(capturedCorrelationIds[1], Is.Not.EqualTo(capturedCorrelationIds[2]));
            Assert.That(capturedCorrelationIds[0], Is.Not.EqualTo(capturedCorrelationIds[2]));

            foreach (var correlationId in capturedCorrelationIds)
            {
                Assert.That(correlationId, Does.StartWith("job-"));
                Assert.That(correlationId.Length, Is.EqualTo(36));
            }
        }
    }
}
