using System.Text;
using Azure.Messaging.EventHubs;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging.Implementation;

namespace UKHO.ADDS.EFS.Orchestrator.UnitTests.Implementation
{
    [TestFixture]
    internal class EventHubLogTests
    {
        private JsonSerializerSettings _jsonSettings;
        private IEventHubClientWrapper _fakeEventHubClient;
        private EventHubLog _eventHubLog;
        private byte[]? _sentBytes;

        [SetUp]
        public void Setup()
        {
            _jsonSettings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.None,
                MissingMemberHandling = MissingMemberHandling.Ignore,
                NullValueHandling = NullValueHandling.Ignore,
                Converters = new List<JsonConverter> { new ExceptionConverter() }
            };

            _fakeEventHubClient = A.Fake<IEventHubClientWrapper>();
            _eventHubLog = new EventHubLog(_fakeEventHubClient, Enumerable.Empty<System.Text.Json.Serialization.JsonConverter>());
            _sentBytes = null;

            A.CallTo(() => _fakeEventHubClient.SendAsync(A<EventData>.Ignored))
                .Invokes((EventData ed) => _sentBytes = ed.Body.ToArray());
        }

        private LogEntry CreateLogEntry(
            int eventId = 2,
            DateTime? timestamp = null,
            Exception? exception = null,
            Dictionary<string, object>? logProperties = null,
            string messageTemplate = "Hello this is a message template",
            string level = "Debug")
        {
            return new LogEntry
            {
                EventId = new EventId(eventId),
                Timestamp = timestamp ?? new DateTime(2002, 03, 04),
                Exception = exception ?? new InvalidOperationException("TestLoggedException"),
                LogProperties = logProperties ?? new Dictionary<string, object> { { "hi", "Guys" } },
                MessageTemplate = messageTemplate,
                Level = level
            };
        }

        private JObject GetLoggedJObject()
        {
            Assert.That(_sentBytes, Is.Not.Null, "sentBytes should not be null after SendAsync is called.");
            var sentString = Encoding.UTF8.GetString(_sentBytes!);
            return JObject.Parse(sentString);
        }

        [Test]
        public void WhenLogEntryIsValid_ThenSerializesJsonCorrectly()
        {
            var testLogEntry = CreateLogEntry();
            _eventHubLog.Log(testLogEntry);

            A.CallTo(() => _fakeEventHubClient.SendAsync(A<EventData>.Ignored)).MustHaveHappenedOnceExactly();

            var jObject = GetLoggedJObject();

            Assert.That(jObject["Timestamp"]?.ToObject<DateTime>(), Is.EqualTo(testLogEntry.Timestamp));
            Assert.That(jObject["MessageTemplate"]?.ToString(), Is.EqualTo(testLogEntry.MessageTemplate));
            Assert.That(jObject["Level"]?.ToString(), Is.EqualTo(testLogEntry.Level));
            Assert.That(jObject["Exception"]?["Message"]?.ToString(), Is.EqualTo(testLogEntry.Exception?.Message));
            Assert.That(jObject["Properties"]?["hi"]?.ToString(), Is.EqualTo("Guys"));
        }

        [Test]
        public void WhenPropertyThrowsException_ThenSerializesJsonWithThrowableProperty()
        {
            var logProperties = new Dictionary<string, object>
            {
                { "hi", "Guys" },
                { "throwable", new ObjectThatThrows() }
            };
            var testLogEntry = CreateLogEntry(logProperties: logProperties, level: "LogLevel");
            _eventHubLog.Log(testLogEntry);

            A.CallTo(() => _fakeEventHubClient.SendAsync(A<EventData>.Ignored)).MustHaveHappenedOnceExactly();

            var jObject = GetLoggedJObject();

            Assert.That(jObject["Timestamp"]?.ToObject<DateTime>(), Is.EqualTo(testLogEntry.Timestamp));
            Assert.That(jObject["MessageTemplate"]?.ToString(), Is.EqualTo(testLogEntry.MessageTemplate));
            Assert.That(jObject["Level"]?.ToString(), Is.EqualTo(testLogEntry.Level));
            Assert.That(jObject["Exception"]?["Message"]?.ToString(), Is.EqualTo(testLogEntry.Exception?.Message));
            Assert.That(jObject["Properties"]?["hi"]?.ToString(), Is.EqualTo("Guys"));
            Assert.That(jObject["Properties"]?["throwable"]?["NotThrowable"]?.ToString(), Is.EqualTo("NotThrowing"));
        }


        //[Test]
        //public void WhenLogEntryHasCircularReference_ThenHandlesCircularReferencesCorrectly()
        //{
        //    var testLogEntry = CreateLogEntry();
        //    testLogEntry.LogProperties.Add("circular", testLogEntry);
        //    _eventHubLog.Log(testLogEntry);

        //    A.CallTo(() => _fakeEventHubClient.SendAsync(A<EventData>.Ignored)).MustHaveHappenedOnceExactly();

        //    var jObject = GetLoggedJObject();

        //    Assert.That(jObject["Timestamp"]?.ToObject<DateTime>(), Is.EqualTo(testLogEntry.Timestamp));
        //    Assert.That(jObject["MessageTemplate"]?.ToString(), Is.EqualTo(testLogEntry.MessageTemplate));
        //    Assert.That(jObject["Level"]?.ToString(), Is.EqualTo(testLogEntry.Level));
        //    Assert.That(jObject["Exception"]?["Message"]?.ToString(), Is.EqualTo(testLogEntry.Exception?.Message));
        //}


        /*
        [Test]
        public void WhenPropertyThrowsException_ThenSerializesJsonAndLogsException()
        {
            var logProperties = new Dictionary<string, object>
            {
                { "hi", "Guys" },
                { "throwable", new ObjectThatThrowsAfterOneGet() }
            };
            var testLogEntry = CreateLogEntry(logProperties: logProperties, level: "LogLevel");
            _eventHubLog.Log(testLogEntry);
            A.CallTo(() => _fakeEventHubClient.SendAsync(A<EventData>.Ignored)).MustHaveHappenedOnceExactly();
            var jObject = GetLoggedJObject();
            Assert.That(jObject["Timestamp"]?.ToObject<DateTime>(), Is.Not.EqualTo(testLogEntry.Timestamp));
            Assert.That(jObject["MessageTemplate"]?.ToString(), Is.Not.EqualTo(testLogEntry.MessageTemplate));
            Assert.That(jObject["Exception"]?["Source"]?.ToString(), Is.EqualTo("Newtonsoft.Json"));
        }
        */

        [TearDown]
        public void TearDown()
        {
            _eventHubLog?.Dispose();
            _fakeEventHubClient?.Dispose();
        }
    }

    // Custom exception converter to handle serialization/deserialization of exceptions
    public class ExceptionConverter : JsonConverter<Exception>
    {
        public override void WriteJson(JsonWriter writer, Exception value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value);
        }

        public override Exception ReadJson(JsonReader reader, Type objectType, Exception existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            var jObject = JObject.Load(reader);
            string message = jObject["Message"]?.ToString() ?? "Unknown error";
            string source = jObject["Source"]?.ToString();
            var exception = new Exception(message);
            if (!string.IsNullOrEmpty(source))
                exception.Source = source;
            return exception;
        }
    }

    internal class ObjectThatThrows
    {
        public static string Throwable
        {
            get => throw new Exception("Thrown on throwable getter");
            set { }
        }

        public string NotThrowable { get; set; } = "NotThrowing";
    }

    internal class ObjectThatThrowsAfterOneGet
    {
        public string Throwable
        {
            get => throw new Exception("Thrown on throwable getter");
        }
    }
}
