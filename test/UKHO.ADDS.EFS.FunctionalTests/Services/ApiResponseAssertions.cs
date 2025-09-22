using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.VisualStudio.TestPlatform.Utilities;
using Xunit.Abstractions;

namespace UKHO.ADDS.EFS.FunctionalTests.Services
{
    public class ApiResponseAssertions(ITestOutputHelper output)
    {
        public async Task checkJobCompletionStatus(HttpResponseMessage responseJobStatus)
        {
            responseJobStatus.IsSuccessStatusCode.Should().BeTrue($"Expected success status code but got: {responseJobStatus.StatusCode}");

            var responseContent = await responseJobStatus.Content.ReadAsStringAsync();
            output.WriteLine($"ResponseContent: {responseContent}");

            var responseJson = JsonDocument.Parse(responseContent);
            var jobState = responseJson.RootElement.GetProperty("jobState").GetString();
            var buildState = responseJson.RootElement.GetProperty("buildState").GetString();
            var jobStartTime = responseJson.RootElement.GetProperty("timestamp").GetString();

            output.WriteLine($"JobStartTime: {jobStartTime}");
            output.WriteLine(jobState == "completed"
                ? $"Job completed successfully with build status: {buildState}"
                : $"Job did not complete successfully. Current job state: {jobState}, build status: {buildState}");

            var root = responseJson.RootElement;

            using (new AssertionScope())
            {
                // Check if properties exist and have expected values
                if (root.TryGetProperty("jobState", out var jobStateElement))
                {
                    jobStateElement.GetString().Should().Be("completed", "JobState should be completed");
                }
                else
                {
                    Execute.Assertion.FailWith("Response is missing jobState property");
                }
                if (root.TryGetProperty("buildState", out var buildStatusElement))
                {
                    buildStatusElement.GetString().Should().Be("succeeded", "BuildStatus should be succeeded");
                }
                else
                {
                    Execute.Assertion.FailWith("Response is missing buildState property");
                }
            }
        }


        public async Task checkBuildStatus(HttpResponseMessage responseBuildStatus)
        {
            responseBuildStatus.IsSuccessStatusCode.Should().BeTrue($"Expected success status code but got: {responseBuildStatus.StatusCode}");

            var responseContent = await responseBuildStatus.Content.ReadAsStringAsync();
            output.WriteLine($"ResponseContent: {responseContent}");

            var responseJson = JsonDocument.Parse(responseContent);
            var builderExitCode = responseJson.RootElement.GetProperty("builderExitCode").GetString();
            output.WriteLine(builderExitCode == "success"
                ? "Build completed successfully."
                : $"Build did not complete successfully. Current build status: {builderExitCode}");

            var builderSteps = responseJson.RootElement.GetProperty("builderSteps");
            var nodeStatuses = new Dictionary<string, string>();

            foreach (var step in builderSteps.EnumerateArray())
            {
                var nodeId = step.GetProperty("nodeId").GetString()!;
                var status = step.GetProperty("status").GetString()!;

                nodeStatuses.Add(nodeId, status);
                output.WriteLine($"Node: {nodeId}, Status: {status}");

                // Verify each step succeeded
                //status.Should().Be("succeeded", $"Step '{nodeId}' should have succeeded, but has status: {status}");
            }

            responseJson.RootElement.GetProperty("builderExitCode").GetString().Should().Be("success");
        }

    }
}
