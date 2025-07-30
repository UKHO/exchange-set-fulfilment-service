namespace UKHO.ADDS.EFS.Auth
{
    public interface IAuthScsTokenProvider
    {
        public Task<string> GetManagedIdentityAuthAsync(string resource);
    }
}
