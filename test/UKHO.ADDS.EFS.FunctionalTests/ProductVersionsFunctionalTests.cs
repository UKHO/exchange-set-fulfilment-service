using UKHO.ADDS.EFS.FunctionalTests.Services;
using UKHO.ADDS.EFS.Infrastructure.Configuration.Namespaces;

namespace UKHO.ADDS.EFS.FunctionalTests
{
    public class ProductVersionsFunctionalTests : TestBase
    {
        //PBI 242767 - Input validation for the ESS API - Product Versions Endpoint
        [Theory]
        [InlineData(" [ { \"productName\": \"101GB40079ABCDEFG\", \"editionNumber\": 7, \"updateNumber\": 10 }, { \"productName\": \"102NO32904820801012\", \"editionNumber\": 36, \"updateNumber\": 0 }, { \"productName\": \"104US00_CHES_TYPE1_20210630_0600\", \"editionNumber\": 7, \"updateNumber\": 10 }, { \"productName\": \"111US00_ches_dcf8_20190703T00Z\", \"editionNumber\": 36, \"updateNumber\": 0 } ]", "https://valid.com/callback", HttpStatusCode.Accepted, "")] // Test Case 244565 - Valid input
        [InlineData("[ {  \"editionNumber\": 7, \"updateNumber\": 10 }, { \"editionNumber\": 36, \"updateNumber\": 0 } ]", "https://valid.com/callback", HttpStatusCode.BadRequest, "ProductName cannot be null or empty.")] // Test Case 244569 - Missing ProductName
        [InlineData("[ { \"productName\": \"\", \"editionNumber\": 7, \"updateNumber\": 10 } ]", "https://valid.com/callback", HttpStatusCode.BadRequest, "ProductName cannot be null or empty.")] // Test Case 244571 - Empty ProductName
        [InlineData("[ { \"productName\": \"null\", \"editionNumber\": 36, \"updateNumber\": 0 } ]", "https://valid.com/callback", HttpStatusCode.BadRequest, "'null' is not valid: it neither starts with a 3-digit S-100 code nor has length 8 for S-57.")] // Test Case 244571 - Invalid ProductName
        [InlineData("[ { \"productName\": 1234567890, \"editionNumber\": 7, \"updateNumber\": 10 } ]", "https://valid.com/callback", HttpStatusCode.InternalServerError, "")] // Test Case 244569 - Non-string ProductName
        [InlineData("[ { \"productName\": \"101GB40079ABCDEFG\", \"updateNumber\": 10 } ]", "https://valid.com/callback", HttpStatusCode.BadRequest, "Edition number must be a positive integer.")] // Test Case 245738 - Missing EditionNumber
        [InlineData("[ { \"productName\": \"101GB40079ABCDEFG\", \"editionNumber\": 0, \"updateNumber\": 10 }, { \"productName\": \"102NO32904820801012\", \"editionNumber\": -1, \"updateNumber\": 0 } ]", "https://valid.com/callback", HttpStatusCode.BadRequest, "Edition number must be a positive integer.")] // Test Case 245073 - Invalid EditionNumber
        [InlineData("[ { \"productName\": \"101GB40079ABCDEFG\", \"editionNumber\": \"\", \"updateNumber\": 10 }, { \"productName\": \"102NO32904820801012\", \"editionNumber\": \"abcd\", \"updateNumber\": 0 } ]", "https://valid.com/callback", HttpStatusCode.InternalServerError, "")] // Test Case 245029 - Empty or Non-integer EditionNumber
        [InlineData(" [ { \"productName\": \"101GB40079ABCDEFG\", \"editionNumber\": 7 } ]", "https://valid.com/callback", HttpStatusCode.BadRequest, "Update number must be zero or a positive integer.")] // Test Case 245040 - Missing UpdateNumber
        [InlineData(" [ { \"productName\": \"101GB40079ABCDEFG\", \"editionNumber\": 7, \"updateNumber\": -1 } ]", "https://valid.com/callback", HttpStatusCode.BadRequest, "Update number must be zero or a positive integer.")] // Test Case 245038 - Invalid UpdateNumber
        [InlineData(" [ { \"productName\": \"101GB40079ABCDEFG\", \"editionNumber\": 7, \"updateNumber\": \"\" }, { \"productName\": \"102NO32904820801012\", \"editionNumber\": 36, \"updateNumber\": \"abcd\" } ]", "https://valid.com/callback", HttpStatusCode.InternalServerError, "")] // Test Case 245715 - Empty or Non-integer UpdateNumber
        [InlineData(" [  ] ", "https://valid.com/callback", HttpStatusCode.BadRequest, "ProductVersions cannot be empty.")] // Test Case 244570 - Empty array
        [InlineData(" [  \"\" ] ", "https://valid.com/callback", HttpStatusCode.InternalServerError, "")] // Test Case 245718 - Array with Empty string
        [InlineData(" { \"productName\": \"101GB40079ABCDEFG\", \"editionNumber\": 7, \"updateNumber\": 10 }, { \"productName\": \"102NO32904820801012\", \"editionNumber\": 36, \"updateNumber\": 0 } ", "https://valid.com/callback", HttpStatusCode.InternalServerError, "")] // Test Case 245718 - Invalid json body
        [InlineData(" [ { } ] ", "https://valid.com/callback", HttpStatusCode.BadRequest, "ProductName cannot be null or empty.")] // Test Case 245718 - Array with empty object
        [InlineData(" [ { \"productName\": \"101GB40079ABCDEFG\", \"editionNumber\": 7, \"updateNumber\": 10 }, { \"productName\": \"102NO32904820801012\", \"editionNumber\": 36, \"updateNumber\": 0 }, { \"productName\": \"104US00_CHES_TYPE1_20210630_0600\", \"editionNumber\": 7, \"updateNumber\": 10 }, { \"productName\": \"111US00_ches_dcf8_20190703T00Z\", \"editionNumber\": 36, \"updateNumber\": 0 } ]", "http://valid.com/callback", HttpStatusCode.BadRequest, "Invalid callbackUri format.")] // Test Case 244581 - Invalid CallBackUrl
        [InlineData(" [ { \"productName\": \"101GB40079ABCDEFG\", \"editionNumber\": 7, \"updateNumber\": 10 }, { \"productName\": \"102NO32904820801012\", \"editionNumber\": 0, \"updateNumber\": 0 }, { \"productName\": \"\", \"editionNumber\": 7, \"updateNumber\": -1 }, { \"productName\": \"111US00_ches_dcf8_20190703T00Z\", \"editionNumber\": -1, \"updateNumber\": 0 } ]", "https://valid.com/callback", HttpStatusCode.BadRequest, "ProductName cannot be null or empty.")] // Test Case 245047 - Combination of valid and invalid inputs
        public async Task ValidateProductVersionsEndpoint(string productVersions, string callbackUri, HttpStatusCode expectedStatusCode, string expectedErrorMessage)
        {
            var httpClient = App!.CreateHttpClient(ProcessNames.OrchestratorService);

            await OrchestratorCommands.VerifyProductVersionEndpointResponse(productVersions, callbackUri,
                        httpClient, expectedStatusCode, expectedErrorMessage, 0);
        }
    }
}
