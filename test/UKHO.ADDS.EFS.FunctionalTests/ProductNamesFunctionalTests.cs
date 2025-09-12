using UKHO.ADDS.EFS.FunctionalTests.Services;
using UKHO.ADDS.EFS.Infrastructure.Configuration.Namespaces;

namespace UKHO.ADDS.EFS.FunctionalTests
{
    public class ProductNamesFunctionalTests : TestBase
    {
        //PBI 242670 - Input validation for the ESS API - Product Name Endpoint
        [Theory]
        [InlineData(new object[] { "101GB40079ABCDEFG", "102NO32904820801012", "104US00_CHES_TYPE1_20210630_0600", "111US00_ches_dcf8_20190703T00Z" }, "https://valid.com/callback", HttpStatusCode.Accepted, "")] // Test Case 243519 - Valid input
        [InlineData(new object[] { "101GB40079ABCDEFG", "102NO32904820801012" }, "", HttpStatusCode.Accepted, "")] // Test Case 243519 - Valid input with valid callBackURI
        [InlineData(new object[] { "112GB40079ABCDEFG" }, "https://valid.com/callback", HttpStatusCode.BadRequest, "112GB40079ABCDEFG' starts with digits '112' but that is not a valid S-100 product")] // Test Case 245717 -Invalid Product
        [InlineData(new object[] { }, "https://valid.com/callback", HttpStatusCode.BadRequest, "Either body is null or malformed.")] // Test Case 243604 - Empty array
        [InlineData(new object[] { "" }, "https://valid.com/callback", HttpStatusCode.BadRequest, "ProductName cannot be null or empty.")] //Test Case 243605 - Array with Empty string
        [InlineData(new object[] { "101GB40079ABCDEFG" }, "InvalidCallbackUri", HttpStatusCode.BadRequest, "Please enter a valid callback URI in HTTPS format.")] // Test Case 245020 - Invalid CallBackUrl
        [InlineData(new object[] { "101GB40079ABCDEFG", 123, 456, 789 }, "https://valid.com/callback", HttpStatusCode.BadRequest, "Either body is null or malformed.")] //Test Case 243659 - Mixed valid and invalid data types
        public async Task ValidateProductNamesEndpoint(object[] productNames, string? callbackUri, HttpStatusCode expectedStatusCode, string expectedErrorMessage)
        {
            var httpClient = App!.CreateHttpClient(ProcessNames.OrchestratorService);

            await OrchestratorCommands.VerifyProductNamesEndpointResponse(productNames, httpClient, callbackUri, expectedStatusCode, expectedErrorMessage);
        }
    }
}
