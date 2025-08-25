using System.Text.Json;
using Azure;
using Azure.Core;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging.Implementation;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging.Implementation.AzureStorageEventLogging.Enums;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging.Implementation.AzureStorageEventLogging.Extensions;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging.Implementation.AzureStorageEventLogging.Models;
using UKHO.ADDS.EFS.Orchestrator.UnitTests.Implementation.AzureStorageEventLogging.Factories;

namespace UKHO.ADDS.EFS.Orchestrator.UnitTests.Implementation.AzureStorageEventLogging.Extensions
{
    /// <summary>
    ///     Tests for the Azure storage event logger extensions
    /// </summary>
    [TestFixture]
    public class AzureStorageEventLoggerExtensionsTests
    {
        private const string ValidUriString = "https://test.com/";
        private const int OneMegabyte = 1024 * 1024;
        private const int HalfMegabyte = OneMegabyte / 2;
        private const int SlightlyOverOneMegabyte = OneMegabyte + 1;
        private const int DefaultFileSize = 1024;
        private const int DefaultMbSize = 1;
        private const string AzureStorageLoggingSuccessTemplate = "Azure Storage Logging: A blob with the error details was created at {{BlobFullName}}. Reason: ErrorMessageEqualOrGreaterTo1MB ResponseMessage: {{ReasonPhrase}} ResponseCode: {{StatusCode}} RequestId: {{RequestId}} Sha256: {{FileSHA}} FileSize(Bs): {{FileSize}} FileModifiedDate: {{ModifiedDate}}";

        private ResourcesFactory _resourcesFactory;
        private JsonSerializerOptions _jsonOptions;

        [SetUp]
        public void SetUp()
        {
            _resourcesFactory = new ResourcesFactory();
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };
        }

        private AzureStorageLogProviderOptions GetAzureStorageLogProviderOptions(bool azureStorageEnabled, bool isManagedIdentity = false)
        {
            if (isManagedIdentity)
            {
                return new AzureStorageLogProviderOptions(
                    new Uri(ValidUriString),
                    A.Fake<TokenCredential>(),
                    azureStorageEnabled,
                    _resourcesFactory.SuccessTemplateMessage,
                    _resourcesFactory.FailureTemplateMessage);
            }

            return new AzureStorageLogProviderOptions(
                ValidUriString,
                azureStorageEnabled,
                _resourcesFactory.SuccessTemplateMessage,
                _resourcesFactory.FailureTemplateMessage);
        }

        private string GenerateTestMessage(int size)
        {
            var charsPool = "ABCDEFGHJKLMNOPQRSTVUWXYZ1234567890";
            var charsArray = new char[size];
            var rand = new Random();

            for (var c = 0; c < charsArray.Length; c++)
                charsArray[c] = charsPool[rand.Next(charsPool.Length)];

            return new string(charsArray);
        }

        #region GetFileSize Tests

        [Test]
        public void WhenResponseIsNull_ThenGetFileSizeReturnsDefaultValue()
        {
            Response response = null;
            var result = response.GetFileSize(DefaultFileSize);
            Assert.That(result, Is.EqualTo(DefaultFileSize));
        }

        [Test]
        public void WhenResponseStreamIsNull_ThenGetFileSizeReturnsDefaultValue()
        {
            var response = A.Fake<Response>();

            var result = response.GetFileSize(DefaultFileSize);
            Assert.That(result, Is.EqualTo(DefaultFileSize));
        }

        #endregion

        #region GetLogEntryPropertyValue Tests

        [Test]
        public void WhenKeyExists_ThenGetLogEntryPropertyValueReturnsValue()
        {
            var key = "test_key_exists";
            var expectedValue = "test_value";
            var dictionary = new Dictionary<string, object> { { key, expectedValue } };

            var result = dictionary.GetLogEntryPropertyValue(key);

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result, Is.EqualTo(expectedValue));
            });
        }

        [Test]
        public void WhenKeyDoesNotExist_ThenGetLogEntryPropertyValueReturnsNull()
        {
            var dictionary = new Dictionary<string, object> { { "test_key_not_exists", "test_value" } };

            var result = dictionary.GetLogEntryPropertyValue("test_key_exists");

            Assert.That(result, Is.Null);
        }

        #endregion

        #region GetModifiedDate Tests

        [Test]
        public void WhenResponseHeaderIsNull_ThenGetModifiedDateReturnsNull()
        {
            Response response = null;
            var result = response.GetModifiedDate();
            Assert.That(result, Is.Null);
        }

        [Test]
        public void WhenResponseStreamIsNull_ThenGetModifiedDateReturnsNull()
        {
            var response = A.Fake<Response>();
            var result = response.GetModifiedDate();
            Assert.That(result, Is.Null);
        }

        #endregion

        #region IsLongMessage Tests

        [Test]
        public void WhenMessageIsEqualToOneMb_ThenIsLongMessageReturnsTrue()
        {
            var message = GenerateTestMessage(OneMegabyte);
            var result = message.IsLongMessage(DefaultMbSize);
            Assert.That(result, Is.True);
        }

        [Test]
        public void WhenMessageIsGreaterThanOneMb_ThenIsLongMessageReturnsTrue()
        {
            var message = GenerateTestMessage(SlightlyOverOneMegabyte);
            var result = message.IsLongMessage(DefaultMbSize);
            Assert.That(result, Is.True);
        }

        [Test]
        public void WhenMessageIsLessThanOneMb_ThenIsLongMessageReturnsFalse()
        {
            var message = GenerateTestMessage(HalfMegabyte);
            var result = message.IsLongMessage(DefaultMbSize);
            Assert.That(result, Is.False);
        }

        #endregion

        #region NeedsAzureStorageLogging Tests

        [TestCase(true)]
        [TestCase(false)]
        public void WhenAzureStorageLoggerIsDisabledAndMessageIsGreaterThanOneMb_ThenNeedsAzureStorageLoggingReturnsLogWarningNoStorage(bool isManagedIdentity)
        {
            var azureStorageBlobContainerBuilder = new AzureStorageBlobContainerBuilder(
                GetAzureStorageLogProviderOptions(false, isManagedIdentity));
            var message = GenerateTestMessage(OneMegabyte);
            var result = azureStorageBlobContainerBuilder.NeedsAzureStorageLogging(message, DefaultMbSize);
            Assert.That(result, Is.EqualTo(AzureStorageLoggingCheckResult.LogWarningNoStorage));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void WhenAzureStorageLoggerIsEnabledAndMessageIsGreaterThanOneMb_ThenNeedsAzureStorageLoggingReturnsLogWarningAndStoreMessage(bool isManagedIdentity)
        {
            var azureStorageBlobContainerBuilder = new AzureStorageBlobContainerBuilder(
                GetAzureStorageLogProviderOptions(true, isManagedIdentity));
            var message = GenerateTestMessage(OneMegabyte);
            var result = azureStorageBlobContainerBuilder.NeedsAzureStorageLogging(message, DefaultMbSize);
            Assert.That(result, Is.EqualTo(AzureStorageLoggingCheckResult.LogWarningAndStoreMessage));
        }

        [Test]
        public void WhenAzureStorageLogProviderOptionsIsNullAndMessageIsGreaterThanOneMb_ThenNeedsAzureStorageLoggingReturnsLogWarningNoStorage()
        {
            var azureStorageBlobContainerBuilder = new AzureStorageBlobContainerBuilder(null);
            var message = GenerateTestMessage(OneMegabyte);

            var result = azureStorageBlobContainerBuilder.NeedsAzureStorageLogging(message, DefaultMbSize);

            Assert.That(result, Is.EqualTo(AzureStorageLoggingCheckResult.LogWarningNoStorage));
        }

        [Test]
        public void WhenBuilderModelIsNullAndMessageIsGreaterThanOneMb_ThenNeedsAzureStorageLoggingReturnsLogWarningNoStorage()
        {
            AzureStorageBlobContainerBuilder azureStorageBlobContainerBuilder = null;
            var message = GenerateTestMessage(OneMegabyte);

            var result = azureStorageBlobContainerBuilder.NeedsAzureStorageLogging(message, DefaultMbSize);
            Assert.That(result, Is.EqualTo(AzureStorageLoggingCheckResult.LogWarningNoStorage));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void WhenMessageIsEqualToDefinedSize_ThenNeedsAzureStorageLoggingReturnsLogWarningAndStoreMessage(bool isManagedIdentity)
        {
            var azureStorageBlobContainerBuilder = new AzureStorageBlobContainerBuilder(
                GetAzureStorageLogProviderOptions(true, isManagedIdentity));
            var message = GenerateTestMessage(OneMegabyte);

            var result = azureStorageBlobContainerBuilder.NeedsAzureStorageLogging(message, DefaultMbSize);

            Assert.That(result, Is.EqualTo(AzureStorageLoggingCheckResult.LogWarningAndStoreMessage));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void WhenMessageIsGreaterThanDefinedSize_ThenNeedsAzureStorageLoggingReturnsLogWarningAndStoreMessage(bool isManagedIdentity)
        {
            var azureStorageBlobContainerBuilder = new AzureStorageBlobContainerBuilder(
                GetAzureStorageLogProviderOptions(true, isManagedIdentity));
            var message = GenerateTestMessage(SlightlyOverOneMegabyte);

            var result = azureStorageBlobContainerBuilder.NeedsAzureStorageLogging(message, DefaultMbSize);

            Assert.That(result, Is.EqualTo(AzureStorageLoggingCheckResult.LogWarningAndStoreMessage));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void WhenMessageIsLessThanDefinedSize_ThenNeedsAzureStorageLoggingReturnsNoLogging(bool isManagedIdentity)
        {
            var azureStorageBlobContainerBuilder = new AzureStorageBlobContainerBuilder(
                GetAzureStorageLogProviderOptions(true, isManagedIdentity));
            var message = GenerateTestMessage(HalfMegabyte);

            var result = azureStorageBlobContainerBuilder.NeedsAzureStorageLogging(message, DefaultMbSize);

            Assert.That(result, Is.EqualTo(AzureStorageLoggingCheckResult.NoLogging));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void WhenStorageIsDisabledAndMessageIsLessThanDefinedSize_ThenNeedsAzureStorageLoggingReturnsNoLogging(bool isManagedIdentity)
        {
            var azureStorageBlobContainerBuilder = new AzureStorageBlobContainerBuilder(
                GetAzureStorageLogProviderOptions(false, isManagedIdentity));
            var message = GenerateTestMessage(HalfMegabyte);

            var result = azureStorageBlobContainerBuilder.NeedsAzureStorageLogging(message, DefaultMbSize);

            Assert.That(result, Is.EqualTo(AzureStorageLoggingCheckResult.NoLogging));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void WhenStorageIsDisabledAndMessageIsGreaterThanDefinedSize_ThenNeedsAzureStorageLoggingReturnsLogWarningNoStorage(bool isManagedIdentity)
        {
            var azureStorageBlobContainerBuilder = new AzureStorageBlobContainerBuilder(
                GetAzureStorageLogProviderOptions(false, isManagedIdentity));
            var message = GenerateTestMessage(SlightlyOverOneMegabyte);

            var result = azureStorageBlobContainerBuilder.NeedsAzureStorageLogging(message, DefaultMbSize);

            Assert.That(result, Is.EqualTo(AzureStorageLoggingCheckResult.LogWarningNoStorage));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void WhenStorageIsDisabledAndMessageIsEqualToDefinedSize_ThenNeedsAzureStorageLoggingReturnsLogWarningNoStorage(bool isManagedIdentity)
        {
            var azureStorageBlobContainerBuilder = new AzureStorageBlobContainerBuilder(
                GetAzureStorageLogProviderOptions(false, isManagedIdentity));
            var message = GenerateTestMessage(OneMegabyte);
            var result = azureStorageBlobContainerBuilder.NeedsAzureStorageLogging(message, DefaultMbSize);

            Assert.That(result, Is.EqualTo(AzureStorageLoggingCheckResult.LogWarningNoStorage));
        }

        #endregion

        #region Message Formatting Tests

        [Test]
        public void WhenLogEntryIsOverOneMb_ThenToLongMessageWarningReturnsFormattedWarningJson()
        {
            var longLogMessage = GenerateTestMessage(SlightlyOverOneMegabyte);
            var expectedMessage = $"A log over 1MB was submitted with part of the message template: {longLogMessage.Substring(0, 256)}. Please enable the Azure Storage Event Logging feature to store details of oversize logs.";
            var expectedTemplate = "A log over 1MB was submitted with a message of template: {MessageTemplate}. Please enable the Azure Storage Event Logging feature to store details of oversize logs.";
            var timestamp = DateTime.Now;

            var logEntry = new LogEntry
            {
                Exception = new Exception(""),
                Level = "Warning",
                MessageTemplate = longLogMessage,
                Timestamp = timestamp,
                EventId = new EventId(7000)
            };

            var expectedLogEntry = new LogEntry
            {
                Exception = new Exception(expectedMessage),
                Level = "Warning",
                MessageTemplate = expectedTemplate,
                Timestamp = timestamp,
                EventId = new EventId(7000)
            };

            var warning = logEntry.ToLongMessageWarning(_jsonOptions);
            var expectedJsonString = JsonSerializer.Serialize(expectedLogEntry, _jsonOptions);

            Assert.That(warning, Is.EqualTo(expectedJsonString));
        }

        [Test]
        public void WhenConvertingLogEntryToJson_ThenToJsonLogEntryStringReturnsExpectedFormat()
        {
            var requestId = Guid.NewGuid().ToString();
            var sha = Guid.NewGuid().ToString();
            var reasonPhrase = "Tested";
            var statusCode = 201;
            var isStored = true;
            var modifiedDate = DateTime.UtcNow;
            long fileSize = 12345678;
            var timestamp = DateTime.UtcNow;
            var blobFullName = string.Format("{0}.{1}", Guid.NewGuid().ToString().Replace("-", "_"), "blob");

            var azureStorageEventLogResult = CreateAzureStorageEventLogResult(
                reasonPhrase, statusCode, requestId, sha, isStored, blobFullName, fileSize, modifiedDate);

            var azureStorageLogProviderOptions = GetAzureStorageLogProviderOptions(true);

            var logEntry = new LogEntry
            {
                Exception = new Exception(""),
                Level = "Warning",
                MessageTemplate = "Log Serialization failed with exception",
                Timestamp = timestamp,
                EventId = new EventId(7437)
            };

            var expectedTemplate = AzureStorageLoggingSuccessTemplate;

            var expectedMessage =
                $"Azure Storage Logging: A blob with the error details was created at {blobFullName}. Reason: ErrorMessageEqualOrGreaterTo1MB ResponseMessage: {reasonPhrase} ResponseCode: {statusCode} RequestId: {requestId} Sha256: {sha} FileSize(Bs): {fileSize} FileModifiedDate: {modifiedDate}";

            var expectedLogEntry = new LogEntry
            {
                Exception = new Exception(expectedMessage),
                Level = "Warning",
                MessageTemplate = expectedTemplate,
                Timestamp = timestamp,
                EventId = new EventId(7437)
            };
            var result = azureStorageEventLogResult.ToJsonLogEntryString(azureStorageLogProviderOptions, logEntry, _jsonOptions);
            var expectedJsonString = JsonSerializer.Serialize(expectedLogEntry, _jsonOptions);

            Assert.That(result, Is.EqualTo(expectedJsonString));
        }

        [Test]
        public void WhenFormattingEventLogResult_ThenToLogMessageReturnsFormattedMessage()
        {
            var requestId = Guid.NewGuid().ToString();
            var sha = Guid.NewGuid().ToString();
            var reasonPhrase = "Tested";
            var statusCode = 201;
            var isStored = true;
            var modifiedDate = DateTime.UtcNow;
            long fileSize = 12345678;
            var blobFullName = string.Format("{0}.{1}", Guid.NewGuid().ToString().Replace("-", "_"), "blob");

            var azureStorageEventLogResult = CreateAzureStorageEventLogResult(
                reasonPhrase, statusCode, requestId, sha, isStored, blobFullName, fileSize, modifiedDate);

            var azureStorageLogProviderOptions = GetAzureStorageLogProviderOptions(true);

            var template = AzureStorageLoggingSuccessTemplate;

            var expected =
                $"Azure Storage Logging: A blob with the error details was created at {blobFullName}. Reason: ErrorMessageEqualOrGreaterTo1MB ResponseMessage: {reasonPhrase} ResponseCode: {statusCode} RequestId: {requestId} Sha256: {sha} FileSize(Bs): {fileSize} FileModifiedDate: {modifiedDate}";

            var result = azureStorageEventLogResult.ToLogMessage(azureStorageLogProviderOptions, template);

            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void WhenStorageOperationFails_ThenToMessageTemplateReturnsFailedTemplate()
        {
            var options = GetAzureStorageLogProviderOptions(true);
            var requestId = Guid.NewGuid().ToString();
            var sha = Guid.NewGuid().ToString();
            var reasonPhrase = "Tested";
            var statusCode = 403;
            var isStored = false;
            var modifiedDate = DateTime.UtcNow;
            long fileSize = 12345678;
            var blobFullName = string.Format("{0}.{1}", Guid.NewGuid().ToString().Replace("-", "_"), "blob");

            var azureStorageEventLogResult = CreateAzureStorageEventLogResult(
                reasonPhrase, statusCode, requestId, sha, isStored, blobFullName, fileSize, modifiedDate);

            var expected =
                "Azure Storage Logging: Storing blob failed. Reason: ErrorMessageEqualOrGreaterTo1MB ResponseMessage: {{ReasonPhrase}} ResponseCode: {{StatusCode}} RequestId: {{RequestId}}";

            // Act
            var result = azureStorageEventLogResult.ToMessageTemplate(options);

            // Assert
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void WhenStorageOperationSucceeds_ThenToMessageTemplateReturnsSuccessTemplate()
        {
            var options = GetAzureStorageLogProviderOptions(true);
            var requestId = Guid.NewGuid().ToString();
            var sha = Guid.NewGuid().ToString();
            var reasonPhrase = "Tested";
            var statusCode = 201;
            var isStored = true;
            var modifiedDate = DateTime.UtcNow;
            long fileSize = 12345678;
            var blobFullName = string.Format("{0}.{1}", Guid.NewGuid().ToString().Replace("-", "_"), "blob");

            var azureStorageEventLogResult = CreateAzureStorageEventLogResult(
                reasonPhrase, statusCode, requestId, sha, isStored, blobFullName, fileSize, modifiedDate);

            var expected = AzureStorageLoggingSuccessTemplate;

            var result = azureStorageEventLogResult.ToMessageTemplate(options);

            Assert.That(result, Is.EqualTo(expected));
        }

        #endregion

        private AzureStorageEventLogResult CreateAzureStorageEventLogResult(
            string reasonPhrase, int statusCode, string requestId, string sha,
            bool isStored, string blobFullName, long fileSize, DateTime modifiedDate)
        {
            return new AzureStorageEventLogResult(
                reasonPhrase, statusCode, requestId, sha,
                isStored, blobFullName, fileSize, modifiedDate);
        }
    }
}
