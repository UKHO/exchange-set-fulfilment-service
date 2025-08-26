using UKHO.ADDS.EFS.Orchestrator.Models;

namespace UKHO.ADDS.EFS.Orchestrator.Services.Products;

/// <summary>
/// Service interface for product data operations.
/// </summary>
public interface IProductDataService
{
    /// <summary>
    /// Creates product data by product identifiers.
    /// </summary>
    /// <param name="productIdentifierRequest">The product identifier request.</param>
    /// <param name="azureAdB2C">The Azure AD B2C information.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Product data for the specified identifiers.</returns>
    Task<object> CreateProductDataByProductIdentifiers(object productIdentifierRequest, AzureAdB2C azureAdB2C, CancellationToken cancellationToken = default);
}
