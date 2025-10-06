using FluentAssertions.Execution;
using Meziantou.Xunit;
using UKHO.ADDS.EFS.FunctionalTests.Assertions;
using UKHO.ADDS.EFS.FunctionalTests.Diagnostics;
using UKHO.ADDS.EFS.FunctionalTests.Framework;
using UKHO.ADDS.EFS.FunctionalTests.Infrastructure;
using UKHO.ADDS.EFS.FunctionalTests.Utilities;
using Xunit.Abstractions;

namespace UKHO.ADDS.EFS.FunctionalTests.Scenarios
{
    [Collection("Startup Collection")]
    [EnableParallelization] // Needed to parallelize inside the class, not just between classes
    public class ProductVersionsFunctionalTests(StartupFixture startup, ITestOutputHelper output) : FunctionalTestBase(startup, output)
    {
        private readonly string _requestId = $"job-productVersionsAutoTest-" + Guid.NewGuid();
        private string _endpoint = "/v2/exchangeSet/s100/productVersions";
        private bool _assertCallbackTxtFile = false;


        private void SetEndpoint(string? callbackUri)
        {
            if (callbackUri != null)
            {
                // Get the base URL from the HttpClient
                var baseUrl = (AspireTestHost.httpClientMock!.BaseAddress)!.ToString();

                if (string.Equals(callbackUri, "https://valid.com/callback", StringComparison.OrdinalIgnoreCase))
                {
                    _assertCallbackTxtFile = true;
                    if (baseUrl.StartsWith("http://localhost", StringComparison.OrdinalIgnoreCase))
                    {
                        _endpoint += "?callbackUri=https://adds-mocks-efs/callback/callback";
                    }
                    else
                    {
                        _endpoint += $"?callbackUri=https://adds-mocks-efs.redmoss-3083029b.uksouth.azurecontainerapps.io/callback/callback";
                    }
                }
                else
                {
                    _endpoint += $"?callbackUri={callbackUri}";
                }
            }
        }


        //PBI 242767 - Input validation for the ESS API - Product Versions Endpoint
        //PBI 244060 - Input validation for the consume mock - Product Versions Endpoint
        [Theory]
        [DisableParallelization] // This test runs in parallel with other tests. However, its test cases are run sequentially.
        [InlineData(" [ { \"productName\": \"101GB40079ABCDEFG\", \"editionNumber\": 5, \"updateNumber\": 10 }, { \"productName\": \"101DE00904820801012\", \"editionNumber\": 36, \"updateNumber\": 5 }, { \"productName\": \"102CA32904820801013\", \"editionNumber\": 13, \"updateNumber\": 0 }, { \"productName\": \"104US00_CHES_TYPE1_20210630_0600\", \"editionNumber\": 9, \"updateNumber\": 0 }, { \"productName\": \"101FR40079QWERTY\", \"editionNumber\": 2, \"updateNumber\": 2 }, { \"productName\": \"111US00_CHES_DCF8_20190703T00Z\", \"editionNumber\": 11, \"updateNumber\": 0 }, { \"productName\": \"102INVA904820801012\", \"editionNumber\": 11, \"updateNumber\": 0 }, { \"productName\": \"102AR00904820801012\", \"editionNumber\": 11, \"updateNumber\": 0 } ] ", "https://valid.com/callback", HttpStatusCode.Accepted, "", "ProductVersionsProducts.zip", 8, 8)] // Test Case 247843 - Valid input
        [InlineData(" [ { \"productName\": \"101GB40079ABCDEFG\", \"editionNumber\": 5, \"updateNumber\": 10 }, { \"productName\": \"101DE00904820801012\", \"editionNumber\": 36, \"updateNumber\": 5 }, { \"productName\": \"102CA32904820801013\", \"editionNumber\": 13, \"updateNumber\": 0 }, { \"productName\": \"104US00_CHES_TYPE1_20210630_0600\", \"editionNumber\": 9, \"updateNumber\": 0 }, { \"productName\": \"101FR40079QWERTY\", \"editionNumber\": 2, \"updateNumber\": 2 }, { \"productName\": \"111US00_CHES_DCF8_20190703T00Z\", \"editionNumber\": 11, \"updateNumber\": 0 }, { \"productName\": \"102INVA904820801012\", \"editionNumber\": 11, \"updateNumber\": 0 }, { \"productName\": \"102AR00904820801012\", \"editionNumber\": 11, \"updateNumber\": 0 } ] ", "", HttpStatusCode.Accepted, "", "ProductVersionsProducts.zip", 8, 8)] // Test Case 247843 - Valid input with only CallBackUri key and value as empty
        [InlineData(" [ { \"productName\": \"101GB40079ABCDEFG\", \"editionNumber\": 5, \"updateNumber\": 10 }, { \"productName\": \"101DE00904820801012\", \"editionNumber\": 36, \"updateNumber\": 5 }, { \"productName\": \"102CA32904820801013\", \"editionNumber\": 13, \"updateNumber\": 0 }, { \"productName\": \"104US00_CHES_TYPE1_20210630_0600\", \"editionNumber\": 9, \"updateNumber\": 0 }, { \"productName\": \"101FR40079QWERTY\", \"editionNumber\": 2, \"updateNumber\": 2 }, { \"productName\": \"111US00_CHES_DCF8_20190703T00Z\", \"editionNumber\": 11, \"updateNumber\": 0 }, { \"productName\": \"102INVA904820801012\", \"editionNumber\": 11, \"updateNumber\": 0 }, { \"productName\": \"102AR00904820801012\", \"editionNumber\": 11, \"updateNumber\": 0 } ] ", null, HttpStatusCode.Accepted, "", "ProductVersionsProducts.zip", 8, 8)] // Test Case 247843 - No CallBackUri parameter in the URL as it is optional parameter
        public async Task ValidateProductVersionsPayloadWithValidInputs(string requestPayload, string? callbackUri, HttpStatusCode expectedStatusCode, string expectedErrorMessage, string zipFileName, int expectedRequestedProductCount, int expectedExchangeSetProductCount)
        {
            using var scope = new AssertionScope(); // root scope

            SetEndpoint(callbackUri);

            TestOutput.WriteLine($"RequestId: {_requestId}\nRequest EndPoint: {_endpoint}\nRequest Payload: {requestPayload}\nExpectedStatusCode: {expectedStatusCode}\nExpectedErrorMessage:{expectedErrorMessage}");

            var productNames = new[] { "101GB40079ABCDEFG", "101DE00904820801012", "102CA32904820801013", "104US00_CHES_TYPE1_20210630_0600", "101FR40079QWERTY", "111US00_CHES_DCF8_20190703T00Z", "102INVA904820801012", "102AR00904820801012" };
            await TestExecutionHelper.ExecuteCustomExchangeSetTestSteps(_requestId, requestPayload, _endpoint, zipFileName, expectedRequestedProductCount, expectedExchangeSetProductCount, _assertCallbackTxtFile, productNames);
        }


        [Theory]
        [DisableParallelization] // This test runs in parallel with other tests. However, its test cases are run sequentially.
        [InlineData("[ { \"editionNumber\": 7, \"updateNumber\": 10 }, { \"editionNumber\": 36, \"updateNumber\": 0 } ]", "https://valid.com/callback", HttpStatusCode.BadRequest, "ProductName cannot be null or empty")] // Test Case 244569 - Missing ProductName
        [InlineData("[ { \"productName\": \"\", \"editionNumber\": 7, \"updateNumber\": 10 } ]", "https://valid.com/callback", HttpStatusCode.BadRequest, "ProductName cannot be null or empty")] // Test Case 244571 - Empty ProductName
        [InlineData("[ { \"productName\": \"null\", \"editionNumber\": 36, \"updateNumber\": 0 } ]", "https://valid.com/callback", HttpStatusCode.BadRequest, "'null' is not valid: it neither starts with a 3-digit S-100 code nor has length 8 for S-57")] // Test Case 244580 - Null value of ProductName
        public async Task ValidatePVPayloadWithInvalidInputsProductName(string productVersions, string? callbackUri, HttpStatusCode expectedStatusCode, string expectedErrorMessage)
        {
            using var scope = new AssertionScope(); // root scope

            SetEndpoint(callbackUri);

            TestOutput.WriteLine($"RequestId: {_requestId}\nRequest EndPoint: {_endpoint}\nRequest Payload: {productVersions}\nExpectedStatusCode: {expectedStatusCode}\nExpectedErrorMessage:{expectedErrorMessage}");

            await ExchangeSetApiAssertions.CustomExSetSubmitPostRequestAndCheckResponse(_requestId, productVersions, _endpoint, expectedStatusCode, expectedErrorMessage);
        }


        [Theory]
        [DisableParallelization] // This test runs in parallel with other tests. However, its test cases are run sequentially.
        [InlineData("[ { \"productName\": \"101GB40079ABCDEFG\", \"updateNumber\": 10 } ]", "https://valid.com/callback", HttpStatusCode.BadRequest, "EditionNumber must be a positive integer")] // Test Case 245738 - Missing EditionNumber
        [InlineData("[ { \"productName\": \"101GB40079ABCDEFG\", \"editionNumber\": 0, \"updateNumber\": 10 } ]", "https://valid.com/callback", HttpStatusCode.BadRequest, "EditionNumber must be a positive integer")] // Test Case 245073 - Invalid EditionNumber
        [InlineData("[ { \"productName\": \"102NO32904820801012\", \"editionNumber\": -1, \"updateNumber\": 0 } ]", "https://valid.com/callback", HttpStatusCode.BadRequest, "EditionNumber must be a positive integer")] // Test Case 245073 - Invalid EditionNumber
        public async Task ValidatePVPayloadWithInvalidInputsEditionNumber(string productVersions, string? callbackUri, HttpStatusCode expectedStatusCode, string expectedErrorMessage)
        {
            using var scope = new AssertionScope(); // root scope

            SetEndpoint(callbackUri);

            TestOutput.WriteLine($"RequestId: {_requestId}\nRequest EndPoint: {_endpoint}\nRequest Payload: {productVersions}\nExpectedStatusCode: {expectedStatusCode}\nExpectedErrorMessage:{expectedErrorMessage}");

            await ExchangeSetApiAssertions.CustomExSetSubmitPostRequestAndCheckResponse(_requestId, productVersions, _endpoint, expectedStatusCode, expectedErrorMessage);
        }


        [Theory]
        [DisableParallelization] // This test runs in parallel with other tests. However, its test cases are run sequentially.
        [InlineData("[ { \"productName\": \"101GB40079ABCDEFG\", \"editionNumber\": 7 } ]", "https://valid.com/callback", HttpStatusCode.BadRequest, "UpdateNumber must be zero or a positive integer")] // Test Case 245040 - Missing UpdateNumber
        [InlineData("[ { \"productName\": \"101GB40079ABCDEFG\", \"editionNumber\": 7, \"updateNumber\": -1 } ]", "https://valid.com/callback", HttpStatusCode.BadRequest, "UpdateNumber must be zero or a positive integer")] // Test Case 245038 - Invalid UpdateNumber
        public async Task ValidatePVPayloadWithInvalidInputsUpdateNumber(string productVersions, string? callbackUri, HttpStatusCode expectedStatusCode, string expectedErrorMessage)
        {
            using var scope = new AssertionScope(); // root scope

            SetEndpoint(callbackUri);

            TestOutput.WriteLine($"RequestId: {_requestId}\nRequest EndPoint: {_endpoint}\nRequest Payload: {productVersions}\nExpectedStatusCode: {expectedStatusCode}\nExpectedErrorMessage:{expectedErrorMessage}");

            await ExchangeSetApiAssertions.CustomExSetSubmitPostRequestAndCheckResponse(_requestId, productVersions, _endpoint, expectedStatusCode, expectedErrorMessage);
        }


        [Theory]
        [DisableParallelization] // This test runs in parallel with other tests. However, its test cases are run sequentially.
        [InlineData("[ { \"productName\": \"112GB40079ABCDEFG\", \"editionNumber\": 36, \"updateNumber\": 0 } ]", "https://valid.com/callback", HttpStatusCode.BadRequest, "'112GB40079ABCDEFG' starts with digits '112' which is not a valid S-100 product identifier")] // Test Case 246904 - Invalid first three characters of S-100 product code in productName
        [InlineData("[ { \"productName\": \"101GB40079ABCDEFG\", \"editionNumber\": 7, \"updateNumber\": 10 } ]", "http://invalid.com/callback", HttpStatusCode.BadRequest, "URI is malformed or does not use HTTPS")] // Test Case 244581 - Invalid CallBackUri
		[InlineData("[ { } ] ", "https://valid.com/callback", HttpStatusCode.BadRequest, "ProductName cannot be null or empty")] // Test Case 247164 - Array with empty object
        [InlineData("[ { \"productName\": \"101GB40079ABCDEFG\", \"editionNumber\": 7, \"updateNumber\": 10 }, { \"productName\": \"102NO32904820801012\", \"editionNumber\": 0, \"updateNumber\": 0 }, { \"productName\": \"\", \"editionNumber\": 7, \"updateNumber\": -1 }, { \"productName\": \"111US00_ches_dcf8_20190703T00Z\", \"editionNumber\": -1, \"updateNumber\": 0 } ]", "https://valid.com/callback", HttpStatusCode.BadRequest, "ProductName cannot be null or empty")] // Test Case 245047 - Combination of valid and invalid inputs
        public async Task ValidateProductVersionsPayloadWithValidAndInvalidInputs(string productVersions, string? callbackUri, HttpStatusCode expectedStatusCode, string expectedErrorMessage)
        {
            using var scope = new AssertionScope(); // root scope

            SetEndpoint(callbackUri);

            TestOutput.WriteLine($"RequestId: {_requestId}\nRequest EndPoint: {_endpoint}\nRequest Payload: {productVersions}\nExpectedStatusCode: {expectedStatusCode}\nExpectedErrorMessage:{expectedErrorMessage}");

            await ExchangeSetApiAssertions.CustomExSetSubmitPostRequestAndCheckResponse(_requestId, productVersions, _endpoint, expectedStatusCode, expectedErrorMessage);
        }


        [Theory]
        [DisableParallelization] // This test runs in parallel with other tests. However, its test cases are run sequentially.
        [InlineData("[  ] ", "https://valid.com/callback", HttpStatusCode.BadRequest, "Either body is null or malformed")] // Test Case 244570 - Empty array
        /*
         * Suppressing the 8 failing assertion for the below bug
         * BUG-247982
         * Once resolved , please reintroduce the assertion for response body "Either body is null or malformed" as currently passing "" to suppress assertion failure
         * Updates on 01 Oct 2025, this bug will not be fixed, and the specifications will be update as per current behavior that for current scenarios under automation where the api responds back with 400 but the response body will be blank
         */
        [InlineData("[  \"\" ] ", "https://valid.com/callback", HttpStatusCode.BadRequest, "")] // Test Case 245718 - Array with Empty string
        [InlineData("", "https://valid.com/callback", HttpStatusCode.BadRequest, "")] // Test Case 247166 - Blank request body
        [InlineData("{ \"productName\": \"101GB40079ABCDEFG\", \"editionNumber\": 7, \"updateNumber\": 10 }, { \"productName\": \"102NO32904820801012\", \"editionNumber\": 36, \"updateNumber\": 0 } ", "https://valid.com/callback", HttpStatusCode.BadRequest, "")] // Test Case 247169 - Invalid json body
        [InlineData("[ { \"productName\": 1234567890, \"editionNumber\": 7, \"updateNumber\": 10 } ]", "https://valid.com/callback", HttpStatusCode.BadRequest, "")] // Test Case 247161 - Non-string ProductName
        [InlineData("[ { \"productName\": \"101GB40079ABCDEFG\", \"editionNumber\": \"\", \"updateNumber\": 10 } ]", "https://valid.com/callback", HttpStatusCode.BadRequest, "")] // Test Case 245029 - Empty EditionNumber
        [InlineData("[ { \"productName\": \"102NO32904820801012\", \"editionNumber\": \"abcd\", \"updateNumber\": 0 } ]", "https://valid.com/callback", HttpStatusCode.BadRequest, "")] // Test Case 245029 - Non-integer EditionNumber
        [InlineData("[ { \"productName\": \"101GB40079ABCDEFG\", \"editionNumber\": 7, \"updateNumber\": \"\" } ]", "https://valid.com/callback", HttpStatusCode.BadRequest, "")] // Test Case 245715 - Empty UpdateNumber
        [InlineData("[ { \"productName\": \"102NO32904820801012\", \"editionNumber\": 36, \"updateNumber\": \"abcd\" } ]", "https://valid.com/callback", HttpStatusCode.BadRequest, "")] // Test Case 245715 - Non-integer UpdateNumber
        public async Task ValidatePVPayloadEitherBodyIsNullOrMalformed(string productVersions, string? callbackUri, HttpStatusCode expectedStatusCode, string expectedErrorMessage)
        {
            using var scope = new AssertionScope(); // root scope

            SetEndpoint(callbackUri);

            TestOutput.WriteLine($"RequestId: {_requestId}\nRequest EndPoint: {_endpoint}\nRequest Payload: {productVersions}\nExpectedStatusCode: {expectedStatusCode}\nExpectedErrorMessage:{expectedErrorMessage}");

            await ExchangeSetApiAssertions.CustomExSetSubmitPostRequestAndCheckResponse(_requestId, productVersions, _endpoint, expectedStatusCode, expectedErrorMessage);
        }
    }
}
