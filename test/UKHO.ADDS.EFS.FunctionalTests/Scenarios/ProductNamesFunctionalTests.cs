using AwesomeAssertions.Execution;
using Meziantou.Xunit;
using UKHO.ADDS.EFS.FunctionalTests.Assertions;
using UKHO.ADDS.EFS.FunctionalTests.Configuration;
using UKHO.ADDS.EFS.FunctionalTests.Diagnostics;
using UKHO.ADDS.EFS.FunctionalTests.Framework;
using UKHO.ADDS.EFS.FunctionalTests.Infrastructure;
using UKHO.ADDS.EFS.FunctionalTests.Utilities;
using Xunit.Abstractions;

namespace UKHO.ADDS.EFS.FunctionalTests.Scenarios
{
    [Collection("Startup Collection")]
    [EnableParallelization] // Needed to parallelize inside the class, not just between classes
    public class ProductNamesFunctionalTests(StartupFixture startup, ITestOutputHelper output) : FunctionalTestBase(startup, output)
    {
        private readonly string _requestId = $"job-productNamesAutoTest-" + Guid.NewGuid();
        private string _endpoint = TestEndpointConfiguration.ProductNamesEndpoint;
        private bool _assertCallbackTextFile = false;


        private void SetEndpoint(string? callbackUri)
        {
            _endpoint = EndpointUtility.BuildEndpoint(
                TestEndpointConfiguration.ProductNamesEndpoint,
                callbackUri,
                null, // No product identifier needed for this endpoint
                out _assertCallbackTextFile);
        }


        //PBI 244063 - Use the existing Product Names Node (GetS100ProductNamesNode) from existing pipeline (S100AssemblyPipeline) to new pipeline (S100CustomAssemblyPipeline).
        [Theory]
        [DisableParallelization] // This test runs in parallel with other tests. However, its test cases are run sequentially.
        [InlineData(new string[] { "101GB004DEVQK" }, "https://valid.com/callback", "Single101Product.zip", 1, 1)] // Test Case 245610 - Product Name (S-101 product) Node Integration
        [InlineData(new string[] { "102CA005N5040W00130" }, "https://adds-mocks-efs/callback/callback", "Single102Product.zip", 1, 1)] // Test Case 245610 - Product Name (S-102 product) Node Integration
        [InlineData(new string[] { "104CA00_20241103T001500Z_GB3DEVK0_DCF2" }, "", "Single104Product.zip", 1, 1)] // Test Case 243519 - Valid input with valid callBackURI and // Test Case 245610 - Product Name (S-104 product) Node Integration
        [InlineData(new string[] { "111FR00_20241217T001500Z_GB3DEVK0_DCF2" }, null, "Single111Product.zip", 1, 1)] // Test Case 245610 - Product Name (S-111 product) Node Integration
        [InlineData(new string[] { "111CA00_20241217T001500Z_GB3DEVQ0_DCF2", "104CA00_20241103T001500Z_GB3DEVK0_DCF2" }, "https://valid.com/callback", "MultipleProducts.zip", 2, 2)]   // Test Case 243519 - Valid input and // Test Case 245610 - Product Names (multiple products) Node Integration
        public async Task ValidateProductNamesNodeInCustomAssemblyPipeline(string[] requestPayload, string? callbackUri, string zipFileName, int expectedRequestedProductCount, int expectedExchangeSetProductCount)
        {
            using var scope = new AssertionScope(); // root scope

            SetEndpoint(callbackUri);

            TestOutput.WriteLine($"RequestId: {_requestId}\nRequest EndPoint: {_endpoint}\nRequest Payload: {System.Text.Json.JsonSerializer.Serialize(requestPayload)}\nCallbackUri: {callbackUri}\nExpectedZipFileName:{zipFileName}");

            await TestExecutionHelper.ExecuteCustomExchangeSetTestSteps(_requestId, requestPayload, _endpoint, zipFileName, expectedRequestedProductCount, expectedExchangeSetProductCount, _assertCallbackTextFile, requestPayload);
        }

        //PBI 242670 - Input validation for the ESS API - Product Name Endpoint
        [Theory]
        [DisableParallelization] // This test runs in parallel with other tests. However, its test cases are run sequentially.
        [InlineData(new object[] { "112GB40079ABCDEFG" }, "https://valid.com/callback", HttpStatusCode.BadRequest, "'112GB40079ABCDEFG' starts with digits '112' which is not a valid S-100 product identifier")] // Test Case 245717 -Invalid Product
        [InlineData(new object[] { "" }, "https://valid.com/callback", HttpStatusCode.BadRequest, "ProductName cannot be null or empty")] //Test Case 243605 - Array with Empty string
        [InlineData(new object[] { "101GB40079ABCDEFG" }, "invalidCallbackUri.com", HttpStatusCode.BadRequest, "URI is malformed or does not use HTTPS")] // Test Case 245020 - Invalid CallBackUrl
        [InlineData(new object[] { }, "https://valid.com/callback", HttpStatusCode.BadRequest, "Either body is null or malformed")] // Test Case 243604 - Empty array
        /*
         * Suppressing the 1 failing assertion for the below bug
         * BUG-247982 
         * Once resolved , please reintroduce the assertion for response body "Either body is null or malformed" as currently passing "" to suppress assertion failure
         * Updates on 01 Oct 2025, this bug will not be fixed, and the specifications will be update as per current behavior that for current scenarios under automation where the api responds back with 400 but the response body will be blank
         */
        [InlineData(new object[] { "101GB40079ABCDEFG", 123, 456, 789 }, "https://valid.com/callback", HttpStatusCode.BadRequest, "")] //Test Case 243659 - Mixed valid and invalid data types
        public async Task ValidateProductNamesPayloadWithInvalidInputs(object[] productNames, string? callbackUri, HttpStatusCode expectedStatusCode, string expectedErrorMessage)
        {
            using var scope = new AssertionScope(); // root scope

            SetEndpoint(callbackUri);

            TestOutput.WriteLine($"RequestId: {_requestId}\nRequest EndPoint: {_endpoint}\nRequest Payload: {System.Text.Json.JsonSerializer.Serialize(productNames)}\nExpectedStatusCode: {expectedStatusCode}\nExpectedErrorMessage:{expectedErrorMessage}");

            await ExchangeSetApiAssertions.CustomExchangeSetSubmitPostRequestAndCheckResponse(_requestId, productNames, _endpoint, expectedStatusCode, expectedErrorMessage);
        }

    }
}
