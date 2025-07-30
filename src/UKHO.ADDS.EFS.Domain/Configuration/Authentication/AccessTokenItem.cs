namespace UKHO.ADDS.EFS.Configuration.Authentication
{
    public class AccessTokenItem
    {
        public string AccessToken { get; set; } = string.Empty;
        public DateTime ExpiresIn { get; set; }
    }
}
