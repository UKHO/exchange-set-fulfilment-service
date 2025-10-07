using System.Text.Json;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.VisualStudio.TestPlatform.Utilities;
using UKHO.ADDS.EFS.Domain.Jobs;
using UKHO.ADDS.EFS.FunctionalTests.Diagnostics;
using UKHO.ADDS.EFS.FunctionalTests.Http;

namespace UKHO.ADDS.EFS.FunctionalTests.Assertions
{
    public class ExchangeSetApiAssertions
    {
        public static async Task FullExSetJobsResponseChecks(string jobId, HttpResponseMessage responseJobSubmit, string expectedJobStatus = "submitted", string expectedBuildStatus = "scheduled")
        {
            Assert.True(responseJobSubmit.IsSuccessStatusCode, $"Expected success status code but got: {responseJobSubmit.StatusCode}");

            var responseContent = await responseJobSubmit.Content.ReadAsStringAsync();
            TestOutput.WriteLine($"ResponseContent: {responseContent}");

            var responseJson = JsonDocument.Parse(responseContent);
            var batchId = responseJson.RootElement.GetProperty("batchId").GetString();

            TestOutput.WriteLine($"JobId => Expected: {jobId} Actual: {responseJson.RootElement.GetProperty("jobId").GetString()}\n" +
                $"JobStatus => Expected: {expectedJobStatus} Actual: {responseJson.RootElement.GetProperty("jobStatus").GetString()}\n" +
                $"BuildStatus => Expected: {expectedBuildStatus} Actual: {responseJson.RootElement.GetProperty("buildStatus").GetString()}\n" +
                $"DataStandard => Expected: s100 Actual: {responseJson.RootElement.GetProperty("dataStandard").GetString()}\n" +
                $"BatchId: {batchId}");

            var root = responseJson.RootElement;

            // Check if properties exist and have expected values
            if (root.TryGetProperty("jobId", out var jobIdElement))
            {
                jobIdElement.GetString().Should().Be(jobId!, "JobId should match expected value");
            }
            else
            {
                // If expected, add assertion failure
                Execute.Assertion.FailWith("Response is missing jobId property");
            }

            if (root.TryGetProperty("jobStatus", out var jobStatusElement))
            {
                jobStatusElement.GetString().Should().Be(expectedJobStatus, "JobStatus should match expected value");
            }
            else
            {
                Execute.Assertion.FailWith("Response is missing jobStatus property");
            }

            if (root.TryGetProperty("buildStatus", out var buildStatusElement))
            {
                buildStatusElement.GetString().Should().Be(expectedBuildStatus, "BuildStatus should match expected value");
            }
            else
            {
                Execute.Assertion.FailWith("Response is missing buildStatus property");
            }

            if (root.TryGetProperty("dataStandard", out var dataStandardElement))
            {
                dataStandardElement.GetString().Should().Be("s100", "DataStandard should be s100");
            }
            else
            {
                Execute.Assertion.FailWith("Response is missing dataStandard property");
            }

            // Only check batchId for submitted/scheduled jobs
            if (expectedJobStatus == "submitted" && expectedBuildStatus == "scheduled")
            {
                if (root.TryGetProperty("batchId", out var batchIdElement))
                {
                    batchId = batchIdElement.GetString();
                    Guid.TryParse(batchId, out _).Should().BeTrue($"Expected '{batchId}' to be a valid GUID");
                }
                else
                {
                    Execute.Assertion.FailWith("Response is missing batchId property");
                }
            }
        }

        public static async Task CustomExchangeSetSubmitPostRequestAndCheckResponse(string requestId, object requestPayload, string endpoint, HttpStatusCode expectedStatusCode, string expectedErrorMessage)
        {
            var response = await OrchestratorClient.PostRequestAsync(requestId, requestPayload, endpoint);
            Assert.Equal(expectedStatusCode, response.StatusCode);

            if (expectedStatusCode != HttpStatusCode.Accepted && expectedErrorMessage != "")
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                TestOutput.WriteLine($"Expected ResponseContent: {expectedErrorMessage}");
                TestOutput.WriteLine($"Actual ResponseContent: {responseBody}");
                Assert.Contains(expectedErrorMessage, responseBody);
            }
        }


        public static async Task<string> CustomExSetReqResponseChecks(string requestId, HttpResponseMessage responseJobSubmit, int expectedRequestedProductCount = -1, int expectedExchangeSetProductCount = -1)
        {
            var batchId = "";
            Assert.True(responseJobSubmit.IsSuccessStatusCode, $"Expected success status code but got: {responseJobSubmit.StatusCode}");

            var responseContent = await responseJobSubmit.Content.ReadAsStringAsync();
            TestOutput.WriteLine($"ResponseContent: {responseContent}");

            var responseJson = JsonDocument.Parse(responseContent);
            var root = responseJson.RootElement;

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
                .Should().Contain(DateTime.UtcNow.Date.AddDays(7).ToString("yyyy-MM-dd"),
                    "exchangeSetUrlExpiryDateTime should be 7 days in future");

            // Presence only checks
            root.TryGetProperty("requestedProductsAlreadyUpToDateCount", out _)
                .Should().BeTrue("Response must contain 'requestedProductsAlreadyUpToDateCount'");
            root.TryGetProperty("requestedProductsNotInExchangeSet", out _)
                .Should().BeTrue("Response must contain 'requestedProductsNotInExchangeSet'");

            root.TryGetProperty("links", out var linksElement)
                    .Should().BeTrue("Response must contain 'links' object");

            root.TryGetProperty("fssBatchId", out var batchIdElement)
                    .Should().BeTrue("Response must contain 'fssBatchId'");
            if (batchIdElement.ValueKind != JsonValueKind.Undefined && batchIdElement.ValueKind != JsonValueKind.Null)
            {
                batchId = batchIdElement.GetString();
            }

            batchId.Should().NotBeNullOrWhiteSpace("'fssBatchId' should be a non-empty string");
            if (linksElement.ValueKind == JsonValueKind.Object)
            {
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

            TestOutput.WriteLine($"JobId => {requestId}\n" +
                $"RequestedProductCount => Expected: {expectedRequestedProductCount} Actual: {responseJson.RootElement.GetProperty("requestedProductCount").GetInt64()}\n" +
                $"ExchangeSetProductCount => Expected: {expectedExchangeSetProductCount} Actual: {responseJson.RootElement.GetProperty("exchangeSetProductCount").GetInt64()}\n" +
                $"BatchId: {batchId}");

            /*
             * Need to have strict assertion on batchId format
             * So we should not use Guid.TryParse(batchId, out _).Should().BeTrue($"Expected '{batchId}' to be a valid GUID");
            */
            Assert.True(Guid.TryParse(batchId, out _), $"Expected 'fssBatchId' to be a valid GUID but got: '{batchId}'");

            return responseContent!;
        }


        public static async Task CheckJobCompletionStatus(HttpResponseMessage responseJobStatus)
        {
            Assert.True(responseJobStatus.IsSuccessStatusCode, $"Expected success status code but got: {responseJobStatus.StatusCode}");

            var responseContent = await responseJobStatus.Content.ReadAsStringAsync();
            TestOutput.WriteLine($"ResponseContent: {responseContent}");

            var responseJson = JsonDocument.Parse(responseContent);
            var jobState = responseJson.RootElement.GetProperty("jobState").GetString();
            var buildState = responseJson.RootElement.GetProperty("buildState").GetString();
            var jobStartTime = responseJson.RootElement.GetProperty("timestamp").GetString();

            TestOutput.WriteLine($"JobStartTime: {jobStartTime}");
            TestOutput.WriteLine(jobState == "completed"
                ? $"Job completed successfully with build status: {buildState}"
                : $"Job did not complete successfully. Current job state: {jobState}, build status: {buildState}");

            Assert.True(jobState!.ToLower().Equals("completed") && buildState!.ToLower().Equals("succeeded"),
                "jobState should be 'completed' and buildState should be 'succeeded'");
        }


        public static async Task CheckBuildStatus(HttpResponseMessage responseBuildStatus)
        {
            Assert.True(responseBuildStatus.IsSuccessStatusCode, $"Expected success status code but got: {responseBuildStatus.StatusCode}");

            var responseContent = await responseBuildStatus.Content.ReadAsStringAsync();
            TestOutput.WriteLine($"ResponseContent: {responseContent}");

            var responseJson = JsonDocument.Parse(responseContent);
            var builderExitCode = responseJson.RootElement.GetProperty("builderExitCode").GetString();
            TestOutput.WriteLine(builderExitCode == "success"
                ? "Build completed successfully."
                : $"Build did not complete successfully. Current build status: {builderExitCode}");

            var builderSteps = responseJson.RootElement.GetProperty("builderSteps");
            var nodeStatuses = new Dictionary<string, string>();

            foreach (var step in builderSteps.EnumerateArray())
            {
                var nodeId = step.GetProperty("nodeId").GetString()!;
                var status = step.GetProperty("status").GetString()!;

                nodeStatuses.Add(nodeId, status);
                TestOutput.WriteLine($"Node: {nodeId}, Status: {status}");

                // Verify each step succeeded
                status.Should().Be("succeeded", $"Step '{nodeId}' should have succeeded, but has status: {status}");
            }

            responseJson.RootElement.GetProperty("builderExitCode").GetString().Should().Be("success");
        }
    }
}
