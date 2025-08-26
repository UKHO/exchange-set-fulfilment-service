using System.Diagnostics.CodeAnalysis;

namespace UKHO.ADDS.EFS.Orchestrator.Models;

/// <summary>
/// Configuration model for Azure AD B2C settings.
/// </summary>
[ExcludeFromCodeCoverage]
public class AzureAdB2CConfiguration
{
    /// <summary>
    /// Gets or sets the Azure B2C instance URL.
    /// </summary>
    public string Instance { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Azure B2C domain.
    /// </summary>
    public string Domain { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Azure B2C client ID.
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Azure B2C sign up sign in policy.
    /// </summary>
    public string SignUpSignInPolicy { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Azure B2C tenant ID.
    /// </summary>
    public string TenantId { get; set; } = string.Empty;
}

/// <summary>
/// Configuration model for Azure AD settings.
/// </summary>
[ExcludeFromCodeCoverage]
public class AzureADConfiguration
{
    /// <summary>
    /// Gets or sets the Microsoft Online login URL.
    /// </summary>
    public string MicrosoftOnlineLoginUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Azure AD tenant ID.
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Azure AD client ID.
    /// </summary>
    public string ClientId { get; set; } = string.Empty;
}
