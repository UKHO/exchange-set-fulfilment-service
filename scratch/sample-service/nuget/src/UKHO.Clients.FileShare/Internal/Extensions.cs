using System.Net.Http.Headers;
using System.Security.Cryptography;
using UKHO.Infrastructure.Serialization.Json;

namespace UKHO.Clients.FileShare.Internal
{
    internal static class Extensions
    {
        /// <summary>
        /// Reads response body json as given type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="httpResponseMessage"></param>
        /// <returns></returns>
        public static async Task<T> ReadAsTypeAsync<T>(this HttpResponseMessage httpResponseMessage)
        {
            var bodyJson = await httpResponseMessage.Content.ReadAsStringAsync();
            return JsonConverter.Deserialize<T>(bodyJson);
        }

        public static async Task<Stream> ReadAsStreamAsync(this HttpResponseMessage httpResponseMessage)
        {
            return await httpResponseMessage.Content.ReadAsStreamAsync();
        }

        /// <summary>
        /// Sets Authorization header
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="authTokenProvider"></param>
        /// <returns></returns>
        public static async Task SetAuthenticationHeaderAsync(this HttpClient httpClient, IAuthTokenProvider authTokenProvider)
        {
            var token = await authTokenProvider.GetTokenAsync();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", token);
        }
    }

    internal static class Md5HashHelper
    {
        public static byte[] CalculateMD5(this Stream stream)
        {
            var position = stream.Position;
            stream.Seek(0, SeekOrigin.Begin);
            using (var md5 = MD5.Create())
            {
                var hash = md5.ComputeHash(stream);
                stream.Seek(position, SeekOrigin.Begin);

                return hash;
            }
        }
    }
}
