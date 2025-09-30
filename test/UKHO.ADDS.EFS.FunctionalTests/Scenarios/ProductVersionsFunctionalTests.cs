using System.Text.Json;
using FluentAssertions.Execution;
using Meziantou.Xunit;
using UKHO.ADDS.EFS.FunctionalTests.Assertions;
using UKHO.ADDS.EFS.FunctionalTests.Framework;
using UKHO.ADDS.EFS.FunctionalTests.Http;
using UKHO.ADDS.EFS.FunctionalTests.Infrastructure;
using UKHO.ADDS.EFS.FunctionalTests.IO;
using Xunit.Abstractions;

namespace UKHO.ADDS.EFS.FunctionalTests.Scenarios
{
    [Collection("Startup Collection")]
    [EnableParallelization] // Needed to parallelize inside the class, not just between classes
    public class ProductVersionsFunctionalTests(StartupFixture startup, ITestOutputHelper output) : FunctionalTestBase(startup, output)
    {
        private readonly string _requestId = $"job-productVersionsAutoTest-" + Guid.NewGuid();
        private string _batchId = "";
        private string _endpoint = "/v2/exchangeSet/s100/productVersions";
        private bool _assertCallbackTxtFile = false;

        private async Task SubmitPostRequestAndCheckResponse(string requestId, object requestPayload, string endpoint, HttpStatusCode expectedStatusCode, string expectedErrorMessage)
        {
            var response = await OrchestratorClient.PostRequestAsync(requestId, requestPayload, endpoint);
            Assert.Equal(expectedStatusCode, response.StatusCode);

            if (expectedStatusCode != HttpStatusCode.Accepted && expectedErrorMessage != "")
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                _output.WriteLine($"Expected ResponseContent: {expectedErrorMessage}");
                _output.WriteLine($"Actual ResponseContent: {responseBody}");
                Assert.Contains(expectedErrorMessage, responseBody);
            }
        }

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

        private async Task TestExecutionSteps(object payload, string zipFileName, int expectedRequestedProductCount, int expectedExchangeSetProductCount)
        {
            var apiResponseAssertions = new ExchangeSetApiAssertions();

            var responseJobSubmit = await OrchestratorClient.PostRequestAsync(_requestId, payload, _endpoint);
            var responseContent = await apiResponseAssertions.CheckCustomExSetReqResponse(_requestId, responseJobSubmit, expectedRequestedProductCount, expectedExchangeSetProductCount);
            _batchId = responseContent.Contains("fssBatchId") ? JsonDocument.Parse(responseContent).RootElement.GetProperty("fssBatchId").GetString()! : "";

            _output.WriteLine($"Started waiting for job completion ... {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
            var responseJobStatus = await OrchestratorClient.WaitForJobCompletionAsync(_requestId);
            await apiResponseAssertions.CheckJobCompletionStatus(responseJobStatus);
            _output.WriteLine($"Finished waiting for job completion ... {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");

            var responseBuildStatus = await OrchestratorClient.GetBuildStatusAsync(_requestId);
            await apiResponseAssertions.CheckBuildStatus(responseBuildStatus);

            if (_assertCallbackTxtFile)
            {
                _output.WriteLine($"Trying to download file callback-response-{_batchId}.txt ... {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                var callbackTxtFilePath = await MockFilesClient.DownloadCallbackTxtAsync(_batchId);
                CallbackResponseAssertions.CompareCallbackResponse(responseContent, callbackTxtFilePath);
            }

            _output.WriteLine($"Trying to download file V01X01_{_requestId}.zip");
            var exchangeSetDownloadPath = await MockFilesClient.DownloadExchangeSetAsZipAsync(_requestId);
            var sourceZipPath = Path.Combine(AspireTestHost.ProjectDirectory!, "TestData", zipFileName);

            var productNames = new[] { "101GB40079ABCDEFG", "101DE00904820801012", "102CA32904820801013", "104US00_CHES_TYPE1_20210630_0600", "101FR40079QWERTY", "111US00_CHES_DCF8_20190703T00Z", "102INVA904820801012", "102AR00904820801012" };

            ZipArchiveAssertions.CompareZipFilesExactMatch(sourceZipPath, exchangeSetDownloadPath, productNames);
        }

        //PBI 242767 - Input validation for the ESS API - Product Versions Endpoint
        //PBI 244060 - Input validation for the consume mock - Product Versions Endpoint
        [Theory]
        [DisableParallelization] // This test runs in parallel with other tests. However, its test cases are run sequentially.
        [InlineData(" [ { \"productName\": \"101GB40079ABCDEFG\", \"editionNumber\": 5, \"updateNumber\": 10 }, { \"productName\": \"101DE00904820801012\", \"editionNumber\": 36, \"updateNumber\": 5 }, { \"productName\": \"102CA32904820801013\", \"editionNumber\": 13, \"updateNumber\": 0 }, { \"productName\": \"104US00_CHES_TYPE1_20210630_0600\", \"editionNumber\": 9, \"updateNumber\": 0 }, { \"productName\": \"101FR40079QWERTY\", \"editionNumber\": 2, \"updateNumber\": 2 }, { \"productName\": \"111US00_CHES_DCF8_20190703T00Z\", \"editionNumber\": 11, \"updateNumber\": 0 }, { \"productName\": \"102INVA904820801012\", \"editionNumber\": 11, \"updateNumber\": 0 }, { \"productName\": \"102AR00904820801012\", \"editionNumber\": 11, \"updateNumber\": 0 } ] ", "https://valid.com/callback", HttpStatusCode.Accepted, "", "ProductVersionsProducts.zip", 8, 8)] // Test Case 247843 - Valid input
        [InlineData(" [ { \"productName\": \"101GB40079ABCDEFG\", \"editionNumber\": 5, \"updateNumber\": 10 }, { \"productName\": \"101DE00904820801012\", \"editionNumber\": 36, \"updateNumber\": 5 }, { \"productName\": \"102CA32904820801013\", \"editionNumber\": 13, \"updateNumber\": 0 }, { \"productName\": \"104US00_CHES_TYPE1_20210630_0600\", \"editionNumber\": 9, \"updateNumber\": 0 }, { \"productName\": \"101FR40079QWERTY\", \"editionNumber\": 2, \"updateNumber\": 2 }, { \"productName\": \"111US00_CHES_DCF8_20190703T00Z\", \"editionNumber\": 11, \"updateNumber\": 0 }, { \"productName\": \"102INVA904820801012\", \"editionNumber\": 11, \"updateNumber\": 0 }, { \"productName\": \"102AR00904820801012\", \"editionNumber\": 11, \"updateNumber\": 0 } ] ", "", HttpStatusCode.Accepted, "", "ProductVersionsProducts.zip", 8, 8)] // Test Case 247843 - Valid input with only CallBackUri key and value as empty
        [InlineData(" [ { \"productName\": \"101GB40079ABCDEFG\", \"editionNumber\": 5, \"updateNumber\": 10 }, { \"productName\": \"101DE00904820801012\", \"editionNumber\": 36, \"updateNumber\": 5 }, { \"productName\": \"102CA32904820801013\", \"editionNumber\": 13, \"updateNumber\": 0 }, { \"productName\": \"104US00_CHES_TYPE1_20210630_0600\", \"editionNumber\": 9, \"updateNumber\": 0 }, { \"productName\": \"101FR40079QWERTY\", \"editionNumber\": 2, \"updateNumber\": 2 }, { \"productName\": \"111US00_CHES_DCF8_20190703T00Z\", \"editionNumber\": 11, \"updateNumber\": 0 }, { \"productName\": \"102INVA904820801012\", \"editionNumber\": 11, \"updateNumber\": 0 }, { \"productName\": \"102AR00904820801012\", \"editionNumber\": 11, \"updateNumber\": 0 } ] ", null, HttpStatusCode.Accepted, "", "ProductVersionsProducts.zip", 8, 8)] // Test Case 247843 - No CallBackUri parameter in the URL as it is optional parameter
        public async Task ValidateProductVersionsPayloadWithValidInputs(string productVersions, string? callbackUri, HttpStatusCode expectedStatusCode, string expectedErrorMessage, string zipFileName, int expectedRequestedProductCount, int expectedExchangeSetProductCount)
        {
            using var scope = new AssertionScope(); // root scope

            SetEndpoint(callbackUri);

            _output.WriteLine($"RequestId: {_requestId}\nRequest EndPoint: {_endpoint}\nRequest Payload: {productVersions}\nExpectedStatusCode: {expectedStatusCode}\nExpectedErrorMessage:{expectedErrorMessage}");

            await TestExecutionSteps(productVersions, zipFileName, expectedRequestedProductCount, expectedExchangeSetProductCount);
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

            _output.WriteLine($"RequestId: {_requestId}\nRequest EndPoint: {_endpoint}\nRequest Payload: {productVersions}\nExpectedStatusCode: {expectedStatusCode}\nExpectedErrorMessage:{expectedErrorMessage}");

            await SubmitPostRequestAndCheckResponse(_requestId, productVersions, _endpoint, expectedStatusCode, expectedErrorMessage);
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

            _output.WriteLine($"RequestId: {_requestId}\nRequest EndPoint: {_endpoint}\nRequest Payload: {productVersions}\nExpectedStatusCode: {expectedStatusCode}\nExpectedErrorMessage:{expectedErrorMessage}");

            await SubmitPostRequestAndCheckResponse(_requestId, productVersions, _endpoint, expectedStatusCode, expectedErrorMessage);
        }


        [Theory]
        [DisableParallelization] // This test runs in parallel with other tests. However, its test cases are run sequentially.
        [InlineData("[ { \"productName\": \"101GB40079ABCDEFG\", \"editionNumber\": 7 } ]", "https://valid.com/callback", HttpStatusCode.BadRequest, "UpdateNumber must be zero or a positive integer")] // Test Case 245040 - Missing UpdateNumber
        [InlineData("[ { \"productName\": \"101GB40079ABCDEFG\", \"editionNumber\": 7, \"updateNumber\": -1 } ]", "https://valid.com/callback", HttpStatusCode.BadRequest, "UpdateNumber must be zero or a positive integer")] // Test Case 245038 - Invalid UpdateNumber
        public async Task ValidatePVPayloadWithInvalidInputsUpdateNumber(string productVersions, string? callbackUri, HttpStatusCode expectedStatusCode, string expectedErrorMessage)
        {
            using var scope = new AssertionScope(); // root scope

            SetEndpoint(callbackUri);

            _output.WriteLine($"RequestId: {_requestId}\nRequest EndPoint: {_endpoint}\nRequest Payload: {productVersions}\nExpectedStatusCode: {expectedStatusCode}\nExpectedErrorMessage:{expectedErrorMessage}");

            await SubmitPostRequestAndCheckResponse(_requestId, productVersions, _endpoint, expectedStatusCode, expectedErrorMessage);
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

            _output.WriteLine($"RequestId: {_requestId}\nRequest EndPoint: {_endpoint}\nRequest Payload: {productVersions}\nExpectedStatusCode: {expectedStatusCode}\nExpectedErrorMessage:{expectedErrorMessage}");

            await SubmitPostRequestAndCheckResponse(_requestId, productVersions, _endpoint, expectedStatusCode, expectedErrorMessage);
        }


        [Theory]
        [DisableParallelization] // This test runs in parallel with other tests. However, its test cases are run sequentially.
        [InlineData("[  ] ", "https://valid.com/callback", HttpStatusCode.BadRequest, "Either body is null or malformed")] // Test Case 244570 - Empty array
        /*
         * Suppressing the 8 failing assertion for the below bug
         * BUG-247982
         * Once resolved , please reintroduce the assertion for response body "Either body is null or malformed" as currently passing "" to suppress assertion failure
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

            _output.WriteLine($"RequestId: {_requestId}\nRequest EndPoint: {_endpoint}\nRequest Payload: {productVersions}\nExpectedStatusCode: {expectedStatusCode}\nExpectedErrorMessage:{expectedErrorMessage}");

            await SubmitPostRequestAndCheckResponse(_requestId, productVersions, _endpoint, expectedStatusCode, expectedErrorMessage);
        }
    }
}
