using UKHO.ADDS.Infrastructure.Results;

namespace UKHO.ADDS.Mocks.EFS.Services;

/// <summary>
/// Interface for generating S100 sample exchange set files
/// </summary>
public interface IS100SampleExchangeSetService
{
    /// <summary>
    /// Generates a ZIP file for the specified S100 product
    /// </summary>
    /// <param name="productName">The product name following S-100 naming convention</param>
    /// <param name="editionNumber">Edition number (default: 1)</param>
    /// <param name="productUpdateNumber">Product update number (default: 0)</param>
    /// <returns>A result containing the ZIP file stream and metadata</returns>
    IResult<S100ExchangeSetFile> GenerateZipFile(string productName, int editionNumber = 1, int productUpdateNumber = 0);

    /// <summary>
    /// Generates a ZIP file for the specified S100 product with data provider override
    /// </summary>
    /// <param name="productName">The product name following S-100 naming convention</param>
    /// <param name="editionNumber">Edition number</param>
    /// <param name="productUpdateNumber">Product update number</param>
    /// <param name="dataProviderIndexOverride">Data provider index override</param>
    /// <returns>A result containing the ZIP file stream and metadata</returns>
    IResult<S100ExchangeSetFile> GenerateZipFile(string productName, int editionNumber, int productUpdateNumber, int dataProviderIndexOverride);

    /// <summary>
    /// Validates if the product name follows S-100 naming convention
    /// </summary>
    /// <param name="productName">The product name to validate</param>
    /// <returns>True if valid, false otherwise</returns>
    bool IsValidProductName(string productName);
}

/// <summary>
/// Represents an S100 exchange set file with metadata
/// </summary>
public class S100ExchangeSetFile
{
    public string FileName { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public Stream FileStream { get; set; } = Stream.Null;
    public long Size { get; set; }
}
