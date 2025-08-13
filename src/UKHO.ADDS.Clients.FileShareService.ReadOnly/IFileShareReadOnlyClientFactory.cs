using UKHO.ADDS.Clients.Common.Authentication;

namespace UKHO.ADDS.Clients.FileShareService.ReadOnly
{
    public interface IFileShareReadOnlyClientFactory
    {
        IFileShareReadOnlyClient CreateClient(string baseAddress, string accessToken);

        IFileShareReadOnlyClient CreateClient(string baseAddress, IAuthenticationTokenProvider tokenProvider);
    }
}
