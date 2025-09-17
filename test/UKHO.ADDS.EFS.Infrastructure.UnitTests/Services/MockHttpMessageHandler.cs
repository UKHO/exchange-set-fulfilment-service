using System.Net;

namespace UKHO.ADDS.EFS.Infrastructure.UnitTests.Services
{
    /// <summary>
    /// Mock HTTP message handler for testing HTTP client interactions
    /// </summary>
    internal class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly Dictionary<string, (HttpStatusCode statusCode, string? content, Exception? exception, TimeSpan? delay)> _responses = new();
        private readonly List<HttpRequestMessage> _requestsSent = new();

        public IReadOnlyList<HttpRequestMessage> RequestsSent => _requestsSent.AsReadOnly();

        public void SetResponse(string url, HttpStatusCode statusCode, string? content = null)
        {
            _responses[url] = (statusCode, content, null, null);
        }

        public void SetException(string url, Exception exception)
        {
            _responses[url] = (HttpStatusCode.OK, null, exception, null);
        }

        public void SetDelay(string url, TimeSpan delay)
        {
            _responses[url] = (HttpStatusCode.OK, null, null, delay);
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            _requestsSent.Add(request);

            var url = request.RequestUri?.ToString() ?? "";

            if (_responses.TryGetValue(url, out var response))
            {
                if (response.exception != null)
                {
                    throw response.exception;
                }

                if (response.delay.HasValue)
                {
                    await Task.Delay(response.delay.Value, cancellationToken);
                }

                var httpResponse = new HttpResponseMessage(response.statusCode);
                if (response.content != null)
                {
                    httpResponse.Content = new StringContent(response.content);
                }

                return httpResponse;
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (var request in _requestsSent)
                {
                    request.Dispose();
                }
                _requestsSent.Clear();
            }

            base.Dispose(disposing);
        }
    }
}
