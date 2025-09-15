using UKHO.ADDS.EFS.FunctionalTests.Services;
using UKHO.ADDS.EFS.Infrastructure.Configuration.Namespaces;

namespace UKHO.ADDS.EFS.FunctionalTests
{
    public class ProductVersionsFunctionalTests : TestBase
    {
        //PBI 242767 - Input validation for the ESS API - Product Versions Endpoint
        [Theory]
        [InlineData(" [ { \"productName\": \"101GB40079ABCDEFG\", \"editionNumber\": 7, \"updateNumber\": 10 }, { \"productName\": \"102NO32904820801012\", \"editionNumber\": 36, \"updateNumber\": 0 }, { \"productName\": \"104US00_CHES_TYPE1_20210630_0600\", \"editionNumber\": 7, \"updateNumber\": 10 }, { \"productName\": \"111US00_ches_dcf8_20190703T00Z\", \"editionNumber\": 36, \"updateNumber\": 0 } ]", "https://valid.com/callback", HttpStatusCode.Accepted, "")] // Test Case 244565 - Valid input
        [InlineData(" [ { \"productName\": \"101GB40079ABCDEFG\", \"editionNumber\": 7, \"updateNumber\": 10 }, { \"productName\": \"102NO32904820801012\", \"editionNumber\": 36, \"updateNumber\": 0 }, { \"productName\": \"104US00_CHES_TYPE1_20210630_0600\", \"editionNumber\": 7, \"updateNumber\": 10 }, { \"productName\": \"111US00_ches_dcf8_20190703T00Z\", \"editionNumber\": 36, \"updateNumber\": 0 } ]", "", HttpStatusCode.Accepted, "")] // Test Case 244906 - Valid input with only CallBackUri key and value as empty
        
        public async Task ValidateProductVersionsEndpointWithValidInputs(string productVersions, string callbackUri, HttpStatusCode expectedStatusCode, string expectedErrorMessage)
        {
            var httpClient = App!.CreateHttpClient(ProcessNames.OrchestratorService);

            await OrchestratorCommands.VerifyProductVersionEndpointResponse(productVersions, callbackUri,
                        httpClient, expectedStatusCode, expectedErrorMessage, 0);
        }

        [Theory]
        [InlineData("[ {  \"editionNumber\": 7, \"updateNumber\": 10 }, { \"editionNumber\": 36, \"updateNumber\": 0 } ]", "https://valid.com/callback", HttpStatusCode.BadRequest, "ProductName cannot be null or empty.")] // Test Case 244569 - Missing ProductName
        [InlineData("[ { \"productName\": \"\", \"editionNumber\": 7, \"updateNumber\": 10 } ]", "https://valid.com/callback", HttpStatusCode.BadRequest, "ProductName cannot be null or empty.")] // Test Case 244571 - Empty ProductName
        [InlineData("[ { \"productName\": \"null\", \"editionNumber\": 36, \"updateNumber\": 0 } ]", "https://valid.com/callback", HttpStatusCode.BadRequest, "'null' is not valid: it neither starts with a 3-digit S-100 code nor has length 8 for S-57.")] // Test Case 244580 - Null value of ProductName
        [InlineData("[ { \"productName\": 1234567890, \"editionNumber\": 7, \"updateNumber\": 10 } ]", "https://valid.com/callback", HttpStatusCode.BadRequest, "Either body is null or malformed.")] // Test Case 247161 - Non-string ProductName
        [InlineData("[ { \"productName\": \"101GB40079ABCDEFG\", \"updateNumber\": 10 } ]", "https://valid.com/callback", HttpStatusCode.BadRequest, "Edition number must be a positive integer.")] // Test Case 245738 - Missing EditionNumber
        [InlineData("[ { \"productName\": \"101GB40079ABCDEFG\", \"editionNumber\": 0, \"updateNumber\": 10 }, { \"productName\": \"102NO32904820801012\", \"editionNumber\": -1, \"updateNumber\": 0 } ]", "https://valid.com/callback", HttpStatusCode.BadRequest, "Edition number must be a positive integer.")] // Test Case 245073 - Invalid EditionNumber
        [InlineData("[ { \"productName\": \"101GB40079ABCDEFG\", \"editionNumber\": \"\", \"updateNumber\": 10 }, { \"productName\": \"102NO32904820801012\", \"editionNumber\": \"abcd\", \"updateNumber\": 0 } ]", "https://valid.com/callback", HttpStatusCode.BadRequest, "Either body is null or malformed.")] // Test Case 245029 - Empty or Non-integer EditionNumber
        [InlineData(" [ { \"productName\": \"101GB40079ABCDEFG\", \"editionNumber\": 7 } ]", "https://valid.com/callback", HttpStatusCode.BadRequest, "Update number must be zero or a positive integer.")] // Test Case 245040 - Missing UpdateNumber
        [InlineData(" [ { \"productName\": \"101GB40079ABCDEFG\", \"editionNumber\": 7, \"updateNumber\": -1 } ]", "https://valid.com/callback", HttpStatusCode.BadRequest, "Update number must be zero or a positive integer.")] // Test Case 245038 - Invalid UpdateNumber
        [InlineData(" [ { \"productName\": \"101GB40079ABCDEFG\", \"editionNumber\": 7, \"updateNumber\": \"\" }, { \"productName\": \"102NO32904820801012\", \"editionNumber\": 36, \"updateNumber\": \"abcd\" } ]", "https://valid.com/callback", HttpStatusCode.BadRequest, "Either body is null or malformed.")] // Test Case 245715 - Empty or Non-integer UpdateNumber
        [InlineData(" [  ] ", "https://valid.com/callback", HttpStatusCode.BadRequest, "Either body is null or malformed.")] // Test Case 244570 - Empty array
        [InlineData(" [  \"\" ] ", "https://valid.com/callback", HttpStatusCode.BadRequest, "Either body is null or malformed.")] // Test Case 245718 - Array with Empty string
        [InlineData(" { \"productName\": \"101GB40079ABCDEFG\", \"editionNumber\": 7, \"updateNumber\": 10 }, { \"productName\": \"102NO32904820801012\", \"editionNumber\": 36, \"updateNumber\": 0 } ", "https://valid.com/callback", HttpStatusCode.BadRequest, "Either body is null or malformed.")] // Test Case 247169 - Invalid json body
        [InlineData(" [ { } ] ", "https://valid.com/callback", HttpStatusCode.BadRequest, "ProductName cannot be null or empty.")] // Test Case 247164 - Array with empty object
        [InlineData("", "https://valid.com/callback", HttpStatusCode.BadRequest, "Either body is null or malformed.")] // Test Case 247166 - Blank request body
        [InlineData(" [ { \"productName\": \"112GB40079ABCDEFG\", \"editionNumber\": 36, \"updateNumber\": 0 } ]", "https://valid.com/callback", HttpStatusCode.BadRequest, "'112GB40079ABCDEFG' starts with digits '112' but that is not a valid S-100 product.")] // Test Case 246904 - Invalid first three characters of S-100 product code in productName
        [InlineData(" [ { \"productName\": \"101GB40079ABCDEFG\", \"editionNumber\": 7, \"updateNumber\": 10 }, { \"productName\": \"102NO32904820801012\", \"editionNumber\": 36, \"updateNumber\": 0 }, { \"productName\": \"104US00_CHES_TYPE1_20210630_0600\", \"editionNumber\": 7, \"updateNumber\": 10 }, { \"productName\": \"111US00_ches_dcf8_20190703T00Z\", \"editionNumber\": 36, \"updateNumber\": 0 } ]", "http://invalid.com/callback", HttpStatusCode.BadRequest, "Please enter a valid callback URI in HTTPS format.")] // Test Case 244581 - Invalid CallBackUri
        [InlineData(" [ { \"productName\": \"101GB40079ABCDEFG\", \"editionNumber\": 7, \"updateNumber\": 10 }, { \"productName\": \"102NO32904820801012\", \"editionNumber\": 0, \"updateNumber\": 0 }, { \"productName\": \"\", \"editionNumber\": 7, \"updateNumber\": -1 }, { \"productName\": \"111US00_ches_dcf8_20190703T00Z\", \"editionNumber\": -1, \"updateNumber\": 0 } ]", "https://valid.com/callback", HttpStatusCode.BadRequest, "ProductName cannot be null or empty.")] // Test Case 245047 - Combination of valid and invalid inputs
        public async Task ValidateProductVersionsEndpointWithInvalidInputs(string productVersions, string callbackUri, HttpStatusCode expectedStatusCode, string expectedErrorMessage)
        {
            var httpClient = App!.CreateHttpClient(ProcessNames.OrchestratorService);

            await OrchestratorCommands.VerifyProductVersionEndpointResponse(productVersions, callbackUri,
                        httpClient, expectedStatusCode, expectedErrorMessage, 0);
        }
    }
}
