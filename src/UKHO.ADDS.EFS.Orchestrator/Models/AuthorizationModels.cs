using UKHO.ADDS.EFS.Jobs;

namespace UKHO.ADDS.EFS.Orchestrator.Models;

/// <summary>
/// Represents Azure AD B2C information for product data operations.
/// </summary>
public class AzureAdB2C
{
    /// <summary>
    /// Gets or sets the audience token.
    /// </summary>
    public string? AudToken { get; set; }

    /// <summary>
    /// Gets or sets the issuer token.
    /// </summary>
    public string? IssToken { get; set; }
}
