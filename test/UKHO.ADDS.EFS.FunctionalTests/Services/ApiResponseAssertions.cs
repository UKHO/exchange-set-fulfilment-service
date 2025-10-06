using System.Text.Json;
using FluentAssertions;
using FluentAssertions.Execution;

namespace UKHO.ADDS.EFS.FunctionalTests.Services
{
    public class ApiResponseAssertions()
    {
        public async Task<string> CheckCustomExSetReqResponse(string requestId, HttpResponseMessage responseJobSubmit, int expectedRequestedProductCount = -1, int expectedExchangeSetProductCount = -1)
        {
            Assert.True(responseJobSubmit.IsSuccessStatusCode, $"Expected success status code but got: {responseJobSubmit.StatusCode}");

            var responseContent = await responseJobSubmit.Content.ReadAsStringAsync();
            TestOutputContext.WriteLine($"ResponseContent: {responseContent}");

            var responseJson = JsonDocument.Parse(responseContent);
            var batchId = responseJson.RootElement.GetProperty("fssBatchId").GetString()!;

            TestOutputContext.WriteLine($"JobId => {requestId}\n" +
                $"RequestedProductCount => Expected: {expectedRequestedProductCount} Actual: {responseJson.RootElement.GetProperty("requestedProductCount").GetInt64()}\n" +
                $"ExchangeSetProductCount => Expected: {expectedExchangeSetProductCount} Actual: {responseJson.RootElement.GetProperty("exchangeSetProductCount").GetInt64()}\n" +
                $"BatchId: {batchId}");

            var root = responseJson.RootElement;

            root.TryGetProperty("fssBatchId", out var batchIdElement)
                    .Should().BeTrue("Response must contain 'fssBatchId'");
            batchId = batchIdElement.GetString();

            batchId.Should().NotBeNullOrWhiteSpace("'fssBatchId' should be a non-empty string");
            Guid.TryParse(batchId, out _).Should().BeTrue($"Expected '{batchId}' to be a valid GUID");

            root.TryGetProperty("requestedProductCount", out var requestedProductCountElement)
                .Should().BeTrue("Response must contain 'requestedProductCount'");
            if (expectedRequestedProductCount != -1)
            {
                requestedProductCountElement.GetInt64()
                .Should().Be(expectedRequestedProductCount, "requestedProductCount should match expected value");
            }

            root.TryGetProperty("exchangeSetProductCount", out var exchangeSetProductCountElement)
                .Should().BeTrue("Response must contain 'exchangeSetProductCount'");
            if (expectedExchangeSetProductCount != -1)
            {
                exchangeSetProductCountElement.GetInt64()
                .Should().Be(expectedExchangeSetProductCount, "exchangeSetProductCount should match expected value");
            }

            root.TryGetProperty("exchangeSetUrlExpiryDateTime", out var exchangeSetUrlExpiryDateTimeElement)
                .Should().BeTrue("Response must contain 'exchangeSetUrlExpiryDateTime'");
            exchangeSetUrlExpiryDateTimeElement.GetString()
                .Should().Contain(DateTime.Today.AddDays(7).ToString("yyyy-MM-dd"),
                    "exchangeSetUrlExpiryDateTime should be 7 days in future");

            // Presence only checks
            root.TryGetProperty("requestedProductsAlreadyUpToDateCount", out _)
                .Should().BeTrue("Response must contain 'requestedProductsAlreadyUpToDateCount'");
            root.TryGetProperty("requestedProductsNotInExchangeSet", out _)
                .Should().BeTrue("Response must contain 'requestedProductsNotInExchangeSet'");

            root.TryGetProperty("links", out var linksElement)
                    .Should().BeTrue("Response must contain 'links' object");

            if (linksElement.ValueKind == JsonValueKind.Object)
            {
                // Extract batchId after prior validation (safe to re-read)
                batchId = root.GetProperty("fssBatchId").GetString()!;

                // exchangeSetBatchStatusUri
                linksElement.TryGetProperty("exchangeSetBatchStatusUri", out var statusElement)
                    .Should().BeTrue("links should contain 'exchangeSetBatchStatusUri'");
                statusElement.TryGetProperty("uri", out var statusUriElement)
                    .Should().BeTrue("'exchangeSetBatchStatusUri' should contain 'uri'");
                statusUriElement.GetString()
                    .Should().Contain($"/fss/batch/{batchId}/status",
                        "exchangeSetBatchStatusUri should contain the correct batchId");

                // exchangeSetBatchDetailsUri
                linksElement.TryGetProperty("exchangeSetBatchDetailsUri", out var detailsElement)
                    .Should().BeTrue("links should contain 'exchangeSetBatchDetailsUri'");
                detailsElement.TryGetProperty("uri", out var detailsUriElement)
                    .Should().BeTrue("'exchangeSetBatchDetailsUri' should contain 'uri'");
                detailsUriElement.GetString()
                    .Should().Contain($"/fss/batch/{batchId}",
                        "exchangeSetBatchDetailsUri should contain the correct batchId");

                // exchangeSetFileUri
                linksElement.TryGetProperty("exchangeSetFileUri", out var fileElement)
                    .Should().BeTrue("links should contain 'exchangeSetFileUri'");
                fileElement.TryGetProperty("uri", out var fileUriElement)
                    .Should().BeTrue("'exchangeSetFileUri' should contain 'uri'");
                fileUriElement.GetString()
                    .Should().Contain($"/fss/batch/{batchId}/files/V01X01_{requestId}.zip",
                        "exchangeSetFileUri should contain the correct batchId and jobId");
            }

            return responseContent!;
        }


        public async Task CheckJobCompletionStatus(HttpResponseMessage responseJobStatus)
        {
            Assert.True(responseJobStatus.IsSuccessStatusCode, $"Expected success status code but got: {responseJobStatus.StatusCode}");

            var responseContent = await responseJobStatus.Content.ReadAsStringAsync();
            TestOutputContext.WriteLine($"ResponseContent: {responseContent}");

            var responseJson = JsonDocument.Parse(responseContent);
            var jobState = responseJson.RootElement.GetProperty("jobState").GetString();
            var buildState = responseJson.RootElement.GetProperty("buildState").GetString();
            var jobStartTime = responseJson.RootElement.GetProperty("timestamp").GetString();

            TestOutputContext.WriteLine($"JobStartTime: {jobStartTime}");
            TestOutputContext.WriteLine(jobState == "completed"
                ? $"Job completed successfully with build status: {buildState}"
                : $"Job did not complete successfully. Current job state: {jobState}, build status: {buildState}");

            Assert.True(jobState!.ToLower().Equals("completed") && buildState!.ToLower().Equals("succeeded"),
                "jobState should be 'completed' and buildState should be 'succeeded'");
        }


        public async Task CheckBuildStatus(HttpResponseMessage responseBuildStatus)
        {
            Assert.True(responseBuildStatus.IsSuccessStatusCode, $"Expected success status code but got: {responseBuildStatus.StatusCode}");

            var responseContent = await responseBuildStatus.Content.ReadAsStringAsync();
            TestOutputContext.WriteLine($"ResponseContent: {responseContent}");

            var responseJson = JsonDocument.Parse(responseContent);
            var builderExitCode = responseJson.RootElement.GetProperty("builderExitCode").GetString();
            TestOutputContext.WriteLine(builderExitCode == "success"
                ? "Build completed successfully."
                : $"Build did not complete successfully. Current build status: {builderExitCode}");

            var builderSteps = responseJson.RootElement.GetProperty("builderSteps");
            var nodeStatuses = new Dictionary<string, string>();

            foreach (var step in builderSteps.EnumerateArray())
            {
                var nodeId = step.GetProperty("nodeId").GetString()!;
                var status = step.GetProperty("status").GetString()!;

                nodeStatuses.Add(nodeId, status);
                TestOutputContext.WriteLine($"Node: {nodeId}, Status: {status}");

                // Verify each step succeeded
                //status.Should().Be("succeeded", $"Step '{nodeId}' should have succeeded, but has status: {status}");
            }

            responseJson.RootElement.GetProperty("builderExitCode").GetString().Should().Be("success");
        }

    }
}
