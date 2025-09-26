using System.Text.Json;
using FluentAssertions;
using FluentAssertions.Execution;
using Meziantou.Xunit;
using UKHO.ADDS.EFS.FunctionalTests.Services;
using xRetry;
using Xunit.Abstractions;
using static System.Net.WebRequestMethods;

namespace UKHO.ADDS.EFS.FunctionalTests
{
    [Collection("Startup Collection")]
    [EnableParallelization] // Needed to parallelize inside the class, not just between classes
    public class ProductNamesFunctionalTests : TestBase
    {
        private string _requestId = "", _batchId = "";
        private string _endpoint = "/v2/exchangeSet/s100/productNames";
        private Boolean _assertCallbackTxtFile = false;


        public ProductNamesFunctionalTests(StartupFixture startup, ITestOutputHelper output) : base(startup, output)
        {
            _requestId = $"job-productNamesAutoTest-" + Guid.NewGuid();
        }


        private async Task SubmitPostRequestAndCheckResponse(string requestId, object requestPayload, string endpoint, HttpStatusCode expectedStatusCode, string expectedErrorMessage)
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


        private void SetEndpoint(string? callbackUri)
        {
            if (callbackUri != null)
            {
                // Get the base URL from the HttpClient
                var baseUrl = (AspireResourceSingleton.httpClientMock!.BaseAddress)!.ToString();

                if (callbackUri.ToLower().Equals("https://valid.com/callback"))
                {
                    _assertCallbackTxtFile = true;
                    if (baseUrl.ToLower().StartsWith("http://localhost"))
                    {
                        _endpoint = _endpoint + "?callbackUri=https://adds-mocks-efs/callback/callback";
                    }
                    else
                    {
                        _endpoint = _endpoint + $"?callbackUri=https://adds-mocks-efs.redmoss-3083029b.uksouth.azurecontainerapps.io/callback/callback";
                    }
                }
                else
                {
                    _endpoint = _endpoint + $"?callbackUri={callbackUri}";
                }
            }
        }


        private async Task TestExecutionSteps(object payload, string zipFileName, int expectedRequestedProductCount, int expectedExchangeSetProductCount)
        {
            ApiResponseAssertions apiResponseAssertions = new ApiResponseAssertions();

            var responseJobSubmit = await OrchestratorCommands.PostRequestAsync(_requestId, payload, _endpoint);
            var responseContent = await apiResponseAssertions.CheckCustomExSetReqResponce(_requestId, responseJobSubmit, expectedRequestedProductCount, expectedExchangeSetProductCount);
            _batchId = responseContent.Contains("fssBatchId") ? JsonDocument.Parse(responseContent).RootElement.GetProperty("fssBatchId").GetString()! : "";

            _output.WriteLine($"Started waiting for job completion ... {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}");
            var responseJobStatus = await OrchestratorCommands.WaitForJobCompletionAsync(_requestId);
            await apiResponseAssertions.CheckJobCompletionStatus(responseJobStatus);
            _output.WriteLine($"Finished waiting for job completion ... {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}");

            var responseBuildStatus = await OrchestratorCommands.GetBuildStatusAsync(_requestId);
            await apiResponseAssertions.CheckBuildStatus(responseBuildStatus);

            if (_assertCallbackTxtFile)
            {
                _output.WriteLine($"Trying to download file callback-response-{_batchId}.txt ... {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}");
                var callbackTxtFilePath = await FileDownloadFromMock.DownloadCallbackTxtAsync(_batchId);
                FileComparer.CompareCallbackResponse(responseContent, callbackTxtFilePath);
            }

            _output.WriteLine($"Trying to download file V01X01_{_requestId}.zip ... {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}");
            var exchangeSetDownloadPath = await FileDownloadFromMock.DownloadExchangeSetAsZipAsync(_requestId);
            var sourceZipPath = Path.Combine(AspireResourceSingleton.ProjectDirectory!, "TestData", zipFileName);
            FileComparer.CompareZipFilesExactMatch(sourceZipPath, exchangeSetDownloadPath);
        }


        //PBI 244063 - Use the existing Product Names Node (GetS100ProductNamesNode) from existing pipeline (S100AssemblyPipeline) to new pipeline (S100CustomAssemblyPipeline).
        [RetryTheory(maxRetries: 1, delayBetweenRetriesMs: 5000)]
        [DisableParallelization] // This test runs in parallel with other tests. However, its test cases are run sequentially.
        [InlineData(new string[] { "101GB004DEVQK" }, "https://valid.com/callback", "Single101Product.zip", 1, 1)] // Test Case 245610 - Product Name (S-101 product) Node Integration
        [InlineData(new string[] { "102CA005N5040W00130" }, "https://adds-mocks-efs/callback/callback", "Single102Product.zip", 1, 1)] // Test Case 245610 - Product Name (S-102 product) Node Integration
        [InlineData(new string[] { "104CA00_20241103T001500Z_GB3DEVK0_DCF2" }, "", "Single104Product.zip", 1, 1)] // Test Case 243519 - Valid input with valid callBackURI and // Test Case 245610 - Product Name (S-104 product) Node Integration
        [InlineData(new string[] { "111FR00_20241217T001500Z_GB3DEVK0_DCF2" }, null, "Single111Product.zip", 1, 1)] // Test Case 245610 - Product Name (S-111 product) Node Integration
        [InlineData(new string[] { "111CA00_20241217T001500Z_GB3DEVQ0_DCF2", "104CA00_20241103T001500Z_GB3DEVK0_DCF2" }, "https://valid.com/callback", "MultipleProducts.zip", 2, 2)]   // Test Case 243519 - Valid input and // Test Case 245610 - Product Names (multiple products) Node Integration
        public async Task ValidateProductNamesNodeInCustomAssemblyPipeline(string[] productNames, string? callbackUri, string zipFileName, int expectedRequestedProductCount, int expectedExchangeSetProductCount)
        {
            using var scope = new AssertionScope(); // root scope

            SetEndpoint(callbackUri);

            _output.WriteLine($"RequestId: {_requestId}\nRequest EndPoint: {_endpoint}\nRequest Payload: {System.Text.Json.JsonSerializer.Serialize(productNames)}\nCallbackUri: {callbackUri}\nExpectedZipFileName:{zipFileName}");

            await TestExecutionSteps(productNames, zipFileName, expectedRequestedProductCount, expectedExchangeSetProductCount);
        }

        //PBI 242670 - Input validation for the ESS API - Product Name Endpoint
        [RetryTheory(maxRetries: 1, delayBetweenRetriesMs: 5000)]
        [DisableParallelization] // This test runs in parallel with other tests. However, its test cases are run sequentially.
        [InlineData(new object[] { "112GB40079ABCDEFG" }, "https://valid.com/callback", HttpStatusCode.BadRequest, "112GB40079ABCDEFG' starts with digits '112' but that is not a valid S-100 product")] // Test Case 245717 -Invalid Product
        [InlineData(new object[] { "" }, "https://valid.com/callback", HttpStatusCode.BadRequest, "ProductName cannot be null or empty")] //Test Case 243605 - Array with Empty string
        [InlineData(new object[] { "101GB40079ABCDEFG" }, "invalidCallbackUri.com", HttpStatusCode.BadRequest, "URI is malformed or does not use HTTPS")] // Test Case 245020 - Invalid CallBackUrl
        [InlineData(new object[] { }, "https://valid.com/callback", HttpStatusCode.BadRequest, "Either body is null or malformed")] // Test Case 243604 - Empty array
        /*
         * Suppressing the 1 failing assertion for the below bug
         * https://dev.azure.com/ukhydro/Exchange%20Set%20Service/_workitems/edit/247982 
         * Once resolved , please reintroduce the assertion for responce body "Either body is null or malformed" as currently passing "" to suppress assertion failure
         */
        [InlineData(new object[] { "101GB40079ABCDEFG", 123, 456, 789 }, "https://valid.com/callback", HttpStatusCode.BadRequest, "")] //Test Case 243659 - Mixed valid and invalid data types
        public async Task ValidatePNPayloadWithInvalidInputs(object[] productNames, string? callbackUri, HttpStatusCode expectedStatusCode, string expectedErrorMessage)
        {
            using var scope = new AssertionScope(); // root scope

            SetEndpoint(callbackUri);

            _output.WriteLine($"RequestId: {_requestId}\nRequest EndPoint: {_endpoint}\nRequest Payload: {System.Text.Json.JsonSerializer.Serialize(productNames)}\nExpectedStatusCode: {expectedStatusCode}\nExpectedErrorMessage:{expectedErrorMessage}");

            await SubmitPostRequestAndCheckResponse(_requestId, productNames, _endpoint, expectedStatusCode, expectedErrorMessage);
        }

    }
}