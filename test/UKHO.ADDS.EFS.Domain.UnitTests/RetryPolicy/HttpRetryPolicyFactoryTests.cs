using System.Net;
using FakeItEasy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UKHO.ADDS.EFS.RetryPolicy;
using UKHO.ADDS.Infrastructure.Results;

namespace UKHO.ADDS.EFS.Builder.S100.UnitTests.Infrastructure
{
    [TestFixture]
    public class HttpRetryPolicyFactoryTests
    {
        private ILogger _logger;
        private IConfiguration _configuration;

        private const string METHOD_NAME = "TestMethod";

        [SetUp]
        public void SetUp()
        {
            _logger = A.Fake<ILogger>();
            _configuration = A.Fake<IConfiguration>();
            A.CallTo(() => _configuration["HttpRetry:RetryDelayInMilliseconds"]).Returns("2000");
            HttpRetryPolicyFactory.SetConfiguration(_configuration);
        }
        
        [Test]
        public async Task WhenHttpRequestExceptionThrown_ThenRetriesExpectedNumberOfTimes()
        {
            int callCount = 0;
            var policy = HttpRetryPolicyFactory.GetRetryPolicy(_logger);
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
            var policy = HttpRetryPolicyFactory.GetRetryPolicy(_logger);
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
            var policy = HttpRetryPolicyFactory.GetRetryPolicy(_logger);
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
            var policy = HttpRetryPolicyFactory.GetRetryPolicy(_logger);
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
            Assert.That(() => HttpRetryPolicyFactory.SetConfiguration(null), Throws.Nothing);
        }

        [Test]
        public void WhenExtractStatusCodeFromErrorWithNull_ThenReturnsNull()
        {
            Assert.That(HttpRetryPolicyFactory.ExtractStatusCodeFromError(null), Is.Null);
        }

        [Test]
        public void WhenExtractStatusCodeFromErrorWithNoMetadata_ThenReturnsNull()
        {
            var error = A.Fake<IError>();
            A.CallTo(() => error.Metadata).Returns(null);
            Assert.That(HttpRetryPolicyFactory.ExtractStatusCodeFromError(error), Is.Null);
        }

        [Test]
        public void WhenExtractStatusCodeFromErrorWithNoStatusCode_ThenReturnsNull()
        {
            var error = A.Fake<IError>();
            A.CallTo(() => error.Metadata).Returns(new System.Collections.Generic.Dictionary<string, object>());
            Assert.That(HttpRetryPolicyFactory.ExtractStatusCodeFromError(error), Is.Null);
        }

        [Test]
        public void WhenExtractStatusCodeFromErrorWithStatusCode_ThenReturnsStatusCode()
        {
            var error = A.Fake<IError>();
            var dict = new System.Collections.Generic.Dictionary<string, object> { { "StatusCode", 503 } };
            A.CallTo(() => error.Metadata).Returns(dict);
            Assert.That(HttpRetryPolicyFactory.ExtractStatusCodeFromError(error), Is.EqualTo(503));
        }

        [Test]
        public void WhenLoadRetrySettingsWithNoConfiguration_ThenReturnsDefault()
        {
            var settings = typeof(HttpRetryPolicyFactory).GetMethod("LoadRetrySettings", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).Invoke(null, null);
            Assert.That(settings, Is.Not.Null);
        }

        [Test]
        public void WhenLoadRetrySettingsWithConfiguration_ThenReturnsConfiguredValues()
        {
            A.CallTo(() => _configuration["HttpRetry:MaxRetryAttempts"]).Returns("5");
            A.CallTo(() => _configuration["HttpRetry:RetryDelayInMilliseconds"]).Returns("1234");
            HttpRetryPolicyFactory.SetConfiguration(_configuration);
            var settings = typeof(HttpRetryPolicyFactory).GetMethod("LoadRetrySettings", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).Invoke(null, null);
            Assert.That(settings.ToString(), Does.Contain("5").And.Contain("1234"));
        }

        [Test]
        public async Task WhenGetRetryPolicyTWithRetriableStatusCode_ThenRetries()
        {
            int callCount = 0;
            var policy = HttpRetryPolicyFactory.GetRetryPolicy<string>(_logger, s => 503, METHOD_NAME);
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
            var policy = HttpRetryPolicyFactory.GetRetryPolicy<string>(_logger, s => 400, METHOD_NAME);
            await policy.ExecuteAsync(() =>
            {
                callCount++;
                return Task.FromResult("test");
            });
            Assert.That(callCount, Is.EqualTo(1));
        }

        [TearDown]
        public void TearDown()
        {
            HttpRetryPolicyFactory.SetConfiguration(null);
        }
    }
}
