using UKHO.ADDS.Infrastructure.Results;
using UKHO.ADDS.Mocks.EFS.Models;

namespace UKHO.ADDS.Mocks.EFS.Services;

/// <summary>
/// Interface for generating S100 sample exchange set files
/// </summary>
public interface IS100DownloadFileService
{
    /// <summary>
    /// Generates a ZIP file for the specified S100 product
    /// </summary>
    /// <param name="productName">The product name following S-100 naming convention</param>
    /// <param name="editionNumber">Edition number (default: 1)</param>
    /// <param name="updateNumber">Product update number (default: 0)</param>
    /// <returns>A result containing the ZIP file stream and metadata</returns>
    IResult<S100ExchangeSetFile> GenerateZipFile(string productName, int editionNumber = 1, int updateNumber = 0);
}
