namespace UKHO.ADDS.EFS.Configuration.Authentication
{
    public class EfsManagedIdentityConfiguration
    {
        public required string EfsClientId { get; set; }
        public required string FssResourceId { get; set; }
        public required string ScsResourceId { get; set; }
        public double DeductTokenExpiryMinutes { get; set; }
    }
}
