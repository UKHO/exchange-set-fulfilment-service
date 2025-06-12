using System.Net;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using UKHO.ADDS.EFS.Domain.RetryPolicy;

namespace UKHO.ADDS.EFS.Builder.S100.UnitTests
{
    [TestFixture]
    internal class HttpClientPolicyProviderTest
    {
        private ILogger _logger;

        [SetUp]
        public void SetUp()
        {
            _logger = A.Fake<ILogger>();
        }

        [Test]
        public async Task When_HttpRequestExceptionThrown_Then_RetriesUpToMaxAndLogsEachAttempt()
        {
            var policy = HttpClientPolicyProvider.GetRetryPolicy(_logger);
            int callCount = 0;
            Task<HttpResponseMessage> Handler()
            {
                callCount++;
                throw new HttpRequestException("Network error");
            }
            var ex = Assert.ThrowsAsync<HttpRequestException>(async () => await policy.ExecuteAsync(Handler));
            Assert.That(callCount, Is.EqualTo(4));
            A.CallTo(() => _logger.Log(
                A<LogLevel>.Ignored,
                A<EventId>.Ignored,
                A<object>.Ignored,
                A<Exception>.Ignored,
                A<Func<object, Exception, string>>.Ignored
            )).MustHaveHappened(3, Times.Exactly);
        }

        [TestCase(408)]
        [TestCase(429)]
        [TestCase(502)]
        [TestCase(503)]
        [TestCase(504)]
        public async Task When_RetriableStatusCode_Then_RetriesUpToMaxAndLogsEachAttempt(int statusCode)
        {
            var policy = HttpClientPolicyProvider.GetRetryPolicy(_logger);
            int callCount = 0;
            Task<HttpResponseMessage> Handler()
            {
                callCount++;
                return Task.FromResult(new HttpResponseMessage((HttpStatusCode)statusCode)
                {
                    RequestMessage = new HttpRequestMessage(HttpMethod.Get, $"http://test/{callCount}")
                });
            }
            var result = await policy.ExecuteAsync(Handler);
            Assert.That(callCount, Is.EqualTo(4));
            Assert.That(result.StatusCode, Is.EqualTo((HttpStatusCode)statusCode));
            A.CallTo(() => _logger.Log(
                A<LogLevel>.Ignored,
                A<EventId>.Ignored,
                A<object>.Ignored,
                A<Exception>.Ignored,
                A<Func<object, Exception, string>>.Ignored
            )).MustHaveHappened(3, Times.Exactly);
        }

        [Test]
        public async Task When_NonRetriableStatusCode_Then_NoRetryAndLogsOnce()
        {
            var policy = HttpClientPolicyProvider.GetRetryPolicy(_logger);
            int callCount = 0;
            var nonRetriableStatus = HttpStatusCode.OK;
            Task<HttpResponseMessage> Handler()
            {
                callCount++;
                return Task.FromResult(new HttpResponseMessage(nonRetriableStatus)
                {
                    RequestMessage = new HttpRequestMessage(HttpMethod.Get, "http://test/ok")
                });
            }
            var result = await policy.ExecuteAsync(Handler);
            Assert.That(callCount, Is.EqualTo(1));
            Assert.That(result.StatusCode, Is.EqualTo(nonRetriableStatus));
            A.CallTo(() => _logger.Log(
                A<LogLevel>.Ignored,
                A<EventId>.Ignored,
                A<object>.Ignored,
                A<Exception>.Ignored,
                A<Func<object, Exception, string>>.Ignored
            )).MustNotHaveHappened();
        }

        [Test]
        public async Task When_RequestMessageIsNull_Then_UrlIsNAInLogger()
        {
            var policy = HttpClientPolicyProvider.GetRetryPolicy(_logger);
            Task<HttpResponseMessage> Handler()
            {
                return Task.FromResult(new HttpResponseMessage((HttpStatusCode)408));
            }
            await policy.ExecuteAsync(Handler);
            A.CallTo(() => _logger.Log(
                A<LogLevel>.Ignored,
                A<EventId>.Ignored,
                A<object>.Ignored,
                A<Exception>.Ignored,
                A<Func<object, Exception, string>>.Ignored
            )).MustHaveHappened();
        }

        [Test]
        public async Task When_RequestUriIsNull_Then_UrlIsNAInLogger()
        {
            var policy = HttpClientPolicyProvider.GetRetryPolicy(_logger);
            Task<HttpResponseMessage> Handler()
            {
                return Task.FromResult(new HttpResponseMessage((HttpStatusCode)408)
                {
                    RequestMessage = new HttpRequestMessage()
                });
            }
            await policy.ExecuteAsync(Handler);
            A.CallTo(() => _logger.Log(
                A<LogLevel>.Ignored,
                A<EventId>.Ignored,
                A<object>.Ignored,
                A<Exception>.Ignored,
                A<Func<object, Exception, string>>.Ignored
            )).MustHaveHappened();
        }

        [Test]
        public async Task When_ExceptionOnFirstThenSuccess_Then_RetriesOnceAndReturnsSuccess()
        {
            var policy = HttpClientPolicyProvider.GetRetryPolicy(_logger);
            int callCount = 0;
            Task<HttpResponseMessage> Handler()
            {
                callCount++;
                if (callCount == 1)
                    throw new HttpRequestException("fail");
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    RequestMessage = new HttpRequestMessage(HttpMethod.Get, "http://test/success")
                });
            }
            var result = await policy.ExecuteAsync(Handler);
            Assert.That(callCount, Is.EqualTo(2));
            Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            A.CallTo(() => _logger.Log(
                A<LogLevel>.Ignored,
                A<EventId>.Ignored,
                A<object>.Ignored,
                A<Exception>.Ignored,
                A<Func<object, Exception, string>>.Ignored
            )).MustHaveHappened(1, Times.Exactly);
        }

        [Test]
        public async Task When_RetriableStatusCodeThenSuccess_Then_RetriesOnceAndReturnsSuccess()
        {
            var policy = HttpClientPolicyProvider.GetRetryPolicy(_logger);
            int callCount = 0;
            Task<HttpResponseMessage> Handler()
            {
                callCount++;
                if (callCount == 1)
                    return Task.FromResult(new HttpResponseMessage((HttpStatusCode)408)
                    {
                        RequestMessage = new HttpRequestMessage(HttpMethod.Get, "http://test/retry")
                    });
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    RequestMessage = new HttpRequestMessage(HttpMethod.Get, "http://test/success")
                });
            }
            var result = await policy.ExecuteAsync(Handler);
            Assert.That(callCount, Is.EqualTo(2));
            Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            A.CallTo(() => _logger.Log(
                A<LogLevel>.Ignored,
                A<EventId>.Ignored,
                A<object>.Ignored,
                A<Exception>.Ignored,
                A<Func<object, Exception, string>>.Ignored
            )).MustHaveHappened(1, Times.Exactly);
        }

        [Test]
        public void When_LoggerIsNull_Then_PolicyDoesNotThrowOnRetry()
        {
            var policy = HttpClientPolicyProvider.GetRetryPolicy(null);
            int callCount = 0;
            Task<HttpResponseMessage> Handler()
            {
                callCount++;
                throw new HttpRequestException("Network error");
            }
            Assert.That(async () => await policy.ExecuteAsync(Handler), Throws.TypeOf<HttpRequestException>());
            Assert.That(callCount, Is.EqualTo(4));
        }

        [Test]
        public async Task When_HandlerReturnsNull_Then_PolicyThrows()
        {
            var policy = HttpClientPolicyProvider.GetRetryPolicy(_logger);
            int callCount = 0;
            Task<HttpResponseMessage> Handler()
            {
                callCount++;
                return Task.FromResult<HttpResponseMessage>(null);
            }
            var ex = Assert.ThrowsAsync<NullReferenceException>(async () => await policy.ExecuteAsync(Handler));
            Assert.That(callCount, Is.EqualTo(1));
        }

        [Test]
        public void When_HandlerThrowsNonHttpRequestException_Then_PolicyDoesNotRetry()
        {
            var policy = HttpClientPolicyProvider.GetRetryPolicy(_logger);
            int callCount = 0;
            Task<HttpResponseMessage> Handler()
            {
                callCount++;
                throw new InvalidOperationException("Non-HTTP error");
            }
            Assert.That(async () => await policy.ExecuteAsync(Handler), Throws.TypeOf<InvalidOperationException>());
            Assert.That(callCount, Is.EqualTo(1));
        }

        [Test]
        public async Task When_ResponseHasUnknownStatusCode_Then_NoRetryOccurs()
        {
            var policy = HttpClientPolicyProvider.GetRetryPolicy(_logger);
            int callCount = 0;
            var unknownStatus = (HttpStatusCode)599;
            Task<HttpResponseMessage> Handler()
            {
                callCount++;
                return Task.FromResult(new HttpResponseMessage(unknownStatus)
                {
                    RequestMessage = new HttpRequestMessage(HttpMethod.Get, "http://test/unknown")
                });
            }
            var result = await policy.ExecuteAsync(Handler);
            Assert.That(callCount, Is.EqualTo(1));
            Assert.That(result.StatusCode, Is.EqualTo(unknownStatus));
            A.CallTo(() => _logger.Log(
                A<LogLevel>.Ignored,
                A<EventId>.Ignored,
                A<object>.Ignored,
                A<Exception>.Ignored,
                A<Func<object, Exception, string>>.Ignored
            )).MustNotHaveHappened();
        }
    }
}
