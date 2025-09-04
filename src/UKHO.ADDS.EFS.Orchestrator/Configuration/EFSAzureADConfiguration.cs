namespace UKHO.ADDS.EFS.Orchestrator.Configuration
{
    /// <summary>
    /// Configuration settings for Azure AD authentication
    /// </summary>
    public class EFSAzureADConfiguration
    {
        /// <summary>
        /// Microsoft Online Login URL
        /// </summary>
        public string MicrosoftOnlineLoginUrl { get; set; } = string.Empty;

        /// <summary>
        /// Azure AD Tenant ID
        /// </summary>
        public string TenantId { get; set; } = string.Empty;

        /// <summary>
        /// Azure AD Client ID (Application ID)
        /// </summary>
        public string ClientId { get; set; } = string.Empty;

        /// <summary>
        /// Gets the complete authority URL for Azure AD
        /// </summary>
        public string Authority => $"{MicrosoftOnlineLoginUrl.TrimEnd('/')}/{TenantId}";

        /// <summary>
        /// Indicates whether authentication is configured (has ClientId)
        /// </summary>
        public bool IsAuthenticationEnabled => !string.IsNullOrWhiteSpace(ClientId);

        /// <summary>
        /// Validates the Azure AD configuration for production environments.
        /// Throws InvalidOperationException if configuration is incomplete.
        /// </summary>
        /// <param name="environmentName">The environment name for error messaging</param>
        /// <exception cref="InvalidOperationException">Thrown when configuration is incomplete for production environments</exception>
        public void ValidateForProductionEnvironment(string environmentName)
        {
            if (string.IsNullOrWhiteSpace(ClientId))
            {
                throw new InvalidOperationException(
                    $"Azure AD ClientId is required for environment '{environmentName}' but is not configured. " +
                    "Please ensure EFSAzureADConfiguration.ClientId is properly set in the configuration.");
            }

            if (string.IsNullOrWhiteSpace(TenantId))
            {
                throw new InvalidOperationException(
                    $"Azure AD TenantId is required for environment '{environmentName}' but is not configured. " +
                    "Please ensure EFSAzureADConfiguration.TenantId is properly set in the configuration.");
            }

            if (string.IsNullOrWhiteSpace(MicrosoftOnlineLoginUrl))
            {
                throw new InvalidOperationException(
                    $"Azure AD MicrosoftOnlineLoginUrl is required for environment '{environmentName}' but is not configured. " +
                    "Please ensure EFSAzureADConfiguration.MicrosoftOnlineLoginUrl is properly set in the configuration.");
            }
        }
    }
}
