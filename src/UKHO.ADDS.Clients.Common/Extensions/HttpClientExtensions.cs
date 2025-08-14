using System.Net.Http.Headers;
using UKHO.ADDS.Clients.Common.Authentication;
using UKHO.ADDS.Clients.Common.Constants;

namespace UKHO.ADDS.Clients.Common.Extensions
{
    internal static class HttpClientExtensions
    {
        /// <summary>
        ///     Sets Authorization header
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="authTokenProvider"></param>
        /// <returns></returns>
        internal static async Task SetAuthenticationHeaderAsync(this HttpClient httpClient, IAuthenticationTokenProvider authTokenProvider)
        {
            var token = await authTokenProvider.GetTokenAsync();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(ApiHeaderKeys.BearerTokenHeaderKey, token);
        }

        /// <summary>
        /// Sets CorrelationId in request header
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="correlationId"></param>
        /// <returns></returns>
        internal static void SetCorrelationIdHeader(this HttpClient httpClient, string correlationId)
        {
            httpClient.DefaultRequestHeaders.Add(ApiHeaderKeys.XCorrelationIdHeaderKey, correlationId);
        }
    }
}
