using System.Text;
using Azure.Storage.Blobs;
using FakeItEasy;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging.Implementation.AzureStorageEventLogging;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging.Implementation.AzureStorageEventLogging.Enums;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging.Implementation.AzureStorageEventLogging.Models;


namespace UKHO.ADDS.EFS.Orchestrator.UnitTests.Logging.Implementation.AzureStorageEventLogging
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
        private const int HalfMegabyteSize = 512 * 1024;
        private const string TestBlobPath = "day";
        private const string TestBlobExtension = "json";

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
            var expected = $"{TestServiceName} - {TestEnvironment}";

            var result = _azureStorageLogger.GenerateServiceName(TestServiceName, TestEnvironment);

            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void WhenGeneratingPathForErrorBlob_ThenReturnsCorrectlyFormattedPath()
        {
            var testDateTime = new DateTime(2021, 12, 10, 12, 13, 14);
            var expected = "2021\\12\\10\\12\\13\\14";

            var result = _azureStorageLogger.GeneratePathForErrorBlob(testDateTime);

            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void WhenGeneratingErrorBlobNameWithGuidOnly_ThenReturnsNameWithDefaultJsonExtension()
        {
            var testGuid = Guid.NewGuid();
            var expected = $"{testGuid.ToString().Replace("-", "_")}.json";

            var result = _azureStorageLogger.GenerateErrorBlobName(testGuid);

            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void WhenGeneratingErrorBlobNameWithGuidAndExtension_ThenReturnsNameWithSpecifiedExtension()
        {
            var testGuid = Guid.NewGuid();
            var extension = "json";
            var expected = $"{testGuid.ToString().Replace("-", "_")}.{extension}";

            var result = _azureStorageLogger.GenerateErrorBlobName(testGuid, extension);

            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void WhenGeneratingBlobFullName_ThenReturnsCombinedPath()
        {
            var testGuid = Guid.NewGuid();
            var extension = "json";
            var path = "2021\\12\\10\\12\\13\\14";
            var blobName = $"{testGuid.ToString().Replace("-", "_")}.{extension}";
            var azureStorageBlobModel = new AzureStorageBlobFullNameModel(TestServiceName, path, blobName);
            var expected = Path.Combine(TestServiceName, path, blobName);

            var result = _azureStorageLogger.GenerateBlobFullName(azureStorageBlobModel);

            Assert.That(result, Is.EqualTo(expected));
        }

        #endregion

        #region Storage Operation Tests

        [Test]
        public void WhenCancellingStorageOperationWithNoActiveOperation_ThenReturnsUnableToCancel()
        {
            var result = _azureStorageLogger.CancelLogFileStoringOperation();

            Assert.That(result, Is.EqualTo(AzureStorageEventLogCancellationResult.UnableToCancel));
        }

        [Test]
        public void WhenCancellingActiveStorageOperation_ThenCancelsSuccessfully()
        {
            var fileName = $"{TestServiceName} - {TestEnvironment}/{TestBlobPath}/{GetTestBlobName()}";
            var azureStorageModel = CreateTestAzureStorageEventModel(fileName, HalfMegabyteSize);

            _azureStorageLogger.StoreLogFile(azureStorageModel, true);
            var result = _azureStorageLogger.CancelLogFileStoringOperation();

            Assert.That(result, Is.EqualTo(AzureStorageEventLogCancellationResult.Successful));
        }

        [Test]
        public void WhenCancellingActiveStorageOperationWithNullToken_ThenCancellationFails()
        {
            var fileName = $"{TestServiceName} - {TestEnvironment}/{TestBlobPath}/{GetTestBlobName()}";
            var azureStorageModel = CreateTestAzureStorageEventModel(fileName, HalfMegabyteSize);

            _azureStorageLogger.StoreLogFile(azureStorageModel, true);
            _azureStorageLogger.NullifyTokenSource();
            var result = _azureStorageLogger.CancelLogFileStoringOperation();

            Assert.That(result, Is.EqualTo(AzureStorageEventLogCancellationResult.CancellationFailed));
        }

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
        private static string GetTestBlobName(Guid? guid = null, string extension = null)
        {
            var id = (guid ?? Guid.NewGuid()).ToString().Replace("-", "_");
            return $"{id}.{extension ?? TestBlobExtension}";
        }
        private static AzureStorageEventModel CreateTestAzureStorageEventModel(string fileName, int size)
        {
            return new AzureStorageEventModel(fileName, GenerateTestMessage(size));
        }

        #endregion
    }
}
