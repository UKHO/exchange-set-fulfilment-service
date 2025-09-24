using System.Text.Json;
using FluentAssertions;
using FluentAssertions.Execution;
using Meziantou.Xunit;
using UKHO.ADDS.EFS.Domain.Jobs;
using UKHO.ADDS.EFS.FunctionalTests.Services;
using xRetry;
using Xunit.Abstractions;

namespace UKHO.ADDS.EFS.FunctionalTests
{
    [Collection("Startup Collection")]
    [EnableParallelization] // Needed to parallelize inside the class, not just between classes
    public class ProductNamesFunctionalTests : TestBase
    {
        private string _requestId = "";
        private string _endpoint = "/v2/exchangeSet/s100/productNames";


        public ProductNamesFunctionalTests(StartupFixture startup, ITestOutputHelper output) : base(startup, output)
        {
            _requestId = $"job-productNamesAutoTest-" + Guid.NewGuid();
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


        private async Task checkJobsResponce(HttpResponseMessage responseJobSubmit, int expectedRequestedProductCount, int expectedExchangeSetProductCount)
        {
            responseJobSubmit.IsSuccessStatusCode.Should().BeTrue($"Expected success status code but got: {responseJobSubmit.StatusCode}");

            var responseContent = await responseJobSubmit.Content.ReadAsStringAsync();
            _output.WriteLine($"ResponseContent: {responseContent}");

            var responseJson = JsonDocument.Parse(responseContent);
            var batchId = responseJson.RootElement.GetProperty("fssBatchId").GetString();

            _output.WriteLine($"JobId => {_requestId}\n" +
                $"RequestedProductCount => Expected: {expectedRequestedProductCount} Actual: {responseJson.RootElement.GetProperty("requestedProductCount").GetInt64()}\n" +
                $"ExchangeSetProductCount => Expected: {expectedExchangeSetProductCount} Actual: {responseJson.RootElement.GetProperty("exchangeSetProductCount").GetInt64()}\n" +
                $"BatchId: {batchId}");

            var root = responseJson.RootElement;

            using (new AssertionScope())
            {
                // Check if properties exist and have expected values
                if (root.TryGetProperty("fssBatchId", out var batchIdElement))
                {
                    batchId = batchIdElement.GetString();
                    Guid.TryParse(batchId, out _).Should().BeTrue($"Expected '{batchId}' to be a valid GUID");
                }
                else
                {
                    Execute.Assertion.FailWith("Response is missing fssBatchId property");
                }

                if (root.TryGetProperty("requestedProductCount", out var requestedProductCountElement))
                {
                    requestedProductCountElement.GetInt64().Should().Be(expectedRequestedProductCount, "requestedProductCount should be a valid GUID");
                }
                else
                {
                    Execute.Assertion.FailWith("Response is missing requestedProductCount property");
                }

                if (root.TryGetProperty("exchangeSetProductCount", out var exchangeSetProductCountElement))
                {
                    exchangeSetProductCountElement.GetInt64().Should().Be(expectedExchangeSetProductCount, "exchangeSetProductCount should match expected value");
                }
                else
                {
                    Execute.Assertion.FailWith("Response is missing exchangeSetProductCount property");
                }
            }
        }


        private async Task testExecutionMethod(object payload, string zipFileName, int expectedRequestedProductCount, int expectedExchangeSetProductCount)
        {
            var responseJobSubmit = await OrchestratorCommands.PostRequestAsync(_requestId, payload, _endpoint);
            await checkJobsResponce(responseJobSubmit, expectedRequestedProductCount, expectedExchangeSetProductCount);

            ApiResponseAssertions apiResponseAssertions = new ApiResponseAssertions(_output);

            _output.WriteLine($"Started waiting for job completion ... {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}");
            var responseJobStatus = await OrchestratorCommands.WaitForJobCompletionAsync(_requestId);
            await apiResponseAssertions.checkJobCompletionStatus(responseJobStatus);
            _output.WriteLine($"Finished waiting for job completion ... {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}");

            var responseBuildStatus = await OrchestratorCommands.GetBuildStatusAsync(_requestId);
            await apiResponseAssertions.checkBuildStatus(responseBuildStatus);

            _output.WriteLine($"Trying to download file V01X01_{_requestId}.zip");
            var exchangeSetDownloadPath = await ZipStructureComparer.DownloadExchangeSetAsZipAsync(_requestId);
            var sourceZipPath = Path.Combine(AspireResourceSingleton.ProjectDirectory!, "TestData", zipFileName);

            ZipStructureComparer.CompareZipFilesExactMatch(sourceZipPath, exchangeSetDownloadPath);
        }


        //PBI 242670 - Input validation for the ESS API - Product Name Endpoint
        [RetryTheory(maxRetries: 1, delayBetweenRetriesMs: 5000)]
        [DisableParallelization] // This test runs in parallel with other tests. However, its test cases are run sequentially.
        [InlineData(new object[] { "101GB40079ABCDEFG", "102NO32904820801012", "104US00_CHES_TYPE1_20210630_0600", "111US00_ches_dcf8_20190703T00Z" }, "https://valid.com/callback", HttpStatusCode.Accepted, "")] // Test Case 243519 - Valid input
        [InlineData(new object[] { "101GB40079ABCDEFG", "102NO32904820801012" }, "", HttpStatusCode.Accepted, "")] // Test Case 243519 - Valid input with valid callBackURI
        [InlineData(new object[] { "101GB40079ABCDEFG", "102NO32904820801012" }, null, HttpStatusCode.Accepted, "")]
        public async Task ValidatePNPayloadWithValidInputs(object[] productNames, string? callbackUri, HttpStatusCode expectedStatusCode, string expectedErrorMessage)
        {

            setEndpoint(callbackUri);

            _output.WriteLine($"RequestId: {_requestId}\nRequest EndPoint: {_endpoint}\nRequest Payload: {System.Text.Json.JsonSerializer.Serialize(productNames)}\nExpectedStatusCode: {expectedStatusCode}\nExpectedErrorMessage:{expectedErrorMessage}");

            await submitPostRequestAndCheckResponse(_requestId, productNames, _endpoint, expectedStatusCode, expectedErrorMessage);
        }

        [RetryTheory(maxRetries: 1, delayBetweenRetriesMs: 5000)]
        [DisableParallelization] // This test runs in parallel with other tests. However, its test cases are run sequentially.
        [InlineData(new object[] { "112GB40079ABCDEFG" }, "https://valid.com/callback", HttpStatusCode.BadRequest, "112GB40079ABCDEFG' starts with digits '112' but that is not a valid S-100 product")] // Test Case 245717 -Invalid Product
        [InlineData(new object[] { }, "https://valid.com/callback", HttpStatusCode.BadRequest, "Either body is null or malformed")] // Test Case 243604 - Empty array
        [InlineData(new object[] { "" }, "https://valid.com/callback", HttpStatusCode.BadRequest, "ProductName cannot be null or empty")] //Test Case 243605 - Array with Empty string
        [InlineData(new object[] { "101GB40079ABCDEFG" }, "invalidCallbackUri.com", HttpStatusCode.BadRequest, "URI is malformed or does not use HTTPS")] // Test Case 245020 - Invalid CallBackUrl
        [InlineData(new object[] { "101GB40079ABCDEFG", 123, 456, 789 }, "https://valid.com/callback", HttpStatusCode.BadRequest, "Either body is null or malformed")] //Test Case 243659 - Mixed valid and invalid data types
        public async Task ValidatePNPayloadWithInvalidInputs(object[] productNames, string? callbackUri, HttpStatusCode expectedStatusCode, string expectedErrorMessage)
        {

            setEndpoint(callbackUri);

            _output.WriteLine($"RequestId: {_requestId}\nRequest EndPoint: {_endpoint}\nRequest Payload: {System.Text.Json.JsonSerializer.Serialize(productNames)}\nExpectedStatusCode: {expectedStatusCode}\nExpectedErrorMessage:{expectedErrorMessage}");

            await submitPostRequestAndCheckResponse(_requestId, productNames, _endpoint, expectedStatusCode, expectedErrorMessage);
        }

        //PBI 244063 - Use the existing Product Names Node (GetS100ProductNamesNode) from existing pipeline (S100AssemblyPipeline) to new pipeline (S100CustomAssemblyPipeline).
        [RetryTheory(maxRetries: 1, delayBetweenRetriesMs: 5000)]
        [InlineData(new string[] { "101GB004DEVQK" }, "https://valid.com/callback", "Single101Product.zip", 1, 1)] // Test Case 245610 - Product Name (S-101 product) Node Integration
        [InlineData(new string[] { "102CA005N5040W00130" }, "https://valid.com/callback", "Single102Product.zip", 1, 1)] // Test Case 245610 - Product Name (S-102 product) Node Integration
        [InlineData(new string[] { "104CA00_20241103T001500Z_GB3DEVK0_DCF2" }, "https://valid.com/callback", "Single104Product.zip", 1, 1)] // Test Case 245610 - Product Name (S-104 product) Node Integration
        [InlineData(new string[] { "111FR00_20241217T001500Z_GB3DEVK0_DCF2" }, "https://valid.com/callback", "Single111Product.zip", 1, 1)] // Test Case 245610 - Product Name (S-111 product) Node Integration
        [InlineData(new string[] { "111CA00_20241217T001500Z_GB3DEVQ0_DCF2", "104CA00_20241103T001500Z_GB3DEVK0_DCF2" }, "https://valid.com/callback", "MultipleProducts.zip", 2, 2)]   // Test Case 245610 - Product Names (multiple products) Node Integration
        public async Task ValidateProductNamesNodeInCustomAssemblyPipeline(string[] productNames, string? callbackUri, string zipFileName, int expectedRequestedProductCount, int expectedExchangeSetProductCount)
        {
            setEndpoint(callbackUri);

            _output.WriteLine($"RequestId: {_requestId}\nRequest EndPoint: {_endpoint}\nRequest Payload: {System.Text.Json.JsonSerializer.Serialize(productNames)}\nCallbackUri: {callbackUri}\nExpectedZipFileName:{zipFileName}");

            await testExecutionMethod(productNames, zipFileName, expectedRequestedProductCount, expectedExchangeSetProductCount);
        }
    }
}
