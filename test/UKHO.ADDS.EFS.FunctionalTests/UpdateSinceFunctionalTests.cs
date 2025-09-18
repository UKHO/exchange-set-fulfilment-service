using Meziantou.Xunit;
using UKHO.ADDS.EFS.FunctionalTests.Services;
using Xunit.Abstractions;

namespace UKHO.ADDS.EFS.FunctionalTests
{
    [Collection("Startup Collection")]
    [EnableParallelization] // Needed to parallelize inside the class, not just between classes
    public class UpdateSinceFunctionalTests : TestBase
    {
        private string _requestId = "";
        private string _endpoint = "/v2/exchangeSet/s100/updatesSince";


        public UpdateSinceFunctionalTests(StartupFixture startup, ITestOutputHelper output) : base(startup, output)
        {
            _requestId = $"job-updateSinceAutoTest-" + Guid.NewGuid();
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


        private void setEndpoint(string? callbackUri, string? productIdentifier)
        {
            if (callbackUri != null && productIdentifier != null)
            {
                _endpoint = _endpoint + $"?callbackUri={callbackUri}&productIdentifier={productIdentifier}";
            }
            else if (callbackUri != null)
            {
                _endpoint = _endpoint + $"?callbackUri={callbackUri}";
            }
            else if (productIdentifier != null)
            {
                _endpoint = _endpoint + $"?productIdentifier={productIdentifier}";
            }
        }


        //PBI 242767 - Input validation for the ESS API - Update Since Endpoint
        [Theory]
        [DisableParallelization] // This test runs in parallel with other tests. However, its test cases are run sequentially.
        [InlineData("https://valid.com/callback", "s101", HttpStatusCode.Accepted, "")] // Test Case 244582 - Valid Format
        [InlineData("", "s101", HttpStatusCode.Accepted, "")]
        [InlineData("https://valid.com/callback", "", HttpStatusCode.Accepted, "")]
        [InlineData("", "", HttpStatusCode.Accepted, "")]
        [InlineData("https://valid.com/callback", null, HttpStatusCode.Accepted, "")]
        [InlineData(null, "s101", HttpStatusCode.Accepted, "")]
        [InlineData(null, null, HttpStatusCode.Accepted, "")]
        public async Task ValidateUSPayloadWithValidDates(string? callbackUri, string? productIdentifier, HttpStatusCode expectedStatusCode, string expectedErrorMessage)
        {
            var requestPayload = $"{{ \"sinceDateTime\": \"{DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")}\" }}";

            setEndpoint(callbackUri, productIdentifier);

            _output.WriteLine($"RequestId: {_requestId}\nRequest EndPoint: {_endpoint}\nRequest Payload: {requestPayload}\nExpectedStatusCode: {expectedStatusCode}\nExpectedErrorMessage:{expectedErrorMessage}");

            await submitPostRequestAndCheckResponse(_requestId, requestPayload, _endpoint, expectedStatusCode, expectedErrorMessage);
        }


        [Theory]
        [DisableParallelization] // This test runs in parallel with other tests. However, its test cases are run sequentially.
        [InlineData("2025-09-29", "https://valid.com/callback", "s102", HttpStatusCode.BadRequest, "Provided updatesSince is either invalid or invalid format, the valid format is 'ISO 8601 format' (e.g. '2025-09-29T00:00:00Z')")] // Test Case 244583 - Invalid Format when time part is missing
        [InlineData("2025-08-25L07:8:00.00Z", "https://valid.com/callback", "s102", HttpStatusCode.BadRequest, "Provided updatesSince is either invalid or invalid format, the valid format is 'ISO 8601 format' (e.g. '2025-09-29T00:00:00Z')")] // Test Case 244583 - Invalid Format
        [InlineData("", "https://valid.com/callback", "s101", HttpStatusCode.BadRequest, "No UpdateSince date time provided")] // Test Case 246905 - Empty sinceDateTime
        [InlineData("null", "https://valid.com/callback", "s101", HttpStatusCode.BadRequest, "Provided updatesSince is either invalid or invalid format, the valid format is 'ISO 8601 format' (e.g. '2025-09-29T00:00:00Z')")] // Test Case 246905 - Null sinceDateTime
        public async Task ValidateUSPayloadWithInvalidDates(string sinceDateTime, string? callbackUri, string? productIdentifier, HttpStatusCode expectedStatusCode, string expectedErrorMessage)
        {
            var requestPayload = $"{{ \"sinceDateTime\": \"{sinceDateTime}\" }}";

            setEndpoint(callbackUri, productIdentifier);

            _output.WriteLine($"RequestId: {_requestId}\nRequest EndPoint: {_endpoint}\nRequest Payload: {requestPayload}\nExpectedStatusCode: {expectedStatusCode}\nExpectedErrorMessage:{expectedErrorMessage}");

            await submitPostRequestAndCheckResponse(_requestId, requestPayload, _endpoint, expectedStatusCode, expectedErrorMessage);
        }


        [Theory]
        [DisableParallelization] // This test runs in parallel with other tests. However, its test cases are run sequentially.
        [InlineData("https//invalid.com/callback", "s101", HttpStatusCode.BadRequest, "URI is malformed or does not use HTTPS")] // Test Case 244586 -  Invalid CallBack Uri Format
        [InlineData("", "s102", HttpStatusCode.Accepted, "")] // Test Case 244585 -  Valid input with blank CallBack Uri
        public async Task ValidateUSPayloadWithInvalidAndBlankCallBackUri(string? callbackUri, string? productIdentifier, HttpStatusCode expectedStatusCode, string expectedErrorMessage)
        {
            var requestPayload = $"{{ \"sinceDateTime\": \"{DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")}\" }}";

            setEndpoint(callbackUri, productIdentifier);

            _output.WriteLine($"RequestId: {_requestId}\nRequest EndPoint: {_endpoint}\nRequest Payload: {requestPayload}\nExpectedStatusCode: {expectedStatusCode}\nExpectedErrorMessage:{expectedErrorMessage}");

            await submitPostRequestAndCheckResponse(_requestId, requestPayload, _endpoint, expectedStatusCode, expectedErrorMessage);
        }


        [Theory]
        [DisableParallelization] // This test runs in parallel with other tests. However, its test cases are run sequentially.
        [InlineData("https://valid.com/callback", "SABC", HttpStatusCode.BadRequest, "productIdentifier must be exactly 4 characters: start with 'S' or 's' followed by three digits, with no spaces or extra characters")] // Test Case 244907 - Invalid Product Identifier Format
        public async Task ValidateUSPayloadWithInvalidProductIdentifier(string? callbackUri, string? productIdentifier, HttpStatusCode expectedStatusCode, string expectedErrorMessage)
        {
            var requestPayload = $"{{ \"sinceDateTime\": \"{DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")}\" }}";

            setEndpoint(callbackUri, productIdentifier);

            _output.WriteLine($"RequestId: {_requestId}\nRequest EndPoint: {_endpoint}\nRequest Payload: {requestPayload}\nExpectedStatusCode: {expectedStatusCode}\nExpectedErrorMessage:{expectedErrorMessage}");

            await submitPostRequestAndCheckResponse(_requestId, requestPayload, _endpoint, expectedStatusCode, expectedErrorMessage);
        }


        [Theory]
        [DisableParallelization] // This test runs in parallel with other tests. However, its test cases are run sequentially.
        [InlineData(-28, "https://valid.com/callback", "s101", HttpStatusCode.BadRequest, "Date time provided is more than 28 days in the past")] // Test Case 244584 - Date is of 28th day in the past
        [InlineData(-14, "https://valid.com/callback", "s102", HttpStatusCode.Accepted, "")] // Test Case 245730 - Past Date less than 28 days
        [InlineData(-35, "https://valid.com/callback", "s104", HttpStatusCode.BadRequest, "Date time provided is more than 28 days in the past")] // Test Case 245720 - Date more than 28 days in the past
        [InlineData(1, "https://valid.com/callback", "s111", HttpStatusCode.BadRequest, "UpdateSince date cannot be a future date")] // Test Case 245121 - Future Date
        public async Task ValidateUSPayloadWithPastAndFutureDates(int days, string? callbackUri, string? productIdentifier, HttpStatusCode expectedStatusCode, string expectedErrorMessage)
        {
            var requestPayload = $"{{ \"sinceDateTime\": \"{DateTime.UtcNow.AddDays(days).ToString("yyyy-MM-ddTHH:mm:ss.fffZ")}\" }}";

            setEndpoint(callbackUri, productIdentifier);

            _output.WriteLine($"RequestId: {_requestId}\nRequest EndPoint: {_endpoint}\nRequest Payload: {requestPayload}\nExpectedStatusCode: {expectedStatusCode}\nExpectedErrorMessage:{expectedErrorMessage}");

            await submitPostRequestAndCheckResponse(_requestId, requestPayload, _endpoint, expectedStatusCode, expectedErrorMessage);
        }
    }
}
