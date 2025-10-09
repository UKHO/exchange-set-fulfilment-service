namespace UKHO.ADDS.EFS.FunctionalTests.Configuration
{
    /// <summary>
    /// Provides centralized configuration for test endpoints and URLs
    /// </summary>
    public class TestEndpointConfiguration
    {
        // Base endpoints
        public const string JobsEndpoint = "/jobs";
        public const string ProductNamesEndpoint = "/v2/exchangeSet/s100/productNames";
        public const string ProductVersionsEndpoint = "/v2/exchangeSet/s100/productVersions";
        public const string UpdatesSinceEndpoint = "/v2/exchangeSet/s100/updatesSince";

        // External URLs
        public const string ValidCallbackUrl = "https://valid.com/callback";
        public const string LocalhostCallbackMockUrl = "https://adds-mocks-efs/callback/callback";
        public const string AzureCallbackMockUrl = "https://adds-mocks-efs.redmoss-3083029b.uksouth.azurecontainerapps.io/callback/callback";
    }
}
