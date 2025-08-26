using UKHO.ADDS.EFS.Orchestrator.Models;
using UKHO.ADDS.EFS.Orchestrator.Services.Authorization;
using UKHO.ADDS.EFS.Orchestrator.Services.Products;

namespace UKHO.ADDS.EFS.Orchestrator.Services.Products.Implementation;

/// <summary>
/// Implementation of product data service.
/// </summary>
public class ProductDataService : IProductDataService
{
    private readonly IConfiguration configuration;
    private readonly IAzureAdB2CHelper azureAdB2CHelper;

    public ProductDataService(IConfiguration configuration, IAzureAdB2CHelper azureAdB2CHelper)
    {
        this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        this.azureAdB2CHelper = azureAdB2CHelper ?? throw new ArgumentNullException(nameof(azureAdB2CHelper));
    }

    public async Task<object> CreateProductDataByProductIdentifiers(object productIdentifierRequest, AzureAdB2C azureAdB2C, CancellationToken cancellationToken = default)
    {
        // Extract correlation ID from request if available
        var correlationId = ExtractCorrelationId(productIdentifierRequest);

        // Use AzureAdB2CHelper to determine if user is Azure B2C user
        var isAzureB2CUser = azureAdB2CHelper.IsAzureB2CUser(azureAdB2C, correlationId);

        // Simulate product data creation based on identifiers
        await Task.Delay(100, cancellationToken); // Simulate async operation

        return new
        {
            Message = "Product data created successfully",
            ProductIdentifierRequest = productIdentifierRequest,
            AzureAdB2C = new
            {
                azureAdB2C.AudToken,
                azureAdB2C.IssToken
            },
            IsAzureB2CUser = isAzureB2CUser,
            CorrelationId = correlationId,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static string ExtractCorrelationId(object productIdentifierRequest)
    {
        // Try to extract correlation ID from the request object
        // This is a simple implementation - in real scenarios you might use reflection or a more sophisticated approach
        if (productIdentifierRequest is { } request)
        {
            var requestType = request.GetType();
            var correlationIdProperty = requestType.GetProperty("CorrelationId");
            if (correlationIdProperty != null)
            {
                var correlationId = correlationIdProperty.GetValue(request)?.ToString();
                if (!string.IsNullOrEmpty(correlationId))
                {
                    return correlationId;
                }
            }
        }

        // Return a default correlation ID if not found
        return Guid.NewGuid().ToString();
    }
}
