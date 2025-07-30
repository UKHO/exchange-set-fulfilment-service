namespace UKHO.ADDS.EFS.Configuration.Authentication
{
    public class EfsManagedIdentityConfiguration
    {
        public required string EfsClientId { get; set; }
        public required string FssClientId { get; set; }
        public required string ScsClientId { get; set; }
        public double DeductTokenExpiryMinutes { get; set; }
    }
}
