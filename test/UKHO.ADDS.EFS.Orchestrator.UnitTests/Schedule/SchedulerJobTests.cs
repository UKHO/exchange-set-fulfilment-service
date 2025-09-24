using FakeItEasy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Quartz;
using UKHO.ADDS.EFS.Domain.Builds;
using UKHO.ADDS.EFS.Domain.External;
using UKHO.ADDS.EFS.Domain.Jobs;
using UKHO.ADDS.EFS.Domain.Products;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Generators;
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
        private ICorrelationIdGenerator _correlationIdGenerator;

        private static readonly JobId TestJobId = JobId.From("sched-12345678-1234-1234-1234-123456789abc");
        private static readonly CorrelationId TestCorrelationId = CorrelationId.From("sched-12345678-1234-1234-1234-123456789abc");
        private static AssemblyPipelineResponse CreateExpectedResponse() => new()
        {
            JobId = TestJobId,
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
            _correlationIdGenerator = A.Fake<ICorrelationIdGenerator>();

            Environment.SetEnvironmentVariable("adds-environment", "local");
            A.CallTo(() => _logger.IsEnabled(A<LogLevel>._)).Returns(true);

            A.CallTo(() => _jobExecutionContext.Trigger).Returns(_trigger);

            _schedulerJob = new SchedulerJob(_logger, _config, _assemblyPipelineFactory, _correlationIdGenerator);
        }

        [Test]
        public void WhenParameterIsNull_ThenConstructorThrowsArgumentNullException()
        {
            var nullLoggerException =
                Assert.Throws<ArgumentNullException>(
                    () => new SchedulerJob(null, _config, _assemblyPipelineFactory, _correlationIdGenerator));
            var nullConfigException =
                Assert.Throws<ArgumentNullException>(
                    () => new SchedulerJob(_logger, null, _assemblyPipelineFactory, _correlationIdGenerator));
            var nullPipelineFactoryException =
                Assert.Throws<ArgumentNullException>(
                    () => new SchedulerJob(_logger, _config, null, _correlationIdGenerator));
            var nullCorrelationIdGeneratorException =
               Assert.Throws<ArgumentNullException>(
                   () => new SchedulerJob(_logger, _config, _assemblyPipelineFactory, null));

            Assert.That(nullLoggerException.ParamName, Is.EqualTo("logger"));
            Assert.That(nullConfigException.ParamName, Is.EqualTo("config"));
            Assert.That(nullPipelineFactoryException.ParamName, Is.EqualTo("pipelineFactory"));
            Assert.That(nullCorrelationIdGeneratorException.ParamName, Is.EqualTo("correlationIdGenerator"));
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

            A.CallTo(() => _correlationIdGenerator.CreateForScheduler()).Returns(TestCorrelationId);

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

            A.CallTo(() => _correlationIdGenerator.CreateForScheduler()).Returns(TestCorrelationId);

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

            A.CallTo(() => _correlationIdGenerator.CreateForScheduler()).Returns(TestCorrelationId);

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

        [TestCase("local")]
        [TestCase("dev")]
        [TestCase("vni")]
        [TestCase("vne")]
        [TestCase("iat")]
        public async Task WhenExecuteAsyncIsCalledMultipleTimes_ThenGeneratesUniqueCorrelationIds(string environment)
        {
            var previous = Environment.GetEnvironmentVariable("adds-environment");
            try
            {
                Environment.SetEnvironmentVariable("adds-environment", environment);

                var expectedResponse = CreateExpectedResponse();
                A.CallTo(() => _assemblyPipeline.RunAsync(A<CancellationToken>._))
                    .Returns(Task.FromResult(expectedResponse));

                var capturedCorrelationIds = new List<JobId>();
                A.CallTo(() => _assemblyPipelineFactory.CreateAssemblyPipeline(A<AssemblyPipelineParameters>._))
                    .Invokes((AssemblyPipelineParameters parameters) => capturedCorrelationIds.Add(parameters.JobId))
                    .Returns(_assemblyPipeline);

                for (var i = 0; i < 3; i++)
                {
                    var guid = Guid.NewGuid().ToString("N");
                    var testCorrelationId = (environment.StartsWith("loc") || environment.StartsWith("dev"))
                        ? CorrelationId.From($"sched-{guid}")
                        : CorrelationId.From(guid);

                    A.CallTo(() => _correlationIdGenerator.CreateForScheduler()).Returns(testCorrelationId);

                    await _schedulerJob.Execute(_jobExecutionContext);
                }

                Assert.That(capturedCorrelationIds, Has.Count.EqualTo(3));
                Assert.That(capturedCorrelationIds[0], Is.Not.EqualTo(capturedCorrelationIds[1]));
                Assert.That(capturedCorrelationIds[1], Is.Not.EqualTo(capturedCorrelationIds[2]));
                Assert.That(capturedCorrelationIds[0], Is.Not.EqualTo(capturedCorrelationIds[2]));
            }
            finally
            {
                Environment.SetEnvironmentVariable("adds-environment", previous);
            }
        }
    }
}
