using UKHO.ADDS.EFS.Orchestrator.Models;

namespace UKHO.ADDS.EFS.Orchestrator.Services.Authorization;

/// <summary>
/// Interface for Azure AD B2C helper service.
/// </summary>
public interface IAzureAdB2CHelper
{
    /// <summary>
    /// Determines if the user is an Azure B2C user.
    /// </summary>
    /// <param name="azureAdB2C">The Azure AD B2C information.</param>
    /// <param name="correlationId">The correlation ID for logging.</param>
    /// <returns>True if the user is an Azure B2C user; otherwise, false.</returns>
    bool IsAzureB2CUser(AzureAdB2C azureAdB2C, string correlationId);
}
