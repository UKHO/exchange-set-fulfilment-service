using System.Text.RegularExpressions;
using UKHO.ADDS.Mocks.EFS.Services;
using UKHO.ADDS.Mocks.Headers;
using UKHO.ADDS.Mocks.Markdown;
using UKHO.ADDS.Mocks.States;
using UKHO.FakePenrose.S100SampleExchangeSets.SampleFileSources;

namespace UKHO.ADDS.Mocks.EFS.Override.Mocks.fss
{
    public class FileDownloadEndpoint : FssEndpointBase
    {
        public override void RegisterSingleEndpoint(IEndpointMock endpoint) =>
            endpoint.MapGet("/batch/{batchId}/files/{fileName}", (string batchId, string fileName, HttpRequest request, HttpResponse response) =>
            {
                var (state, correlationId) = SetupRequest(request, response);

                return state switch
                {
                    WellKnownState.Default => HandleDefaultState(fileName, response, correlationId),
                    WellKnownState.BadRequest => Results.Json(CreateErrorResponse(correlationId, "File Download", "Invalid batchId."), statusCode: 400),
                    _ => ProcessCommonStates(state, correlationId, "File Download") ?? WellKnownStateHandler.HandleWellKnownState(state)
                };
            })
                .Produces<string>()
                .WithEndpointMetadata(endpoint, d =>
                {
                    d.Append(new MarkdownHeader("Download a file", 3));
                    d.Append(new MarkdownParagraph("Downloads S100 sample exchange sets for ZIP files with format 'ProductName_Edition_Update.zip' (e.g., 101CA100129_1_0.zip)."));
                    d.Append(new MarkdownParagraph("**S100 Filename Format:**"));
                    d.Append(new MarkdownParagraph("- Required: `{ProductName}_{EditionNumber}_{ProductUpdateNumber}.zip`"));
                    d.Append(new MarkdownParagraph("- Example: `101CA100129_1_0.zip` → Product: `101CA100129`, Edition: `1`, Update: `0`"));
                    d.Append(new MarkdownParagraph("- ProductName must start with S100 product codes: 101, 102, 104, or 111"));
                });

        private IResult HandleDefaultState(string fileName, HttpResponse response, string correlationId)
        {
            try
            {
                var s100FileSource = new S100FileSource();
                var s100Service = new S100DownloadFileService(s100FileSource);
                var (productName, editionNumber, productUpdateNumber) = ParseS100FileName(fileName);
                
                if (!string.IsNullOrEmpty(productName))
                {
                    var zipResult = s100Service.GenerateZipFile(productName, editionNumber, productUpdateNumber);
                    if (zipResult.IsSuccess(out var exchangeSetFile, out var error))
                    {
                        response.Headers["Content-Disposition"] = $"attachment; filename=\"{fileName}\"";
                        response.Headers["Content-Length"] = exchangeSetFile.Size.ToString();
                        return Results.File(exchangeSetFile.FileStream, exchangeSetFile.MimeType, fileName);
                    }
                }
            }
            catch (Exception ex)
            {
                // Fall through to error response
            }
            
            return Results.Json(CreateDetailsResponse(correlationId, InternalServerErrorMessage), statusCode: 500);
        }

        /// <summary>
        /// Parses S100 filename to extract productName, editionNumber, and productUpdateNumber
        /// </summary>
        /// <param name="fileName">The filename to parse</param>
        /// <returns>Tuple containing productName, editionNumber, and productUpdateNumber</returns>
        /// <exception cref="ArgumentException">Thrown when filename doesn't match expected S100 format</exception>
        private static (string productName, int editionNumber, int productUpdateNumber) ParseS100FileName(string fileName)
        {
            // Remove file extension
            var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);

            // Try to parse standard format first: ProductName_Edition_Update
            var standardPattern = @"^([0-9]{3}[A-Z]{2}[A-Za-z0-9_]+)_(\d+)_(\d+)$";
            var match = Regex.Match(nameWithoutExtension, standardPattern, RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));

            if (match.Success)
            {
                var productName = match.Groups[1].Value;
                var editionNumber = int.Parse(match.Groups[2].Value);
                var productUpdateNumber = int.Parse(match.Groups[3].Value);

                return (productName, editionNumber, productUpdateNumber);
            }

            // If filename doesn't match expected format, throw an exception
            throw new ArgumentException($"Filename '{fileName}' does not match expected S100 format 'ProductName_Edition_Update.zip' (e.g., '101CA100129_1_0.zip')", nameof(fileName));
        }
    }
}
