using UKHO.ADDS.Clients.Common.Authentication;

namespace UKHO.ADDS.Clients.FileShareService.ReadWrite
{
    public interface IFileShareReadWriteClientFactory
    {
        IFileShareReadWriteClient CreateClient(string baseAddress, string accessToken);

        IFileShareReadWriteClient CreateClient(string baseAddress, IAuthenticationTokenProvider tokenProvider);
    }
}
