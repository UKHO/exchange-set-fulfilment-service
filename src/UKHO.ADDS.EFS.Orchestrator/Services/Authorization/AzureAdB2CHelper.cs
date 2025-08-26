using Microsoft.Extensions.Options;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging;
using UKHO.ADDS.EFS.Orchestrator.Models;

namespace UKHO.ADDS.EFS.Orchestrator.Services.Authorization;

/// <summary>
/// Helper service for Azure AD B2C operations.
/// </summary>
public partial class AzureAdB2CHelper : IAzureAdB2CHelper
{
    private readonly ILogger<AzureAdB2CHelper> logger;
    private readonly IOptions<AzureAdB2CConfiguration> azureAdB2CConfiguration;
    private readonly IOptions<AzureADConfiguration> azureAdConfiguration;

    public AzureAdB2CHelper(
        ILogger<AzureAdB2CHelper> logger,
        IOptions<AzureAdB2CConfiguration> azureAdB2CConfiguration,
        IOptions<AzureADConfiguration> azureAdConfiguration)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.azureAdB2CConfiguration = azureAdB2CConfiguration ?? throw new ArgumentNullException(nameof(azureAdB2CConfiguration));
        this.azureAdConfiguration = azureAdConfiguration ?? throw new ArgumentNullException(nameof(azureAdConfiguration));
    }

    public bool IsAzureB2CUser(AzureAdB2C azureAdB2C, string correlationId)
    {
        bool isAzureB2CUser = false;
        string b2CAuthority = $"{azureAdB2CConfiguration.Value.Instance}{azureAdB2CConfiguration.Value.TenantId}/v2.0/"; // for B2C Token
        string adB2CAuthority = $"{azureAdConfiguration.Value.MicrosoftOnlineLoginUrl}{azureAdB2CConfiguration.Value.TenantId}/v2.0"; // for AdB2C Token
        string audience = azureAdB2CConfiguration.Value.ClientId;

        if (azureAdB2C.IssToken == b2CAuthority && azureAdB2C.AudToken == audience)
        {
            isAzureB2CUser = true;
        }
        else if (azureAdB2C.IssToken == adB2CAuthority && azureAdB2C.AudToken == audience)
        {
            isAzureB2CUser = true;
        }

        return isAzureB2CUser;
    }
}
