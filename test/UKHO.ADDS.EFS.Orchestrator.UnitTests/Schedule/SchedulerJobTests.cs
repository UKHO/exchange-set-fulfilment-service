using FakeItEasy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Quartz;
using UKHO.ADDS.EFS.Builds;
using UKHO.ADDS.EFS.Jobs;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Assembly;
using UKHO.ADDS.EFS.Orchestrator.Schedule;

namespace UKHO.ADDS.EFS.Orchestrator.UnitTests.Schedule
{
    [TestFixture]
    public class SchedulerJobTests
    {
        private ILogger<SchedulerJob> _logger;
        private IConfiguration _config;
        private SchedulerJob _schedulerJob;
        private IJobExecutionContext _jobExecutionContext;
        private IAssemblyPipelineFactory _assemblyPipelineFactory;
        private IAssemblyPipeline _assemblyPipeline;
        private ITrigger _trigger;

        private static readonly JobId TestCorrelationId = JobId.From("job-12345678-1234-1234-1234-123456789abc");
        private static AssemblyPipelineResponse CreateExpectedResponse() => new()
        {
            JobId = TestCorrelationId,
            JobStatus = JobState.Completed,
            BuildStatus = BuildState.Succeeded,
            DataStandard = DataStandard.S100,
            BatchId = BatchId.From("test-batch-id")
        };

        [SetUp]
        public void SetUp()
        {
            _logger = A.Fake<ILogger<SchedulerJob>>();
            _config = A.Fake<IConfiguration>();
            _jobExecutionContext = A.Fake<IJobExecutionContext>();
            _assemblyPipelineFactory = A.Fake<IAssemblyPipelineFactory>();
            _assemblyPipeline = A.Fake<IAssemblyPipeline>();
            _trigger = A.Fake<ITrigger>();

            A.CallTo(() => _logger.IsEnabled(A<LogLevel>._)).Returns(true);

            A.CallTo(() => _jobExecutionContext.Trigger).Returns(_trigger);

            _schedulerJob = new SchedulerJob(_logger, _config, _assemblyPipelineFactory);
        }

        [Test]
        public void WhenParameterIsNull_ThenConstructorThrowsArgumentNullException()
        {
            var nullLoggerException =
                Assert.Throws<ArgumentNullException>(
                    () => new SchedulerJob(null, _config, _assemblyPipelineFactory));
            var nullConfigException =
                Assert.Throws<ArgumentNullException>(
                    () => new SchedulerJob(_logger, null, _assemblyPipelineFactory));
            var nullPipelineFactoryException =
                Assert.Throws<ArgumentNullException>(
                    () => new SchedulerJob(_logger, _config, null));

            Assert.That(nullLoggerException.ParamName, Is.EqualTo("logger"));
            Assert.That(nullConfigException.ParamName, Is.EqualTo("config"));
            Assert.That(nullPipelineFactoryException.ParamName, Is.EqualTo("pipelineFactory"));
        }

        [Test]
        public void WhenSchedulerJobExecutesSuccessfully_ThenShouldLogWithoutError()
        {
            var expectedResponse = CreateExpectedResponse();
            var nextFireTime = DateTimeOffset.UtcNow.AddHours(1);

            A.CallTo(() => _assemblyPipelineFactory.CreateAssemblyPipeline(A<AssemblyPipelineParameters>._))
                .Returns(_assemblyPipeline);

            A.CallTo(() => _assemblyPipeline.RunAsync(A<CancellationToken>._))
                .Returns(Task.FromResult(expectedResponse));

            A.CallTo(() => _trigger.GetNextFireTimeUtc()).Returns(nextFireTime);

            Assert.DoesNotThrowAsync(() => _schedulerJob.Execute(_jobExecutionContext));

            A.CallTo(() => _assemblyPipelineFactory.CreateAssemblyPipeline(
                A<AssemblyPipelineParameters>.That.Matches(p =>
                    p.Version == 1 &&
                    p.DataStandard == DataStandard.S100 &&
                    !p.Products.HasProducts &&
                    p.Filter == "")))
                .MustHaveHappenedOnceExactly();

            A.CallTo(() => _assemblyPipeline.RunAsync(CancellationToken.None))
                .MustHaveHappenedOnceExactly();

            A.CallTo(() => _logger.Log<LoggerMessageState>(
                    LogLevel.Information,
                    A<EventId>.That.Matches(e => e.Name == "LogSchedulerJobStarted"),
                    A<LoggerMessageState>._,
                    null,
                    A<Func<LoggerMessageState, Exception?, string>>._));

            A.CallTo(() => _logger.Log<LoggerMessageState>(
                    LogLevel.Information,
                    A<EventId>.That.Matches(e => e.Name == "LogSchedulerJobCompleted"),
                    A<LoggerMessageState>._,
                    null,
                    A<Func<LoggerMessageState, Exception?, string>>._));

            A.CallTo(() => _logger.Log<LoggerMessageState>(
                    LogLevel.Information,
                    A<EventId>.That.Matches(e => e.Name == "LogSchedulerJobNextRun"),
                    A<LoggerMessageState>._,
                    null,
                    A<Func<LoggerMessageState, Exception?, string>>._));
        }

        [Test]
        public void WhenSchedulerJobExecutesHasExceptionInPipelineCreation_ThenThrowAndLogsException()
        {
            var expectedException = new NotSupportedException("Unsupported data standard");

            A.CallTo(() => _assemblyPipelineFactory.CreateAssemblyPipeline(A<AssemblyPipelineParameters>._))
                .Throws(expectedException);

            var actualException = Assert.ThrowsAsync<NotSupportedException>(
                () => _schedulerJob.Execute(_jobExecutionContext));

            Assert.That(actualException, Is.Not.Null);

            A.CallTo(() => _logger.Log<LoggerMessageState>(
                    LogLevel.Information,
                    A<EventId>.That.Matches(e => e.Name == "LogSchedulerJobStarted"),
                    A<LoggerMessageState>._,
                    null,
                    A<Func<LoggerMessageState, Exception?, string>>._));

            A.CallTo(() => _logger.Log<LoggerMessageState>(
                    LogLevel.Error,
                    A<EventId>.That.Matches(e => e.Name == "LogSchedulerJobException"),
                    A<LoggerMessageState>._,
                    expectedException,
                    A<Func<LoggerMessageState, Exception?, string>>._))
                .MustHaveHappenedOnceExactly();
        }

        [Test]
        public void WhenSchedulerJobExecutesHasExceptionInPipelineRun_ThenThrowAndLogsException()
        {
            var expectedException = new OperationCanceledException("Pipeline execution was cancelled");

            A.CallTo(() => _assemblyPipelineFactory.CreateAssemblyPipeline(A<AssemblyPipelineParameters>._))
                .Returns(_assemblyPipeline);

            A.CallTo(() => _assemblyPipeline.RunAsync(A<CancellationToken>._))
                .Throws(expectedException);

            var actualException = Assert.ThrowsAsync<OperationCanceledException>(
                () => _schedulerJob.Execute(_jobExecutionContext));

            Assert.That(actualException, Is.Not.Null);

            A.CallTo(() => _logger.Log<LoggerMessageState>(
                    LogLevel.Information,
                    A<EventId>.That.Matches(e => e.Name == "LogSchedulerJobStarted"),
                    A<LoggerMessageState>._,
                    null,
                    A<Func<LoggerMessageState, Exception?, string>>._));

            A.CallTo(() => _logger.Log<LoggerMessageState>(
                    LogLevel.Error,
                    A<EventId>.That.Matches(e => e.Name == "LogSchedulerJobException"),
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

            var capturedCorrelationIds = new List<JobId>();

            A.CallTo(() => _assemblyPipelineFactory.CreateAssemblyPipeline(A<AssemblyPipelineParameters>._))
                .Invokes((AssemblyPipelineParameters parameters) => capturedCorrelationIds.Add(parameters.JobId))
                .Returns(_assemblyPipeline);

            await _schedulerJob.Execute(_jobExecutionContext);
            await _schedulerJob.Execute(_jobExecutionContext);
            await _schedulerJob.Execute(_jobExecutionContext);

            Assert.That(capturedCorrelationIds, Has.Count.EqualTo(3));
            Assert.That(capturedCorrelationIds[0], Is.Not.EqualTo(capturedCorrelationIds[1]));
            Assert.That(capturedCorrelationIds[1], Is.Not.EqualTo(capturedCorrelationIds[2]));
            Assert.That(capturedCorrelationIds[0], Is.Not.EqualTo(capturedCorrelationIds[2]));
        }
    }
}
