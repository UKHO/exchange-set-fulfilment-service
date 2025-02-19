using System.Diagnostics.CodeAnalysis;

namespace UKHO.Clients.FileShare.Internal
{
    [ExcludeFromCodeCoverage]
    internal class DefaultHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
        {
            return new HttpClient();
        }
    }
}
