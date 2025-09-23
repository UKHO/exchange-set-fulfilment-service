using UKHO.ADDS.Clients.Kiota.SalesCatalogueService.Models;
using UKHO.ADDS.EFS.FunctionalTests.Services;
using UKHO.ADDS.EFS.Infrastructure.Configuration.Namespaces;

namespace UKHO.ADDS.EFS.FunctionalTests
{
    public class UpdateSinceFunctionalTests : TestBase
    {
        //PBI 242767 - Input validation for the ESS API - Update Since Endpoint
        [Theory]
        [InlineData("2025-09-29", "https://valid.com/callback", "s102", HttpStatusCode.BadRequest, "Provided updatesSince is either invalid or invalid format, the valid format is 'ISO 8601 format' (e.g. '2025-09-29T00:00:00Z')")] // Test Case 244583 - Invalid Format when time part is missing
        [InlineData("2025-08-25L07:8:00.00Z", "https://valid.com/callback", "s102", HttpStatusCode.BadRequest, "Provided updatesSince is either invalid or invalid format, the valid format is 'ISO 8601 format' (e.g. '2025-09-29T00:00:00Z')")] // Test Case 244583 - Invalid Format
        [InlineData("", "https://valid.com/callback", "s101", HttpStatusCode.BadRequest, "No UpdateSince date time provided")] // Test Case 246905 - Empty sinceDateTime
        [InlineData("null", "https://valid.com/callback", "s101", HttpStatusCode.BadRequest, "Provided updatesSince is either invalid or invalid format, the valid format is 'ISO 8601 format' (e.g. '2025-09-29T00:00:00Z')")] // Test Case 246905 - Null sinceDateTime
        public async Task ValidateUpdateSinceEndPointWithInvalidDates(string sinceDateTime, string callbackUri, string productIdentifier, HttpStatusCode expectedStatusCode, string expectedErrorMessage)
        {
            var httpClient = App!.CreateHttpClient(ProcessNames.OrchestratorService);

            await OrchestratorCommands.VerifyUpdateSinceEndpointResponse(sinceDateTime, callbackUri,
                productIdentifier, httpClient, expectedStatusCode, expectedErrorMessage);
        }

        [Theory]
        [InlineData("http://invalid.com/callback", "s101", HttpStatusCode.BadRequest, "URI is malformed or does not use HTTPS")] // Test Case 244586 -  Invalid CallBack Uri Format
        [InlineData("", "s102", HttpStatusCode.Accepted, "")] // Test Case 244585 -  Valid input with blank CallBack Uri
        public async Task ValidateUpdateSinceEndpointWithInvalidAndBlankCallBackUri(string callbackUri, string productIdentifier, HttpStatusCode expectedStatusCode, string expectedErrorMessage)
        {
            var httpClient = App!.CreateHttpClient(ProcessNames.OrchestratorService);

            var sinceDateTime = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

            await OrchestratorCommands.VerifyUpdateSinceEndpointResponse(sinceDateTime, callbackUri,
                productIdentifier, httpClient, expectedStatusCode, expectedErrorMessage);
        }

        [Theory]
        [InlineData(-28, "https://valid.com/callback", "s101", HttpStatusCode.BadRequest, "Date time provided is more than 28 days in the past")] // Test Case 244584 - Date is of 28th day in the past
        [InlineData(-14, "https://valid.com/callback", "s102", HttpStatusCode.Accepted, "")] // Test Case 245730 - Past Date less than 28 days
        [InlineData(-35, "https://valid.com/callback", "s104", HttpStatusCode.BadRequest, "Date time provided is more than 28 days in the past")] // Test Case 245720 - Date more than 28 days in the past
        [InlineData(1, "https://valid.com/callback", "s111", HttpStatusCode.BadRequest, "UpdateSince date cannot be a future date")] // Test Case 245121 - Future Date
        public async Task ValidateUpdateSinceEndpointWithPastAndFutureDates(int days, string callbackUri, string productIdentifier, HttpStatusCode expectedStatusCode, string expectedErrorMessage)
        {
            var httpClient = App!.CreateHttpClient(ProcessNames.OrchestratorService);

            var sinceDateTime = DateTime.UtcNow.AddDays(days).ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

            await OrchestratorCommands.VerifyUpdateSinceEndpointResponse(sinceDateTime, callbackUri,
                productIdentifier, httpClient, expectedStatusCode, expectedErrorMessage);
        }

        //PBI 246464 - Consume ESS API - Update Since Endpoint
        [Theory]
        [InlineData("https://valid.com/callback", "s101", HttpStatusCode.Accepted, "UpdateSinceProduct.zip")] // Test Case 247827 - Valid Format
        public async Task ValidateConsumeUpdateSinceEndpointWithValidDates(string callbackUri, string productIdentifier, HttpStatusCode expectedStatusCode, string zipFileName)
        {
            var httpClient = App!.CreateHttpClient(ProcessNames.OrchestratorService);
            
            var sinceDateTime = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

            var jobId = await OrchestratorCommands.UpdateSinceInCustomAssemblyPipelineSubmitJobAsync(httpClient, productIdentifier, callbackUri, sinceDateTime);

            await OrchestratorCommands.WaitForJobCompletionAsync(httpClient, jobId);

            await OrchestratorCommands.VerifyBuildStatusAsync(httpClient, jobId);

            var exchangeSetDownloadPath = await ZipStructureComparer.DownloadExchangeSetAsZipAsync(jobId, App!);
            var sourceZipPath = Path.Combine(ProjectDirectory!, "TestData", zipFileName);

            ZipStructureComparer.CompareZipFilesExactMatchForUpdateSince(sourceZipPath, exchangeSetDownloadPath);
        }


        [Theory]
        [InlineData("https://valid.com/callback", "S333", HttpStatusCode.BadRequest, "productIdentifier must be exactly 4 characters: start with 'S' or 's' followed by three digits, with no spaces or extra characters")] // Test Case 247830 - Invalid Product Identifier 
        [InlineData("https://valid.com/callback", "S101, s102", HttpStatusCode.BadRequest, "productIdentifier must be exactly 4 characters: start with 'S' or 's' followed by three digits, with no spaces or extra characters")] // Test Case 247831 - Multiple Product Identifier
        public async Task ValidateConsumeUpdateSinceEndpointWithInvalidProductIdentifier(string callbackUri, string productIdentifier, HttpStatusCode expectedStatusCode, string expectedErrorMessage)
        {
            var httpClient = App!.CreateHttpClient(ProcessNames.OrchestratorService);

            var sinceDateTime = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

            await OrchestratorCommands.VerifyUpdateSinceEndpointResponse(sinceDateTime, callbackUri,
                productIdentifier, httpClient, expectedStatusCode, expectedErrorMessage);
        }

    }
}
