using Meziantou.Xunit;
using UKHO.ADDS.EFS.FunctionalTests.Services;
using xRetry;
using Xunit.Abstractions;

namespace UKHO.ADDS.EFS.FunctionalTests
{
    [Collection("Startup Collection")]
    [EnableParallelization] // Needed to parallelize inside the class, not just between classes
    public class ProductVersionsFunctionalTests : TestBase
    {
        private string _requestId = "";
        private string _endpoint = "/v2/exchangeSet/s100/productVersions";


        public ProductVersionsFunctionalTests(StartupFixture startup, ITestOutputHelper output) : base(startup, output)
        {
            _requestId = $"job-productVersionsAutoTest-" + Guid.NewGuid();
        }


        private async Task submitPostRequestAndCheckResponse(string requestId, object requestPayload, string endpoint, HttpStatusCode expectedStatusCode, string expectedErrorMessage)
        {
            var response = await OrchestratorCommands.PostRequestAsync(requestId, requestPayload, endpoint);
            Assert.Equal(expectedStatusCode, response.StatusCode);

            if (expectedStatusCode != HttpStatusCode.Accepted && expectedErrorMessage != "")
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                _output.WriteLine($"ResponseContent: {responseBody}");
                Assert.Contains(expectedErrorMessage, responseBody);
            }
        }


        private void setEndpoint(string? callbackUri)
        {
            if (callbackUri != null)
            {
                _endpoint = _endpoint + $"?callbackUri={callbackUri}";
            }
        }


        //PBI 242767 - Input validation for the ESS API - Product Versions Endpoint
        [RetryTheory(maxRetries: 1, delayBetweenRetriesMs: 5000)]
        [DisableParallelization] // This test runs in parallel with other tests. However, its test cases are run sequentially.
        [InlineData("[ { \"productName\": \"101GB40079ABCDEFG\", \"editionNumber\": 7, \"updateNumber\": 10 }, { \"productName\": \"102NO32904820801012\", \"editionNumber\": 36, \"updateNumber\": 0 }, { \"productName\": \"104US00_CHES_TYPE1_20210630_0600\", \"editionNumber\": 7, \"updateNumber\": 10 }, { \"productName\": \"111US00_ches_dcf8_20190703T00Z\", \"editionNumber\": 36, \"updateNumber\": 0 } ]", "https://valid.com/callback", HttpStatusCode.Accepted, "")] // Test Case 244565 - Valid input
        [InlineData("[ { \"productName\": \"101GB40079ABCDEFG\", \"editionNumber\": 7, \"updateNumber\": 10 }, { \"productName\": \"102NO32904820801012\", \"editionNumber\": 36, \"updateNumber\": 0 }, { \"productName\": \"104US00_CHES_TYPE1_20210630_0600\", \"editionNumber\": 7, \"updateNumber\": 10 }, { \"productName\": \"111US00_ches_dcf8_20190703T00Z\", \"editionNumber\": 36, \"updateNumber\": 0 } ]", "", HttpStatusCode.Accepted, "")] // Test Case 244906 - Valid input with only CallBackUri key and value as empty
        [InlineData("[ { \"productName\": \"101GB40079ABCDEFG\", \"editionNumber\": 7, \"updateNumber\": 10 }, { \"productName\": \"102NO32904820801012\", \"editionNumber\": 36, \"updateNumber\": 0 }, { \"productName\": \"104US00_CHES_TYPE1_20210630_0600\", \"editionNumber\": 7, \"updateNumber\": 10 }, { \"productName\": \"111US00_ches_dcf8_20190703T00Z\", \"editionNumber\": 36, \"updateNumber\": 0 } ]", null, HttpStatusCode.Accepted, "")] // no CallBackUri parameter in the URL as it is optional parameter
        public async Task ValidatePVPayloadWithValidInputs(string productVersions, string? callbackUri, HttpStatusCode expectedStatusCode, string expectedErrorMessage)
        {

            setEndpoint(callbackUri);

            _output.WriteLine($"RequestId: {_requestId}\nRequest EndPoint: {_endpoint}\nRequest Payload: {productVersions}\nExpectedStatusCode: {expectedStatusCode}\nExpectedErrorMessage:{expectedErrorMessage}");

            await submitPostRequestAndCheckResponse(_requestId, productVersions, _endpoint, expectedStatusCode, expectedErrorMessage);
        }


        [RetryTheory(maxRetries: 1, delayBetweenRetriesMs: 5000)]
        [DisableParallelization] // This test runs in parallel with other tests. However, its test cases are run sequentially.
        [InlineData("[ { \"editionNumber\": 7, \"updateNumber\": 10 }, { \"editionNumber\": 36, \"updateNumber\": 0 } ]", "https://valid.com/callback", HttpStatusCode.BadRequest, "ProductName cannot be null or empty")] // Test Case 244569 - Missing ProductName
        [InlineData("[ { \"productName\": \"\", \"editionNumber\": 7, \"updateNumber\": 10 } ]", "https://valid.com/callback", HttpStatusCode.BadRequest, "ProductName cannot be null or empty")] // Test Case 244571 - Empty ProductName
        [InlineData("[ { \"productName\": \"null\", \"editionNumber\": 36, \"updateNumber\": 0 } ]", "https://valid.com/callback", HttpStatusCode.BadRequest, "'null' is not valid: it neither starts with a 3-digit S-100 code nor has length 8 for S-57")] // Test Case 244580 - Null value of ProductName
        [InlineData("[ { \"productName\": 1234567890, \"editionNumber\": 7, \"updateNumber\": 10 } ]", "https://valid.com/callback", HttpStatusCode.BadRequest, "Either body is null or malformed")] // Test Case 247161 - Non-string ProductName
        public async Task ValidatePVPayloadWithInvalidInputsProductName(string productVersions, string? callbackUri, HttpStatusCode expectedStatusCode, string expectedErrorMessage)
        {

            setEndpoint(callbackUri);

            _output.WriteLine($"RequestId: {_requestId}\nRequest EndPoint: {_endpoint}\nRequest Payload: {productVersions}\nExpectedStatusCode: {expectedStatusCode}\nExpectedErrorMessage:{expectedErrorMessage}");

            await submitPostRequestAndCheckResponse(_requestId, productVersions, _endpoint, expectedStatusCode, expectedErrorMessage);
        }


        [RetryTheory(maxRetries: 1, delayBetweenRetriesMs: 5000)]
        [DisableParallelization] // This test runs in parallel with other tests. However, its test cases are run sequentially.
        [InlineData("[ { \"productName\": \"101GB40079ABCDEFG\", \"updateNumber\": 10 } ]", "https://valid.com/callback", HttpStatusCode.BadRequest, "EditionNumber must be a positive integer")] // Test Case 245738 - Missing EditionNumber
        [InlineData("[ { \"productName\": \"101GB40079ABCDEFG\", \"editionNumber\": 0, \"updateNumber\": 10 } ]", "https://valid.com/callback", HttpStatusCode.BadRequest, "EditionNumber must be a positive integer")] // Test Case 245073 - Invalid EditionNumber
        [InlineData("[ { \"productName\": \"102NO32904820801012\", \"editionNumber\": -1, \"updateNumber\": 0 } ]", "https://valid.com/callback", HttpStatusCode.BadRequest, "EditionNumber must be a positive integer")] // Test Case 245073 - Invalid EditionNumber
        [InlineData("[ { \"productName\": \"101GB40079ABCDEFG\", \"editionNumber\": \"\", \"updateNumber\": 10 } ]", "https://valid.com/callback", HttpStatusCode.BadRequest, "Either body is null or malformed")] // Test Case 245029 - Empty EditionNumber
        [InlineData("[ { \"productName\": \"102NO32904820801012\", \"editionNumber\": \"abcd\", \"updateNumber\": 0 } ]", "https://valid.com/callback", HttpStatusCode.BadRequest, "Either body is null or malformed")] // Test Case 245029 - Non-integer EditionNumber
        public async Task ValidatePVPayloadWithInvalidInputsEditionNumber(string productVersions, string? callbackUri, HttpStatusCode expectedStatusCode, string expectedErrorMessage)
        {

            setEndpoint(callbackUri);

            _output.WriteLine($"RequestId: {_requestId}\nRequest EndPoint: {_endpoint}\nRequest Payload: {productVersions}\nExpectedStatusCode: {expectedStatusCode}\nExpectedErrorMessage:{expectedErrorMessage}");

            await submitPostRequestAndCheckResponse(_requestId, productVersions, _endpoint, expectedStatusCode, expectedErrorMessage);
        }


        [RetryTheory(maxRetries: 1, delayBetweenRetriesMs: 5000)]
        [DisableParallelization] // This test runs in parallel with other tests. However, its test cases are run sequentially.
        [InlineData("[ { \"productName\": \"101GB40079ABCDEFG\", \"editionNumber\": 7 } ]", "https://valid.com/callback", HttpStatusCode.BadRequest, "UpdateNumber must be zero or a positive integer")] // Test Case 245040 - Missing UpdateNumber
        [InlineData("[ { \"productName\": \"101GB40079ABCDEFG\", \"editionNumber\": 7, \"updateNumber\": -1 } ]", "https://valid.com/callback", HttpStatusCode.BadRequest, "UpdateNumber must be zero or a positive integer")] // Test Case 245038 - Invalid UpdateNumber
        [InlineData("[ { \"productName\": \"101GB40079ABCDEFG\", \"editionNumber\": 7, \"updateNumber\": \"\" } ]", "https://valid.com/callback", HttpStatusCode.BadRequest, "Either body is null or malformed")] // Test Case 245715 - Empty UpdateNumber
        [InlineData("[ { \"productName\": \"102NO32904820801012\", \"editionNumber\": 36, \"updateNumber\": \"abcd\" } ]", "https://valid.com/callback", HttpStatusCode.BadRequest, "Either body is null or malformed")] // Test Case 245715 - Non-integer UpdateNumber
        public async Task ValidatePVPayloadWithInvalidInputsUpdateNumber(string productVersions, string? callbackUri, HttpStatusCode expectedStatusCode, string expectedErrorMessage)
        {

            setEndpoint(callbackUri);

            _output.WriteLine($"RequestId: {_requestId}\nRequest EndPoint: {_endpoint}\nRequest Payload: {productVersions}\nExpectedStatusCode: {expectedStatusCode}\nExpectedErrorMessage:{expectedErrorMessage}");

            await submitPostRequestAndCheckResponse(_requestId, productVersions, _endpoint, expectedStatusCode, expectedErrorMessage);
        }


        [RetryTheory(maxRetries: 1, delayBetweenRetriesMs: 5000)]
        [DisableParallelization] // This test runs in parallel with other tests. However, its test cases are run sequentially.
        [InlineData("[ { \"productName\": \"112GB40079ABCDEFG\", \"editionNumber\": 36, \"updateNumber\": 0 } ]", "https://valid.com/callback", HttpStatusCode.BadRequest, "'112GB40079ABCDEFG' starts with digits '112' but that is not a valid S-100 product")] // Test Case 246904 - Invalid first three characters of S-100 product code in productName
        [InlineData("[ { \"productName\": \"101GB40079ABCDEFG\", \"editionNumber\": 7, \"updateNumber\": 10 } ]", "http://invalid.com/callback", HttpStatusCode.BadRequest, "URI is malformed or does not use HTTPS")] // Test Case 244581 - Invalid CallBackUri
        [InlineData("[ { \"productName\": \"101GB40079ABCDEFG\", \"editionNumber\": 7, \"updateNumber\": 10 }, { \"productName\": \"102NO32904820801012\", \"editionNumber\": 0, \"updateNumber\": 0 }, { \"productName\": \"\", \"editionNumber\": 7, \"updateNumber\": -1 }, { \"productName\": \"111US00_ches_dcf8_20190703T00Z\", \"editionNumber\": -1, \"updateNumber\": 0 } ]", "https://valid.com/callback", HttpStatusCode.BadRequest, "ProductName cannot be null or empty")] // Test Case 245047 - Combination of valid and invalid inputs
        public async Task ValidatePVPayloadWithValidAndInvalidInputs(string productVersions, string? callbackUri, HttpStatusCode expectedStatusCode, string expectedErrorMessage)
        {

            setEndpoint(callbackUri);

            _output.WriteLine($"RequestId: {_requestId}\nRequest EndPoint: {_endpoint}\nRequest Payload: {productVersions}\nExpectedStatusCode: {expectedStatusCode}\nExpectedErrorMessage:{expectedErrorMessage}");

            await submitPostRequestAndCheckResponse(_requestId, productVersions, _endpoint, expectedStatusCode, expectedErrorMessage);
        }


        [RetryTheory(maxRetries: 1, delayBetweenRetriesMs: 5000)]
        [DisableParallelization] // This test runs in parallel with other tests. However, its test cases are run sequentially.
        [InlineData("[  ] ", "https://valid.com/callback", HttpStatusCode.BadRequest, "Either body is null or malformed")] // Test Case 244570 - Empty array
        [InlineData("[  \"\" ] ", "https://valid.com/callback", HttpStatusCode.BadRequest, "Either body is null or malformed")] // Test Case 245718 - Array with Empty string
        [InlineData("[ { } ] ", "https://valid.com/callback", HttpStatusCode.BadRequest, "ProductName cannot be null or empty")] // Test Case 247164 - Array with empty object
        [InlineData("", "https://valid.com/callback", HttpStatusCode.BadRequest, "Either body is null or malformed")] // Test Case 247166 - Blank request body
        [InlineData("{ \"productName\": \"101GB40079ABCDEFG\", \"editionNumber\": 7, \"updateNumber\": 10 }, { \"productName\": \"102NO32904820801012\", \"editionNumber\": 36, \"updateNumber\": 0 } ", "https://valid.com/callback", HttpStatusCode.BadRequest, "Either body is null or malformed")] // Test Case 247169 - Invalid json body
        public async Task ValidatePVPayloadWithInvalidInputs(string productVersions, string? callbackUri, HttpStatusCode expectedStatusCode, string expectedErrorMessage)
        {

            setEndpoint(callbackUri);

            _output.WriteLine($"RequestId: {_requestId}\nRequest EndPoint: {_endpoint}\nRequest Payload: {productVersions}\nExpectedStatusCode: {expectedStatusCode}\nExpectedErrorMessage:{expectedErrorMessage}");

            await submitPostRequestAndCheckResponse(_requestId, productVersions, _endpoint, expectedStatusCode, expectedErrorMessage);
        }
    }
}
