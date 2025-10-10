using UKHO.ADDS.Infrastructure.Results;
using UKHO.ADDS.Mocks.EFS.Models;
using UKHO.FakePenrose.S100SampleExchangeSets.SampleFileSources;

namespace UKHO.ADDS.Mocks.EFS.Services;

/// <summary>
/// Service for generating S100 sample exchange set files using UKHO.FakePenrose library
/// </summary>
public class S100DownloadFileService : IS100DownloadFileService
{
    private readonly IS100FileSource _s100FileSource;

    public S100DownloadFileService(IS100FileSource s100FileSource)
    {
        _s100FileSource = s100FileSource ?? throw new ArgumentNullException(nameof(s100FileSource));
    }

    public IResult<S100ExchangeSetFile> GenerateZipFile(string productName, int editionNumber = 1, int productUpdateNumber = 0)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(productName))
            {
                return Result.Failure<S100ExchangeSetFile>("Product name cannot be null or empty");
            }

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

            return Result.Success(exchangeSetFile);
        }
        catch (Exception ex)
        {
            return Result.Failure<S100ExchangeSetFile>($"Error generating ZIP file for product {productName}: {ex.Message}");
        }
    }
}
