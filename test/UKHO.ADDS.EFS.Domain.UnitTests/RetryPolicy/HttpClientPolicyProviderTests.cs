using System.Net;
using FakeItEasy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UKHO.ADDS.EFS.Domain.RetryPolicy;
using UKHO.ADDS.Infrastructure.Results;

namespace UKHO.ADDS.EFS.Builder.S100.UnitTests.Infrastructure
{
    [TestFixture]
    public class HttpClientPolicyProviderTests
    {
        private ILogger _logger;
        private IConfiguration _configuration;
        private const string METHOD_NAME = "TestMethod";

        [SetUp]
        public void SetUp()
        {
            _logger = A.Fake<ILogger>();
            _configuration = A.Fake<IConfiguration>();
            HttpClientPolicyProvider.SetConfiguration(null);
        }

        [Test]
        public async Task WhenHttpRequestExceptionThrown_ThenRetriesExpectedNumberOfTimes()
        {
            int callCount = 0;
            var policy = HttpClientPolicyProvider.GetRetryPolicy(_logger);
            try
            {
                await policy.ExecuteAsync(() =>
                {
                    callCount++;
                    throw new HttpRequestException("Transient error");
                });
            }
            catch (HttpRequestException) { }
            Assert.That(callCount, Is.EqualTo(4));
        }

        [Test]
        public async Task WhenRetriableStatusCode_ThenRetriesExpectedNumberOfTimes()
        {
            int callCount = 0;
            var policy = HttpClientPolicyProvider.GetRetryPolicy(_logger);
            await policy.ExecuteAsync(() =>
            {
                callCount++;
                var response = new HttpResponseMessage((HttpStatusCode)503);
                return Task.FromResult(response);
            });
            Assert.That(callCount, Is.EqualTo(4));
        }

        [Test]
        public async Task WhenNonRetriableStatusCode_ThenDoesNotRetry()
        {
            int callCount = 0;
            var policy = HttpClientPolicyProvider.GetRetryPolicy(_logger);
            await policy.ExecuteAsync(() =>
            {
                callCount++;
                var response = new HttpResponseMessage(HttpStatusCode.BadRequest);
                return Task.FromResult(response);
            });
            Assert.That(callCount, Is.EqualTo(1));
        }

        [Test]
        public async Task WhenSuccessResponse_ThenDoesNotRetry()
        {
            int callCount = 0;
            var policy = HttpClientPolicyProvider.GetRetryPolicy(_logger);
            await policy.ExecuteAsync(() =>
            {
                callCount++;
                var response = new HttpResponseMessage(HttpStatusCode.OK);
                return Task.FromResult(response);
            });
            Assert.That(callCount, Is.EqualTo(1));
        }

        [Test]
        public void WhenSetConfigurationWithNull_ThenDoesNotThrow()
        {
            Assert.That(() => HttpClientPolicyProvider.SetConfiguration(null), Throws.Nothing);
        }

        [Test]
        public void WhenGetStatusCodeFromErrorWithNull_ThenReturnsNull()
        {
            Assert.That(HttpClientPolicyProvider.GetStatusCodeFromError(null), Is.Null);
        }

        [Test]
        public void WhenGetStatusCodeFromErrorWithNoMetadata_ThenReturnsNull()
        {
            var error = A.Fake<IError>();
            A.CallTo(() => error.Metadata).Returns(null);
            Assert.That(HttpClientPolicyProvider.GetStatusCodeFromError(error), Is.Null);
        }

        [Test]
        public void WhenGetStatusCodeFromErrorWithNoStatusCode_ThenReturnsNull()
        {
            var error = A.Fake<IError>();
            A.CallTo(() => error.Metadata).Returns(new System.Collections.Generic.Dictionary<string, object>());
            Assert.That(HttpClientPolicyProvider.GetStatusCodeFromError(error), Is.Null);
        }

        [Test]
        public void WhenGetStatusCodeFromErrorWithStatusCode_ThenReturnsStatusCode()
        {
            var error = A.Fake<IError>();
            var dict = new System.Collections.Generic.Dictionary<string, object> { { "StatusCode", 503 } };
            A.CallTo(() => error.Metadata).Returns(dict);
            Assert.That(HttpClientPolicyProvider.GetStatusCodeFromError(error), Is.EqualTo(503));
        }

        [Test]
        public void WhenGetRetrySettingsWithNoConfiguration_ThenReturnsDefault()
        {
            var settings = typeof(HttpClientPolicyProvider).GetMethod("GetRetrySettings", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).Invoke(null, null);
            Assert.That(settings, Is.Not.Null);
        }

        [Test]
        public void WhenGetRetrySettingsWithConfiguration_ThenReturnsConfiguredValues()
        {
            A.CallTo(() => _configuration["HttpRetry:MaxRetryAttempts"]).Returns("5");
            A.CallTo(() => _configuration["HttpRetry:RetryDelayMs"]).Returns("1234");
            HttpClientPolicyProvider.SetConfiguration(_configuration);
            var settings = typeof(HttpClientPolicyProvider).GetMethod("GetRetrySettings", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).Invoke(null, null);
            Assert.That(settings.ToString(), Does.Contain("5").And.Contain("1234"));
        }

        [Test]
        public async Task WhenGetRetryPolicyTWithRetriableStatusCode_ThenRetries()
        {
            int callCount = 0;
            var policy = HttpClientPolicyProvider.GetRetryPolicy<string>(_logger, s => 503, METHOD_NAME);
            await policy.ExecuteAsync(() =>
            {
                callCount++;
                return Task.FromResult("test");
            });
            Assert.That(callCount, Is.EqualTo(4));
        }

        [Test]
        public async Task WhenGetRetryPolicyTWithNonRetriableStatusCode_ThenDoesNotRetry()
        {
            int callCount = 0;
            var policy = HttpClientPolicyProvider.GetRetryPolicy<string>(_logger, s => 400, METHOD_NAME);
            await policy.ExecuteAsync(() =>
            {
                callCount++;
                return Task.FromResult("test");
            });
            Assert.That(callCount, Is.EqualTo(1));
        }
    }
}
