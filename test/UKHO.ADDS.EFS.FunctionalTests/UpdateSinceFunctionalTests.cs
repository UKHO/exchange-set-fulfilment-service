using UKHO.ADDS.EFS.FunctionalTests.Services;
using UKHO.ADDS.EFS.Infrastructure.Configuration.Namespaces;

namespace UKHO.ADDS.EFS.FunctionalTests
{
    public class UpdateSinceFunctionalTests : TestBase
    {
        //PBI 242767 - Input validation for the ESS API - Update Since Endpoint
        [Theory]
        [InlineData("https://valid.com/callback", "s101", HttpStatusCode.Accepted, "")] // Test Case 244582 - Valid Format
        [InlineData("http://invalid/callback", "s101", HttpStatusCode.BadRequest, "Invalid callbackUri format.")] // Test Case 244586 -  Invalid CallBack Uri Format
        [InlineData("https://valid.com/callback", "SABC", HttpStatusCode.BadRequest, "productIdentifier must be exactly 4 characters: start with 'S' or 's' followed by three digits, with no spaces or extra characters.")] // Test Case 244907 - Invalid Product Identifier Format
        public async Task ValidateUpdateSinceEndpointWithValidDates(string callbackUri, string productIdentifier, HttpStatusCode expectedStatusCode, string expectedErrorMessage)
        {
            var httpClient = App!.CreateHttpClient(ProcessNames.OrchestratorService);

            var sinceDateTime = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

            await OrchestratorCommands.VerifyUpdateSinceEndpointResponse(sinceDateTime, callbackUri,
                productIdentifier, httpClient, expectedStatusCode, expectedErrorMessage);
        }

        [Theory]
        [InlineData("2025-09-29", "https://valid.com/callback", "s102", HttpStatusCode.BadRequest, "Provided updatesSince is either invalid or invalid format, the valid format is 'ISO 8601 format' (e.g. '2025-09-29T00:00:00Z').")] // Test Case 244583 - Invalid Format
        [InlineData("2015-09-03T07:28:00.000Z", "https://valid.com/callback", "s111", HttpStatusCode.BadRequest, "Date time provided is more than 28 days in the past.")] // Test Case 245720 - Date more than 28 days in the past
        [InlineData("9999-09-03T07:28:00.000Z", "https://valid.com/callback", "s104", HttpStatusCode.BadRequest, "sinceDateTime cannot be a future date.")] // Test Case 245121 - Future Date
        public async Task ValidateUpdateSinceEndPointWithInvalidDates(string sinceDateTime, string callbackUri, string productIdentifier, HttpStatusCode expectedStatusCode, string expectedErrorMessage)
        {
            var httpClient = App!.CreateHttpClient(ProcessNames.OrchestratorService);

            await OrchestratorCommands.VerifyUpdateSinceEndpointResponse(sinceDateTime, callbackUri,
                productIdentifier, httpClient, expectedStatusCode, expectedErrorMessage);
        }
    }
}
