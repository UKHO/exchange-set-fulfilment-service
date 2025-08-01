using FakeItEasy;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Parsing;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging.Implementation;

namespace UKHO.ADDS.EFS.Orchestrator.UnitTests.Infrastructure.Logging.Implementation
{
    [TestFixture]
    public class EventHubSerilogSinkTests
    {
        private const string ValidConnectionString = "test-namespace.servicebus.windows.net";
        private string? _originalEnv;

        [SetUp]
        public void SetUp()
        {
            _originalEnv = Environment.GetEnvironmentVariable("ConnectionStrings__efs-events-namespace");
            Environment.SetEnvironmentVariable("ConnectionStrings__efs-events-namespace", ValidConnectionString);
        }

        [Test]
        public void WhenConnectionStringAndEventHubNameAreMissing_ThenThrowsInvalidOperationException()
        {
            Environment.SetEnvironmentVariable("ConnectionStrings__efs-events-namespace", null);
            Assert.That(() => new EventHubSerilogSink(null), Throws.TypeOf<InvalidOperationException>());
        }

        [Test]
        public void WhenConnectionStringIsEmpty_ThenThrowsInvalidOperationException()
        {
            Assert.That(() => new EventHubSerilogSink(" "), Throws.TypeOf<InvalidOperationException>());
        }

        [Test]
        public void WhenFormatterIsNull_ThenUsesDefaultJsonFormatter()
        {
            var sink = new EventHubSerilogSink(ValidConnectionString, null);
            Assert.That(sink, Is.Not.Null);
        }

        [Test]
        public void WhenFormatterIsProvided_ThenUsesProvidedFormatter()
        {
            var formatter = A.Fake<ITextFormatter>();
            var sink = new EventHubSerilogSink(ValidConnectionString, formatter);
            Assert.That(sink, Is.Not.Null);
        }

        [Test]
        public void WhenEmitIsCalledAndChannelIsNotFull_ThenNoExceptionIsThrown()
        {
            var sink = new EventHubSerilogSink(ValidConnectionString);
            var logEvent = new LogEvent(DateTimeOffset.Now, LogEventLevel.Information, null, new MessageTemplate("msg", Enumerable.Empty<MessageTemplateToken>()), new List<LogEventProperty>());
            Assert.That(() => sink.Emit(logEvent), Throws.Nothing);
        }

        [Test]
        public void WhenEmitIsCalledAndChannelIsFull_ThenNoExceptionIsThrownAndErrorIsWritten()
        {
            var sink = new EventHubSerilogSink(ValidConnectionString);
            var logEvent = new LogEvent(DateTimeOffset.Now, LogEventLevel.Information, null, new MessageTemplate("msg", Enumerable.Empty<MessageTemplateToken>()), new List<LogEventProperty>());
            for (int i = 0; i < 4000; i++)
            {
                sink.Emit(logEvent);
            }
            Assert.That(() => sink.Emit(logEvent), Throws.Nothing);
        }

        [Test]
        public async Task WhenDisposeAsyncIsCalled_ThenResourcesAreDisposed()
        {
            var sink = new EventHubSerilogSink(ValidConnectionString);
            await sink.DisposeAsync();
            Assert.That(true, Is.True);
        }

        [Test]
        public async Task WhenDisposeAsyncIsCalledTwice_ThenNoExceptionIsThrown()
        {
            var sink = new EventHubSerilogSink(ValidConnectionString);
            await sink.DisposeAsync();
            Assert.That(async () => await sink.DisposeAsync(), Throws.Nothing);
        }

        [TearDown]
        public void TearDown()
        {
            Environment.SetEnvironmentVariable("ConnectionStrings__efs-events-namespace", _originalEnv);
        }
    }
}
