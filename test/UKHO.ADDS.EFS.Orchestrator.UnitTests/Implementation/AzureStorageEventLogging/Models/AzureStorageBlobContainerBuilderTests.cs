using Azure.Core;
using FakeItEasy;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging.Implementation.AzureStorageEventLogging.Models;
using UKHO.ADDS.EFS.Orchestrator.UnitTests.Implementation.AzureStorageEventLogging.Factories;

namespace UKHO.ADDS.EFS.Orchestrator.UnitTests.Implementation.AzureStorageEventLogging.Models
{
    /// <summary>
    ///     Tests for the azure blob container builder
    /// </summary>
    [TestFixture]
    public class AzureStorageBlobContainerBuilderTests
    {
        private const string ValidUri = "https://test.com";
        private ResourcesFactory _resourcesFactory;

        [SetUp]
        public void SetUp()
        {
            _resourcesFactory = new ResourcesFactory();
        }

        [TestCase(ValidUri, false, false, TestName = "WhenBuildIsCalledWithLoggerDisabledOptions_ThenBlobContainerClientIsNull")]
        [TestCase(ValidUri, true, true, TestName = "WhenBuildIsCalledWithSASConnectionOptions_ThenPropertiesAreNotNull")]
        public void Build_WithVariousOptions_ReturnsExpectedResults(string uri, bool enabled, bool expectNotNull)
        {
            AzureStorageBlobContainerBuilder azureOptionsModel;
            if (uri == null)
            {
                azureOptionsModel = new AzureStorageBlobContainerBuilder(null);
            }
            else
            {
                azureOptionsModel = new AzureStorageBlobContainerBuilder(
                    new AzureStorageLogProviderOptions(uri, enabled, _resourcesFactory.SuccessTemplateMessage, _resourcesFactory.FailureTemplateMessage));
            }

            azureOptionsModel.Build();

            if (uri == null)
            {
                Assert.That(azureOptionsModel.AzureStorageLogProviderOptions, Is.Null);
                Assert.That(azureOptionsModel.BlobContainerClient, Is.Null);
            }
            else if (!enabled)
            {
                Assert.That(azureOptionsModel.BlobContainerClient, Is.Null);
            }
            else
            {
                Assert.That(azureOptionsModel.AzureStorageLogProviderOptions, Is.Not.Null);
                Assert.That(azureOptionsModel.BlobContainerClient, Is.Not.Null);
            }
        }

        [Test]
        public void WhenBuildIsCalledWithManagedIdentityOptions_ThenPropertiesAreNotNull()
        {
            var tokenCredential = A.Fake<TokenCredential>();
            var azureOptionsModel = new AzureStorageBlobContainerBuilder(
                new AzureStorageLogProviderOptions(
                    new Uri(ValidUri),
                    tokenCredential,
                    true,
                    _resourcesFactory.SuccessTemplateMessage,
                    _resourcesFactory.FailureTemplateMessage));

            azureOptionsModel.Build();

            Assert.That(azureOptionsModel.AzureStorageLogProviderOptions, Is.Not.Null);
            Assert.That(azureOptionsModel.BlobContainerClient, Is.Not.Null);
        }
    }
}
