using System.Text.RegularExpressions;
using UKHO.ADDS.Infrastructure.Results;
using UKHO.FakePenrose.S100SampleExchangeSets.SampleFileSources;

namespace UKHO.ADDS.Mocks.EFS.Services;

/// <summary>
/// Service for generating S100 sample exchange set files using UKHO.FakePenrose library
/// </summary>
public class S100SampleExchangeSetService : IS100SampleExchangeSetService
{
    private readonly IS100FileSource _s100FileSource;
    private readonly ILogger<S100SampleExchangeSetService> _logger;

    // S-100 product name patterns
    private static readonly Dictionary<string, Regex> ProductPatterns = new()
    {
        { "S101", new Regex(@"^101[A-Z]{2}[A-Za-z0-9]+$", RegexOptions.Compiled) }, // S-101: Electronic Navigational Charts
        { "S102", new Regex(@"^102[A-Z]{2}[A-Za-z0-9]+$", RegexOptions.Compiled) }, // S-102: Bathymetric Surface
        { "S104", new Regex(@"^104[A-Z]{2}[A-Za-z0-9_]+$", RegexOptions.Compiled) }, // S-104: Water Level Information
        { "S111", new Regex(@"^111[A-Z]{2}[A-Za-z0-9_]+$", RegexOptions.Compiled) }  // S-111: Surface Currents
    };

    public S100SampleExchangeSetService(ILogger<S100SampleExchangeSetService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _s100FileSource = new S100FileSource();
    }

    /// <inheritdoc />
    public IResult<S100ExchangeSetFile> GenerateZipFile(string productName, int editionNumber = 1, int productUpdateNumber = 0)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(productName))
            {
                return Result.Failure<S100ExchangeSetFile>("Product name cannot be null or empty");
            }

            if (!IsValidProductName(productName))
            {
                return Result.Failure<S100ExchangeSetFile>($"Invalid product name format: {productName}. Expected S-100 naming convention (e.g., 101GB12345678, 102GBTD5N5050W00120)");
            }

            _logger.LogInformation("Generating ZIP file for product: {ProductName}, Edition: {Edition}, Update: {Update}", 
                productName, editionNumber, productUpdateNumber);

            var zipFile = _s100FileSource.GetZipFile(productName, editionNumber, productUpdateNumber);

            if (zipFile?.FileStream == null)
            {
                return Result.Failure<S100ExchangeSetFile>($"Failed to generate ZIP file for product: {productName}");
            }

            var exchangeSetFile = new S100ExchangeSetFile
            {
                FileName = zipFile.FileName,
                MimeType = zipFile.MimeType,
                FileStream = zipFile.FileStream,
                Size = zipFile.FileStream.Length
            };

            _logger.LogInformation("Successfully generated ZIP file: {FileName} ({Size} bytes) for product: {ProductName}", 
                exchangeSetFile.FileName, exchangeSetFile.Size, productName);

            return Result.Success(exchangeSetFile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating ZIP file for product: {ProductName}", productName);
            return Result.Failure<S100ExchangeSetFile>($"Error generating ZIP file for product {productName}: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public IResult<S100ExchangeSetFile> GenerateZipFile(string productName, int editionNumber, int productUpdateNumber, int dataProviderIndexOverride)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(productName))
            {
                return Result.Failure<S100ExchangeSetFile>("Product name cannot be null or empty");
            }

            if (!IsValidProductName(productName))
            {
                return Result.Failure<S100ExchangeSetFile>($"Invalid product name format: {productName}. Expected S-100 naming convention (e.g., 101GB12345678, 102GBTD5N5050W00120)");
            }

            _logger.LogInformation("Generating ZIP file for product: {ProductName}, Edition: {Edition}, Update: {Update}, DataProvider: {DataProvider}", 
                productName, editionNumber, productUpdateNumber, dataProviderIndexOverride);

            var zipFile = _s100FileSource.GetZipFile(productName, editionNumber, productUpdateNumber, dataProviderIndexOverride);

            if (zipFile?.FileStream == null)
            {
                return Result.Failure<S100ExchangeSetFile>($"Failed to generate ZIP file for product: {productName}");
            }

            var exchangeSetFile = new S100ExchangeSetFile
            {
                FileName = zipFile.FileName,
                MimeType = zipFile.MimeType,
                FileStream = zipFile.FileStream,
                Size = zipFile.FileStream.Length
            };

            _logger.LogInformation("Successfully generated ZIP file: {FileName} ({Size} bytes) for product: {ProductName} with DataProvider: {DataProvider}", 
                exchangeSetFile.FileName, exchangeSetFile.Size, productName, dataProviderIndexOverride);

            return Result.Success(exchangeSetFile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating ZIP file for product: {ProductName} with DataProvider: {DataProvider}", productName, dataProviderIndexOverride);
            return Result.Failure<S100ExchangeSetFile>($"Error generating ZIP file for product {productName}: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public bool IsValidProductName(string productName)
    {
        if (string.IsNullOrWhiteSpace(productName) || productName.Length < 5)
        {
            return false;
        }

        // Extract product type (first 3 characters)
        var productType = productName[..3];
        var productKey = $"S{productType}";

        if (ProductPatterns.TryGetValue(productKey, out var pattern))
        {
            return pattern.IsMatch(productName);
        }

        _logger.LogWarning("Unsupported product type: {ProductType} in product name: {ProductName}", productType, productName);
        return false;
    }
}
