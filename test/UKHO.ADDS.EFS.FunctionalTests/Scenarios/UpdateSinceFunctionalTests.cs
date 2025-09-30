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
    public class UpdateSinceFunctionalTests(StartupFixture startup, ITestOutputHelper output) : FunctionalTestBase(startup, output)
    {
        private readonly string _requestId = $"job-updateSinceAutoTest-" + Guid.NewGuid();
        private string _batchId = "";
        private string _endpoint = "/v2/exchangeSet/s100/updatesSince";
        private bool _assertCallbackTxtFile = false;

        private async Task SubmitPostRequestAndCheckResponse(string requestId, object requestPayload, string endpoint, HttpStatusCode expectedStatusCode, string expectedErrorMessage)
        {
            var response = await OrchestratorClient.PostRequestAsync(requestId, requestPayload, endpoint);
            Assert.Equal(expectedStatusCode, response.StatusCode);

            if (expectedStatusCode != HttpStatusCode.Accepted && expectedErrorMessage != "")
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                _output.WriteLine($"ResponseContent: {responseBody}");
                Assert.Contains(expectedErrorMessage, responseBody);
            }
        }


        private void SetEndpoint(string? callbackUri, string? productIdentifier)
        {
            if (callbackUri == null && productIdentifier != null)
            {
                _endpoint += $"?productIdentifier={productIdentifier}";
            }
            else if (callbackUri != null)
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

                if (productIdentifier != null)
                {
                    _endpoint += $"&productIdentifier={productIdentifier}";
                }
            }
        }


        private async Task TestExecutionSteps(object payload, string zipFileName, int expectedRequestedProductCount, int expectedExchangeSetProductCount, string[]? products = null)
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

            _output.WriteLine($"Trying to download file V01X01_{_requestId}.zip ... {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
            var exchangeSetDownloadPath = await MockFilesClient.DownloadExchangeSetAsZipAsync(_requestId);
            var sourceZipPath = Path.Combine(AspireTestHost.ProjectDirectory!, "TestData", zipFileName);
            ZipArchiveAssertions.CompareZipFilesExactMatch(sourceZipPath, exchangeSetDownloadPath, products);
        }

        //PBI 246464 - Consume ESS API - Update Since Endpoint
        //PBI 242767 - Input validation for the ESS API - Update Since Endpoint
        [Theory]
        [DisableParallelization] // This test runs in parallel with other tests. However, its test cases are run sequentially.
        [InlineData("https://valid.com/callback", "s101", HttpStatusCode.Accepted, "s101UpdateSinceProduct.zip", 0, 0, 1, new [] { "101GB40079ABCDEFG" })] // Test Case 244582 - Valid Format and Test Case 247827 - Valid Format
        [InlineData("", "s102", HttpStatusCode.Accepted, "s102UpdateSinceProduct.zip", 0, 0, 1, new [] { "102NO32904820801012" })] // Test Case 244585 -  Valid input with blank CallBack Uri
        [InlineData(null, "s104", HttpStatusCode.Accepted, "s104UpdateSinceProduct.zip", 0, 0, 1, new [] { "104US00_CHES_TYPE1_20210630_0600" })] // Valid input with null CallBack Uri
        [InlineData("https://valid.com/callback", "s111", HttpStatusCode.Accepted, "s111UpdateSinceProduct.zip", -15, 0, 1, new [] { "111US00_CHES_DCF8_20190703T00Z" })] // Test Case 245730 - Past Date less than 28 days
        public async Task ValidateUSWithValidPayloadHavingProductIdentifier(string? callbackUri, string? productIdentifier, HttpStatusCode expectedStatusCode, string zipFileName, int days, int expectedRequestedProductCount, int expectedExchangeSetProductCount, string[] productNames)
        {
            using var scope = new AssertionScope(); // root scope

            var requestPayload = $"{{ \"sinceDateTime\": \"{DateTime.UtcNow.AddDays(days):yyyy-MM-ddTHH:mm:ss.fffZ}\" }}";
            //var requestPayload = $"{{ \"sinceDateTime\": \"{DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")}\" }}";

            SetEndpoint(callbackUri, productIdentifier);

            _output.WriteLine($"RequestId: {_requestId}\nRequest EndPoint: {_endpoint}\nRequest Payload: {requestPayload}\nExpectedStatusCode: {expectedStatusCode}");

            await TestExecutionSteps(requestPayload, zipFileName, expectedRequestedProductCount, expectedExchangeSetProductCount, productNames);
        }

        [Theory]
        [InlineData("https://valid.com/callback", "", HttpStatusCode.Accepted, "UpdateSince.zip", 0, 0, 4)] // Valid input with blank Product Identifier
        [InlineData("", "", HttpStatusCode.Accepted, "UpdateSince.zip", 0, 0, 4)] // Valid input with blank CallBack Uri and Product Identifier
        [InlineData("https://valid.com/callback", null, HttpStatusCode.Accepted, "UpdateSince.zip", 0, 0, 4)] // Valid input with null Product Identifier
        [InlineData(null, null, HttpStatusCode.Accepted, "UpdateSince.zip", 0, 0, 4)] // Valid input with null CallBack Uri and Product Identifier
        public async Task ValidateUSWithValidPayloadNotHavingProductIdentifier(string? callbackUri, string? productIdentifier, HttpStatusCode expectedStatusCode, string zipFileName, int days, int expectedRequestedProductCount, int expectedExchangeSetProductCount)
        {
            using var scope = new AssertionScope(); // root scope

            var requestPayload = $"{{ \"sinceDateTime\": \"{DateTime.UtcNow.AddDays(days):yyyy-MM-ddTHH:mm:ss.fffZ}\" }}";
            //var requestPayload = $"{{ \"sinceDateTime\": \"{DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")}\" }}";

            SetEndpoint(callbackUri, productIdentifier);

            _output.WriteLine($"RequestId: {_requestId}\nRequest EndPoint: {_endpoint}\nRequest Payload: {requestPayload}\nExpectedStatusCode: {expectedStatusCode}");

            var productNames = new[] { "101GB40079ABCDEFG", "102NO32904820801012", "104US00_CHES_TYPE1_20210630_0600", "111US00_CHES_DCF8_20190703T00Z" };

            await TestExecutionSteps(requestPayload, zipFileName, expectedRequestedProductCount, expectedExchangeSetProductCount, productNames);
        }

        [Theory]
        [DisableParallelization] // This test runs in parallel with other tests. However, its test cases are run sequentially.
        [InlineData("2025-09-29", "https://valid.com/callback", "s102", HttpStatusCode.BadRequest, "Provided updatesSince is either invalid or invalid format, the valid format is 'ISO 8601 format' (e.g. '2025-09-29T00:00:00Z')")] // Test Case 244583 - Invalid Format when time part is missing
        [InlineData("2025-08-25L07:8:00.00Z", "https://valid.com/callback", "s102", HttpStatusCode.BadRequest, "Provided updatesSince is either invalid or invalid format, the valid format is 'ISO 8601 format' (e.g. '2025-09-29T00:00:00Z')")] // Test Case 244583 - Invalid Format
        [InlineData("", "https://valid.com/callback", "s101", HttpStatusCode.BadRequest, "No UpdateSince date time provided")] // Test Case 246905 - Empty sinceDateTime
        [InlineData("null", "https://valid.com/callback", "s101", HttpStatusCode.BadRequest, "Provided updatesSince is either invalid or invalid format, the valid format is 'ISO 8601 format' (e.g. '2025-09-29T00:00:00Z')")] // Test Case 246905 - Null sinceDateTime
        public async Task ValidateUpdateSincePayloadWithInvalidDates(string sinceDateTime, string? callbackUri, string? productIdentifier, HttpStatusCode expectedStatusCode, string expectedErrorMessage)
        {
            using var scope = new AssertionScope(); // root scope

            var requestPayload = $"{{ \"sinceDateTime\": \"{sinceDateTime}\" }}";

            SetEndpoint(callbackUri, productIdentifier);

            _output.WriteLine($"RequestId: {_requestId}\nRequest EndPoint: {_endpoint}\nRequest Payload: {requestPayload}\nExpectedStatusCode: {expectedStatusCode}\nExpectedErrorMessage:{expectedErrorMessage}");

            await SubmitPostRequestAndCheckResponse(_requestId, requestPayload, _endpoint, expectedStatusCode, expectedErrorMessage);
        }

        [Theory]
        [DisableParallelization] // This test runs in parallel with other tests. However, its test cases are run sequentially.
        [InlineData("https//invalid.com/callback", "s101", HttpStatusCode.BadRequest, "URI is malformed or does not use HTTPS")] // Test Case 244586 -  Invalid CallBack Uri Format
        public async Task ValidateUpdateSincePayloadWithInvalidAndBlankCallBackUri(string? callbackUri, string? productIdentifier, HttpStatusCode expectedStatusCode, string expectedErrorMessage)
        {
            using var scope = new AssertionScope(); // root scope

            var requestPayload = $"{{ \"sinceDateTime\": \"{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ss.fffZ}\" }}";

            SetEndpoint(callbackUri, productIdentifier);

            _output.WriteLine($"RequestId: {_requestId}\nRequest EndPoint: {_endpoint}\nRequest Payload: {requestPayload}\nExpectedStatusCode: {expectedStatusCode}\nExpectedErrorMessage:{expectedErrorMessage}");

            await SubmitPostRequestAndCheckResponse(_requestId, requestPayload, _endpoint, expectedStatusCode, expectedErrorMessage);
        }

        [Theory]
        [DisableParallelization] // This test runs in parallel with other tests. However, its test cases are run sequentially.
        [InlineData(-28, "https://valid.com/callback", "s101", HttpStatusCode.BadRequest, "Date time provided is more than 28 days in the past")] // Test Case 244584 - Date is of 28th day in the past
        [InlineData(-35, "https://valid.com/callback", "s104", HttpStatusCode.BadRequest, "Date time provided is more than 28 days in the past")] // Test Case 245720 - Date more than 28 days in the past
        [InlineData(1, "https://valid.com/callback", "s111", HttpStatusCode.BadRequest, "UpdateSince date cannot be a future date")] // Test Case 245121 - Future Date
        public async Task ValidateUpdateSincePayloadWithPastAndFutureDates(int days, string? callbackUri, string? productIdentifier, HttpStatusCode expectedStatusCode, string expectedErrorMessage)
        {
            using var scope = new AssertionScope(); // root scope

            var requestPayload = $"{{ \"sinceDateTime\": \"{DateTime.UtcNow.AddDays(days):yyyy-MM-ddTHH:mm:ss.fffZ}\" }}";

            SetEndpoint(callbackUri, productIdentifier);

            _output.WriteLine($"RequestId: {_requestId}\nRequest EndPoint: {_endpoint}\nRequest Payload: {requestPayload}\nExpectedStatusCode: {expectedStatusCode}\nExpectedErrorMessage:{expectedErrorMessage}");

            await SubmitPostRequestAndCheckResponse(_requestId, requestPayload, _endpoint, expectedStatusCode, expectedErrorMessage);
        }

        [Theory]
        [DisableParallelization] // This test runs in parallel with other tests. However, its test cases are run sequentially.
        [InlineData("https://valid.com/callback", "S333", HttpStatusCode.BadRequest, "Invalid product identifier, It must be exactly 4 characters, starting with 'S' or 's' followed by a valid 3-digit product code")] // Test Case 244907 - Invalid Product Identifier Format
        [InlineData("https://valid.com/callback", "S101, s102", HttpStatusCode.BadRequest, "Invalid product identifier, It must be exactly 4 characters, starting with 'S' or 's' followed by a valid 3-digit product code")] // Test Case 244907 - Invalid Product Identifier Format
        public async Task ValidateUpdateSincePayloadWithInvalidProductIdentifier(string? callbackUri, string? productIdentifier, HttpStatusCode expectedStatusCode, string expectedErrorMessage)
        {
            using var scope = new AssertionScope(); // root scope

            var requestPayload = $"{{ \"sinceDateTime\": \"{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ss.fffZ}\" }}";

            SetEndpoint(callbackUri, productIdentifier);

            _output.WriteLine($"RequestId: {_requestId}\nRequest EndPoint: {_endpoint}\nRequest Payload: {requestPayload}\nExpectedStatusCode: {expectedStatusCode}\nExpectedErrorMessage:{expectedErrorMessage}");

            await SubmitPostRequestAndCheckResponse(_requestId, requestPayload, _endpoint, expectedStatusCode, expectedErrorMessage);
        }
    }
}
