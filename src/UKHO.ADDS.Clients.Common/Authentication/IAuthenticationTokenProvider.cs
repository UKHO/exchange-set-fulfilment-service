namespace UKHO.ADDS.Clients.Common.Authentication
{
    public interface IAuthenticationTokenProvider
    {
        Task<string> GetTokenAsync();
    }
}
