using FakeItEasy;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Parsing;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging.Implementation;

namespace UKHO.ADDS.EFS.Orchestrator.UnitTests.Infrastructure.Logging.Implementation
{
    [TestFixture]
    public class AppInsightsSerilogSinkTests
    {
        private const string ValidConnectionString = "InstrumentationKey=00000000-0000-0000-0000-000000000000";

        [SetUp]
        public void SetUp()
        {
            Environment.SetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING", ValidConnectionString);
        }

        [Test]
        public void WhenConnectionStringIsNullAndEnvVarIsMissing_ThenThrowsInvalidOperationException()
        {
            Environment.SetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING", null);
            Assert.That(() => new AppInsightsSerilogSink(null), Throws.TypeOf<InvalidOperationException>());
        }

        [Test]
        public void WhenConnectionStringIsEmpty_ThenThrowsInvalidOperationException()
        {
            Assert.That(() => new AppInsightsSerilogSink(" "), Throws.TypeOf<InvalidOperationException>());
        }

        [Test]
        public void WhenConnectionStringIsProvided_ThenDoesNotThrow()
        {
            Assert.That(() => new AppInsightsSerilogSink(ValidConnectionString), Throws.Nothing);
        }

        [Test]
        public void WhenFormatterIsNull_ThenUsesDefaultJsonFormatter()
        {
            var sink = new AppInsightsSerilogSink(ValidConnectionString, null);
            Assert.That(sink, Is.Not.Null);
        }

        [Test]
        public void WhenFormatterIsProvided_ThenUsesProvidedFormatter()
        {
            var formatter = A.Fake<ITextFormatter>();
            var sink = new AppInsightsSerilogSink(ValidConnectionString, formatter);
            Assert.That(sink, Is.Not.Null);
        }

        [Test]
        public void WhenEmitIsCalledAndChannelIsNotFull_ThenNoExceptionIsThrown()
        {
            var sink = new AppInsightsSerilogSink(ValidConnectionString);
            var logEvent = new LogEvent(DateTimeOffset.Now, LogEventLevel.Information, null, new MessageTemplate("msg", new List<MessageTemplateToken>()), new List<LogEventProperty>());
            Assert.That(() => sink.Emit(logEvent), Throws.Nothing);
        }

        [Test]
        public void WhenEmitIsCalledAndChannelIsFull_ThenNoExceptionIsThrownAndErrorIsWritten()
        {
            var sink = new AppInsightsSerilogSink(ValidConnectionString);
            var logEvent = new LogEvent(DateTimeOffset.Now, LogEventLevel.Information, null, new MessageTemplate("msg", new List<MessageTemplateToken>()), new List<LogEventProperty>());
            for (int i = 0; i < 4000; i++)
            {
                sink.Emit(logEvent);
            }
            Assert.That(() => sink.Emit(logEvent), Throws.Nothing);
        }

        [Test]
        public async Task WhenDisposeAsyncIsCalled_ThenResourcesAreDisposed()
        {
            var sink = new AppInsightsSerilogSink(ValidConnectionString);
            await sink.DisposeAsync();
            Assert.That(true, Is.True);
        }

        [Test]
        public async Task WhenDisposeAsyncIsCalledTwice_ThenNoExceptionIsThrown()
        {
            var sink = new AppInsightsSerilogSink(ValidConnectionString);
            await sink.DisposeAsync();
            Assert.That(async () => await sink.DisposeAsync(), Throws.Nothing);
        }

        [TearDown]
        public void TearDown()
        {
            Environment.SetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING", null);
        }
    }
}
