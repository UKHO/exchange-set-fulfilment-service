using System.Text;
using Azure.Storage.Blobs;
using FakeItEasy;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging.Implementation;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging.Implementation.AzureStorageEventLogging;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging.Implementation.AzureStorageEventLogging.Enums;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging.Implementation.AzureStorageEventLogging.Models;


namespace UKHO.ADDS.EFS.Orchestrator.UnitTests.Implementation.AzureStorageEventLogging
{
    /// <summary>
    /// Tests for the Azure Storage Event Logger functionality
    /// </summary>
    [TestFixture]
    internal class AzureStorageEventLoggerTests
    {
        private BlobContainerClient _blobContainerClient;
        private AzureStorageEventLogger _azureStorageLogger;
        private const string TestServiceName = "testService";
        private const string TestEnvironment = "testEnvironment";
        private const int MegabyteSize = 1024 * 1024;
        private const int HalfMegabyteSize = 512 * 1024;

        [SetUp]
        public void SetUp()
        {
            _blobContainerClient = A.Dummy<BlobContainerClient>();
            _azureStorageLogger = new AzureStorageEventLogger(_blobContainerClient);
        }

        #region String Formatting Tests

        [Test]
        public void WhenGeneratingServiceName_ThenReturnsCorrectlyFormattedString()
        {
            // Arrange
            var expected = $"{TestServiceName} - {TestEnvironment}";

            // Act
            var result = _azureStorageLogger.GenerateServiceName(TestServiceName, TestEnvironment);

            // Assert
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void WhenGeneratingPathForErrorBlob_ThenReturnsCorrectlyFormattedPath()
        {
            // Arrange
            var testDateTime = new DateTime(2021, 12, 10, 12, 13, 14);
            var expected = "2021\\12\\10\\12\\13\\14";

            // Act
            var result = _azureStorageLogger.GeneratePathForErrorBlob(testDateTime);

            // Assert
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void WhenGeneratingErrorBlobNameWithGuidOnly_ThenReturnsNameWithDefaultJsonExtension()
        {
            // Arrange
            var testGuid = Guid.NewGuid();
            var expected = $"{testGuid.ToString().Replace("-", "_")}.json";

            // Act
            var result = _azureStorageLogger.GenerateErrorBlobName(testGuid);

            // Assert
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void WhenGeneratingErrorBlobNameWithGuidAndExtension_ThenReturnsNameWithSpecifiedExtension()
        {
            // Arrange
            var testGuid = Guid.NewGuid();
            var extension = "json";
            var expected = $"{testGuid.ToString().Replace("-", "_")}.{extension}";

            // Act
            var result = _azureStorageLogger.GenerateErrorBlobName(testGuid, extension);

            // Assert
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void WhenGeneratingBlobFullName_ThenReturnsCombinedPath()
        {
            // Arrange
            var testGuid = Guid.NewGuid();
            var extension = "json";
            var path = "2021\\12\\10\\12\\13\\14";
            var blobName = $"{testGuid.ToString().Replace("-", "_")}.{extension}";
            var azureStorageBlobModel = new AzureStorageBlobFullNameModel(TestServiceName, path, blobName);
            var expected = Path.Combine(TestServiceName, path, blobName);

            // Act
            var result = _azureStorageLogger.GenerateBlobFullName(azureStorageBlobModel);

            // Assert
            Assert.That(result, Is.EqualTo(expected));
        }

        #endregion

        #region Storage Operation Tests

        [Test]
        public void WhenCancellingStorageOperationWithNoActiveOperation_ThenReturnsUnableToCancel()
        {
            // Act
            var result = _azureStorageLogger.CancelLogFileStoringOperation();

            // Assert
            Assert.That(result, Is.EqualTo(AzureStorageEventLogCancellationResult.UnableToCancel));
        }

        [Test]
        public void WhenCancellingActiveStorageOperation_ThenCancelsSuccessfully()
        {
            // Arrange
            var azureStorageModel = new AzureStorageEventModel(
                "test service - test environment/day/test.json",
                GenerateTestMessage(HalfMegabyteSize));

            // Act
            _azureStorageLogger.StoreLogFile(azureStorageModel, true);
            var result = _azureStorageLogger.CancelLogFileStoringOperation();

            // Assert
            Assert.That(result, Is.EqualTo(AzureStorageEventLogCancellationResult.Successful));
        }

        [Test]
        public void WhenCancellingActiveStorageOperationWithNullToken_ThenCancellationFails()
        {
            // Arrange
            var azureStorageModel = new AzureStorageEventModel(
                "test service - test environment/day/test.json",
                GenerateTestMessage(HalfMegabyteSize));

            // Act
            _azureStorageLogger.StoreLogFile(azureStorageModel, true);
            _azureStorageLogger.NullifyTokenSource();
            var result = _azureStorageLogger.CancelLogFileStoringOperation();

            // Assert
            Assert.That(result, Is.EqualTo(AzureStorageEventLogCancellationResult.CancellationFailed));
        }

        //[Test]
        //public void WhenMessageIsGreaterThan1MB_ThenStoresInAzureStorage()
        //{
        //    // Arrange
        //    var fakeEventHubClient = A.Fake<IEventHubClientWrapper>();
        //    var service = "test service";
        //    var environment = "test environment";
        //    var testLogProperties = new Dictionary<string, object> { { "_Service", service }, { "_Environment", environment } };
        //    var testDateStamp = new DateTime(2002, 03, 04);
        //    byte[] sentBytes = null;
        //    A.CallTo(() => fakeEventHubClient.SendAsync(A<EventData>.Ignored))
        //        .Invokes((EventData ed) => sentBytes = ed.Body.ToArray());

        //    var template = GenerateTestMessage(MegabyteSize);
        //    var eventHubLog = new EventHubLog(fakeEventHubClient, Enumerable.Empty<System.Text.Json.Serialization.JsonConverter>());

        //    var testLogEntry = new LogEntry
        //    {
        //        EventId = new EventId(2),
        //        Timestamp = testDateStamp,
        //        Exception = new InvalidOperationException("TestLoggedException"),
        //        LogProperties = testLogProperties,
        //        MessageTemplate = template,
        //        Level = "LogLevel"
        //    };

        //    // Act
        //    eventHubLog.Log(testLogEntry);

        //    // Assert
        //    A.CallTo(() => fakeEventHubClient.SendAsync(A<EventData>.Ignored)).MustHaveHappenedOnceExactly();

        //    var sentString = Encoding.UTF8.GetString(sentBytes);
        //    var sentLogEntry = JsonConvert.DeserializeObject<LogEntry>(sentString);

        //    Assert.That(sentLogEntry.Timestamp, Is.EqualTo(testLogEntry.Timestamp));
        //    Assert.Multiple(() =>
        //    {
        //        Assert.That(sentLogEntry.LogProperties, Is.EquivalentTo(testLogEntry.LogProperties));
        //        Assert.That(sentLogEntry.EventId, Is.EqualTo(testLogEntry.EventId));
        //        Assert.That(sentLogEntry.Level, Is.EqualTo(testLogEntry.Level));
        //        Assert.That(sentLogEntry.MessageTemplate.StartsWith("Azure Storage Logging:"), Is.True);
        //        Assert.That(sentLogEntry.Exception.Message.StartsWith("Azure Storage Logging:"), Is.True);
        //    });
        //}

        //[Test]
        //public void WhenMessageIsExactly1MB_ThenStoresInAzureStorage()
        //{
        //    // Arrange
        //    var fakeEventHubClient = A.Fake<IEventHubClientWrapper>();
        //    var service = "test service";
        //    var environment = "test environment";
        //    var testLogProperties = new Dictionary<string, object> { { "_Service", service }, { "_Environment", environment } };
        //    var testDateStamp = new DateTime(2002, 03, 04);
        //    byte[] sentBytes = Array.Empty<byte>();

        //    A.CallTo(() => fakeEventHubClient.SendAsync(A<EventData>.Ignored))
        //        .Invokes((EventData ed) => sentBytes = ed.Body.ToArray());

        //    var eventHubLog = new EventHubLog(fakeEventHubClient, Enumerable.Empty<System.Text.Json.Serialization.JsonConverter>());
        //    var testLogEntry = new LogEntry
        //    {
        //        EventId = new EventId(2),
        //        Timestamp = testDateStamp,
        //        Exception = new InvalidOperationException("TestLoggedException"),
        //        LogProperties = testLogProperties,
        //        MessageTemplate = string.Empty,
        //        Level = "LogLevel"
        //    };

        //    var template = CreateMessageEqualTo1Mb(testLogEntry);
        //    testLogEntry.MessageTemplate = template;

        //    // Act
        //    eventHubLog.Log(testLogEntry);

        //    // Assert
        //    A.CallTo(() => fakeEventHubClient.SendAsync(A<EventData>.Ignored)).MustHaveHappenedOnceExactly();

        //    var sentString = Encoding.UTF8.GetString(sentBytes);
        //    var sentLogEntry = JsonConvert.DeserializeObject<LogEntry>(sentString);

        //    Assert.Multiple(() =>
        //    {
        //        Assert.That(sentLogEntry.Timestamp, Is.EqualTo(testLogEntry.Timestamp));
        //        Assert.That(sentLogEntry.LogProperties, Is.EquivalentTo(testLogEntry.LogProperties));
        //        Assert.That(sentLogEntry.EventId, Is.EqualTo(testLogEntry.EventId));
        //        Assert.That(sentLogEntry.Level, Is.EqualTo(testLogEntry.Level));
        //        Assert.That(sentLogEntry.MessageTemplate.StartsWith("Azure Storage Logging:"), Is.True);
        //        Assert.That(sentLogEntry.Exception.Message.StartsWith("Azure Storage Logging:"), Is.True);
        //    });
        //}

        //[Test]
        //public void WhenMessageIsLessThan1MB_ThenSendsDirectlyToEventHub()
        //{
        //    // Arrange
        //    var fakeEventHubClient = A.Fake<IEventHubClientWrapper>();
        //    var service = "test service";
        //    var environment = "test environment";
        //    var testLogProperties = new Dictionary<string, object> { { "_Service", service }, { "_Environment", environment } };
        //    var testDateStamp = new DateTime(2002, 03, 04);
        //    byte[] sentBytes = null;

        //    A.CallTo(() => fakeEventHubClient.SendAsync(A<EventData>.Ignored))
        //        .Invokes((EventData ed) => sentBytes = ed.Body.ToArray());

        //    var template = GenerateTestMessage(HalfMegabyteSize);
        //    var eventHubLog = new EventHubLog(fakeEventHubClient, Enumerable.Empty<System.Text.Json.Serialization.JsonConverter>());

        //    var testLogEntry = new LogEntry
        //    {
        //        EventId = new EventId(2),
        //        Timestamp = testDateStamp,
        //        Exception = new InvalidOperationException("TestLoggedException"),
        //        LogProperties = testLogProperties,
        //        MessageTemplate = template,
        //        Level = "LogLevel"
        //    };

        //    // Act
        //    eventHubLog.Log(testLogEntry);

        //    // Assert
        //    A.CallTo(() => fakeEventHubClient.SendAsync(A<EventData>.Ignored)).MustHaveHappenedOnceExactly();

        //    var sentString = Encoding.UTF8.GetString(sentBytes);
        //    var sentLogEntry = JsonConvert.DeserializeObject<LogEntry>(sentString);

        //    Assert.Multiple(() =>
        //    {
        //        Assert.That(sentLogEntry.Timestamp, Is.EqualTo(testLogEntry.Timestamp));
        //        Assert.That(sentLogEntry.LogProperties, Is.EquivalentTo(testLogEntry.LogProperties));
        //        Assert.That(sentLogEntry.EventId, Is.EqualTo(testLogEntry.EventId));
        //        Assert.That(sentLogEntry.Level, Is.EqualTo(testLogEntry.Level));
        //        Assert.That(sentLogEntry.LogProperties.Count, Is.EqualTo(2));
        //        Assert.That(sentLogEntry.LogProperties.First(), Is.EqualTo(testLogEntry.LogProperties.First()));
        //        Assert.That(sentLogEntry.MessageTemplate, Is.EqualTo(template));
        //        Assert.That(sentLogEntry.Exception.Message, Is.EqualTo(testLogEntry.Exception.Message));
        //    });
        //}

        #endregion

        #region Helper Methods

        /// <summary>
        /// Generates a random string message of specified size
        /// </summary>
        /// <param name="size">Size of the message in characters</param>
        /// <returns>A random string of specified length</returns>
        private static string GenerateTestMessage(int size)
        {
            const string charsPool = "ABCDEFGHJKLMNOPQRSTVUWXYZ1234567890";
            var rand = new Random();

            return new string(Enumerable.Range(0, size)
                .Select(_ => charsPool[rand.Next(charsPool.Length)])
                .ToArray());
        }

        /// <summary>
        /// Gets the size of a LogEntry object when serialized to JSON
        /// </summary>
        /// <param name="entry">The LogEntry to measure</param>
        /// <returns>Size in bytes of the serialized object</returns>
        private static int GetSizeOfJsonObject(LogEntry entry)
        {
            var jsonOptions = new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };

            return Encoding.UTF8.GetBytes(
                System.Text.Json.JsonSerializer.Serialize(entry, jsonOptions)).Length;
        }

        /// <summary>
        /// Creates a message that, when added to the LogEntry, will make the total size exactly 1MB
        /// </summary>
        /// <param name="entry">The LogEntry to supplement</param>
        /// <returns>A string of appropriate length</returns>
        private static string CreateMessageEqualTo1Mb(LogEntry entry)
        {
            return GenerateTestMessage(MegabyteSize - GetSizeOfJsonObject(entry));
        }

        #endregion
    }
}
