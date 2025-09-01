using Azure.Core;
using FakeItEasy;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging.Implementation.AzureStorageEventLogging.Models;
using UKHO.ADDS.EFS.Orchestrator.UnitTests.Logging.Implementation.AzureStorageEventLogging.Factories;

namespace UKHO.ADDS.EFS.Orchestrator.UnitTests.Logging.Implementation.AzureStorageEventLogging.Models
{
    /// <summary>
    ///     Tests for the azure storage log provider
    /// </summary>
    [TestFixture]
    public class AzureStorageLogProviderOptionsTests
    {
        private readonly ResourcesFactory resourcesFactory = new ResourcesFactory();
        private const string _invalidUrl = "-invalidUrl";
        private const string _validUrl = "https://test.com/";

        [Test]
        public void WhenValidatingSasUrlWithInvalidUrl_ThenThrowsUriFormatException()
        {
            var exception = Assert.Throws<UriFormatException>(() => new AzureStorageLogProviderOptions(_invalidUrl,
                true,
                resourcesFactory.SuccessTemplateMessage,
                resourcesFactory.FailureTemplateMessage));
            Assert.Multiple(() =>
            {
                Assert.That(exception, Is.Not.Null);
                Assert.That(exception.Message, Is.EqualTo("Invalid sas url."));
            });

        }

        [Test]
        public void WhenValidatingSasUrlWithNullUrl_ThenThrowsNullReferenceException()
        {
            string url = null;

            var exception = Assert.Throws<NullReferenceException>(() =>
                                                                    new AzureStorageLogProviderOptions(url,
                                                                    true,
                                                                    resourcesFactory.SuccessTemplateMessage,
                                                                    resourcesFactory.FailureTemplateMessage));

            Assert.That(exception, Is.Not.Null);
            Assert.That(exception.Message, Is.EqualTo("The Azure storage container sas url cannot be null or empty when Azure storage option is set to enabled"));
        }

        [Test]
        public void WhenValidatingSasUrlWithValidUrl_ThenCreatesOptionsWithCorrectUrl()
        {
            var result = new AzureStorageLogProviderOptions(_validUrl, true, resourcesFactory.SuccessTemplateMessage, resourcesFactory.FailureTemplateMessage);

            Assert.Multiple(() =>
            {
                Assert.That(result.AzureStorageContainerSasUrl, Is.Not.Null);
                Assert.That(result.AzureStorageContainerSasUrl.AbsoluteUri, Is.EqualTo(_validUrl));
            });

        }

        [Test]
        public void WhenUsingManagedIdentity_WithInvalidUri_ThrowsException()
        {
            var tokenCredential = A.Fake<TokenCredential>();

            var exception = Assert.Throws<UriFormatException>(() => new AzureStorageLogProviderOptions(new Uri(_invalidUrl),
                                                                                                        tokenCredential,
                                                                                                        true,
                                                                                                        resourcesFactory.SuccessTemplateMessage,
                                                                                                        resourcesFactory.FailureTemplateMessage));
            Assert.Multiple(() =>
            {
                Assert.That(exception, Is.Not.Null);
                Assert.That(exception.Message, Is.EqualTo("Invalid URI: The format of the URI could not be determined."));
            });

        }

        [Test]
        public void WhenUsingManagedIdentity_WithValidUriAndTokenCredentials_ThenSucceeds()
        {
            var tokenCredential = A.Fake<TokenCredential>();

            var result = new AzureStorageLogProviderOptions(new Uri(_validUrl),
                                                            tokenCredential,
                                                            true,
                                                            resourcesFactory.SuccessTemplateMessage,
                                                            resourcesFactory.FailureTemplateMessage);

            Assert.Multiple(() =>
            {
                Assert.That(result.AzureStorageBlobContainerUri, Is.Not.Null);
                Assert.That(result.AzureStorageBlobContainerUri.AbsoluteUri, Is.EqualTo(_validUrl));
            });
        }

        [Test]
        public void WhenUsingManagedIdentity_WithNullTokenCredentials_ThenThrowsException()
        {
            TokenCredential tokenCredentials = null;

            var exception = Assert.Throws<NullReferenceException>(() => new AzureStorageLogProviderOptions(new Uri(_validUrl),
                                                                tokenCredentials,
                                                                true,
                                                                resourcesFactory.SuccessTemplateMessage,
                                                                resourcesFactory.FailureTemplateMessage));
            Assert.Multiple(() =>
            {
                Assert.That(exception, Is.Not.Null);
                Assert.That(exception.Message, Is.EqualTo("The credential cannot be null when Azure storage option is set to enabled"));
            });
        }

        [Test]
        public void WhenUsingManagedIdentity_WithStorageOptionNotSet_ThenNoValidationHappens()
        {
            TokenCredential tokenCredentials = null;

            Assert.DoesNotThrow(() => new AzureStorageLogProviderOptions(new Uri(_validUrl),
                                                                tokenCredentials,
                                                                false,
                                                                resourcesFactory.SuccessTemplateMessage,
                                                                resourcesFactory.FailureTemplateMessage));
        }
    }
}
