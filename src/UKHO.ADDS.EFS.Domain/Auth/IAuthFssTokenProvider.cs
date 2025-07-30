namespace UKHO.ADDS.EFS.Auth
{
    public interface IAuthFssTokenProvider
    {
        public Task<string> GetManagedIdentityAuthAsync(string resource);
    }
}
