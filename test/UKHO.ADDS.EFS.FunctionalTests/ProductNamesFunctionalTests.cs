using Meziantou.Xunit;
using UKHO.ADDS.EFS.FunctionalTests.Services;
using Xunit.Abstractions;

namespace UKHO.ADDS.EFS.FunctionalTests
{
    [Collection("Startup")]
    public class ProductNamesFunctionalTests
    {
        private readonly ITestOutputHelper _output;
        private string _requestId = "";
        private string _endpoint = "/v2/exchangeSet/s100/productNames";


        public ProductNamesFunctionalTests(ITestOutputHelper output)
        {
            _output = output;
            _requestId = $"job-productNamesAutoTest-" + Guid.NewGuid();
        }


        private async Task submitPostRequestAndCheckResponse(string requestId, object requestPayload, string endpoint, HttpStatusCode expectedStatusCode, string expectedErrorMessage)
        {
            var response = await OrchestratorCommands.commonOrchPostCallHelper(requestId, requestPayload, endpoint);
            Assert.Equal(expectedStatusCode, response.StatusCode);

            if (expectedStatusCode != HttpStatusCode.Accepted && expectedErrorMessage != "")
            {
                var responseBody = await response.Content.ReadAsStringAsync();
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


        //PBI 242670 - Input validation for the ESS API - Product Name Endpoint
        [Theory]
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

        [Theory]
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
        [Theory]
        [InlineData(new string[] { "101GB004DEVQK" }, "https://valid.com/callback", "Single101Product.zip")] // Test Case 245610 - Product Name (S-101 product) Node Integration
        [InlineData(new string[] { "102CA005N5040W00130" }, "https://valid.com/callback", "Single102Product.zip")] // Test Case 245610 - Product Name (S-102 product) Node Integration
        [InlineData(new string[] { "104CA00_20241103T001500Z_GB3DEVK0_DCF2" }, "https://valid.com/callback", "Single104Product.zip")] // Test Case 245610 - Product Name (S-104 product) Node Integration
        [InlineData(new string[] { "111FR00_20241217T001500Z_GB3DEVK0_DCF2" }, "https://valid.com/callback", "Single111Product.zip")] // Test Case 245610 - Product Name (S-111 product) Node Integration
        [InlineData(new string[] { "111CA00_20241217T001500Z_GB3DEVQ0_DCF2", "104CA00_20241103T001500Z_GB3DEVK0_DCF2" }, "https://valid.com/callback", "MultipleProducts.zip")]   // Test Case 245610 - Product Names (multiple products) Node Integration
        public async Task ValidateProductNamesNodeInCustomAssemblyPipeline(string[] productNames, string? callbackUri, string zipFileName)
        {
            var httpClient = App!.CreateHttpClient(ProcessNames.OrchestratorService);

            var jobId = await OrchestratorCommands.ProductNamesInCustomAssemblyPipelineSubmitJobAsync(httpClient, callbackUri, productNames);

            await OrchestratorCommands.WaitForJobCompletionAsync(httpClient, jobId);

            await OrchestratorCommands.VerifyBuildStatusAsync(httpClient, jobId);

            var exchangeSetDownloadPath = await ZipStructureComparer.DownloadExchangeSetAsZipAsync(jobId, App!);
            var sourceZipPath = Path.Combine(ProjectDirectory!, "TestData", zipFileName);

            ZipStructureComparer.CompareZipFilesExactMatch(sourceZipPath, exchangeSetDownloadPath, productNames);
        }
    }
}
