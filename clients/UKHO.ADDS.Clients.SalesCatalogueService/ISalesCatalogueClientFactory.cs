using UKHO.ADDS.Clients.Common.Authentication;

namespace UKHO.ADDS.Clients.SalesCatalogueService
{
    public interface ISalesCatalogueClientFactory
    {
        ISalesCatalogueClient CreateClient(string baseAddress, string accessToken);

        ISalesCatalogueClient CreateClient(string baseAddress, IAuthenticationTokenProvider tokenProvider);
    }
}
