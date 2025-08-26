using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UKHO.ADDS.EFS.Orchestrator.Controllers;
using UKHO.ADDS.EFS.Orchestrator.Models;
using UKHO.ADDS.EFS.Orchestrator.Services.Products;

namespace UKHO.ADDS.EFS.Orchestrator.Controllers;

/// <summary>
/// Controller for product data operations.
/// </summary>
[Authorize]
[Route("api/[controller]")]
public class ProductDataController : BaseController<ProductDataController>
{
    private readonly IProductDataService productDataService;

    public ProductDataController(
        IHttpContextAccessor httpContextAccessor,
        IProductDataService productDataService)
        : base(httpContextAccessor)
    {
        this.productDataService = productDataService ?? throw new ArgumentNullException(nameof(productDataService));
    }

    /// <summary>
    /// Creates product data by product identifiers.
    /// </summary>
    /// <param name="productIdentifierRequest">The product identifier request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Created product data.</returns>
    [HttpPost("create")]
    public async Task<IActionResult> CreateProductDataByProductIdentifiers(
        [FromBody] object productIdentifierRequest,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var azureAdB2C = new AzureAdB2C()
            {
                AudToken = TokenAudience,
                IssToken = TokenIssuer
            };

            var productDetail = await productDataService.CreateProductDataByProductIdentifiers(productIdentifierRequest, azureAdB2C, cancellationToken);

            return Ok(productDetail);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { error = "An error occurred while processing the request" });
        }
    }
}
