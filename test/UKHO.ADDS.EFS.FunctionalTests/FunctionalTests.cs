using System.Text.Json;
using FluentAssertions;
using FluentAssertions.Execution;
using Meziantou.Xunit;
using UKHO.ADDS.EFS.FunctionalTests.Services;
using Xunit.Abstractions;

namespace UKHO.ADDS.EFS.FunctionalTests
{
    [Collection("Startup")]
    public class FunctionalTests
    {
        private readonly ITestOutputHelper _output;
        private string _jobId = "";

        public FunctionalTests(ITestOutputHelper output)
        {
            _output = output;
            _jobId = $"job-autoTest-" + Guid.NewGuid();
        }


        private object createPayload(string filter = "", object[]? products = null)
        {
            products ??= new object[] { "" };
            var payload = new { dataStandard = "s100", products = products, filter = $"{filter}" };
            return payload;
        }


        private async Task checkJobsResponce(HttpResponseMessage response, string expectedJobStatus = "submitted", string expectedBuildStatus = "scheduled")
        {
            response.IsSuccessStatusCode.Should().BeTrue($"Expected success status code but got: {response.StatusCode}");
            
            var responseJson = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var batchId = responseJson.RootElement.GetProperty("batchId").GetString();

            _output.WriteLine($"JobId => Expected: {_jobId} Actual: {responseJson.RootElement.GetProperty("jobId").GetString()}\n" +
                $"JobStatus => Expected: {expectedJobStatus} Actual: {responseJson.RootElement.GetProperty("jobStatus").GetString()}\n" +
                $"BuildStatus => Expected: {expectedBuildStatus} Actual: {responseJson.RootElement.GetProperty("buildStatus").GetString()}\n" +
                $"DataStandard => Expected: s100 Actual: {responseJson.RootElement.GetProperty("dataStandard").GetString()} " +
                $"BatchId: {batchId}");

            var root = responseJson.RootElement;

            using (new AssertionScope())
            {
                // Check if properties exist and have expected values
                if (root.TryGetProperty("jobId", out var jobIdElement))
                {
                    jobIdElement.GetString().Should().Be(_jobId!, "JobId should match expected value");
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
        }


        private async Task checkJobCompletionStatus(HttpResponseMessage response)
        {
            var responseJson = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var jobState = responseJson.RootElement.GetProperty("jobState").GetString();
            var buildState = responseJson.RootElement.GetProperty("buildStatus").GetString();
            var jobStartTime = responseJson.RootElement.GetProperty("timestamp").GetString();

            _output.WriteLine($"JobStartTime: {jobStartTime}" + " and " + jobState == "completed"
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
                if (root.TryGetProperty("buildStatus", out var buildStatusElement))
                {
                    buildStatusElement.GetString().Should().Be("succeeded", "BuildStatus should be succeeded");
                }
                else
                {
                    Execute.Assertion.FailWith("Response is missing buildStatus property");
                }
            }
        }


        private async Task checkBuildStatus(HttpResponseMessage response)
        {
            response.IsSuccessStatusCode.Should().BeTrue($"Expected success status code but got: {response.StatusCode}");

            var responseJson = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var builderExitCode = responseJson.RootElement.GetProperty("builderExitCode").GetString();
            _output.WriteLine(builderExitCode == "success"
                ? "Build completed successfully."
                : $"Build did not complete successfully. Current build status: {builderExitCode}");

            var builderSteps = responseJson.RootElement.GetProperty("builderSteps");
            var nodeStatuses = new Dictionary<string, string>();

            foreach (var step in builderSteps.EnumerateArray())
            {
                var nodeId = step.GetProperty("nodeId").GetString()!;
                var status = step.GetProperty("status").GetString()!;

                nodeStatuses.Add(nodeId, status);
                _output.WriteLine($"Node: {nodeId}, Status: {status}");

                // Verify each step succeeded
                //status.Should().Be("succeeded", $"Step '{nodeId}' should have succeeded, but has status: {status}");
            }

            responseJson.RootElement.GetProperty("builderExitCode").GetString().Should().Be("success");
        }


        private async Task testExecutionMethod(object payload, string zipFileName)
        {
            var response = await OrchestratorCommands.SubmitJobAsync(_jobId, payload);
            await checkJobsResponce(response);

            response = await OrchestratorCommands.WaitForJobCompletionAsync(_jobId);
            await checkJobCompletionStatus(response);

            response = await OrchestratorCommands.GetBuildStatusAsync(_jobId);
            await checkBuildStatus(response);

            var exchangeSetDownloadPath = await ZipStructureComparer.DownloadExchangeSetAsZipAsync(_jobId);
            var sourceZipPath = Path.Combine(AspireResourceSingleton.ProjectDirectory!, "TestData", zipFileName);

            ZipStructureComparer.CompareZipFilesExactMatch(sourceZipPath, exchangeSetDownloadPath);
        }

        [Theory]
        [DisableParallelization] // This test runs in parallel with other tests. However, its test cases are run sequentially.
        [InlineData("WithoutFilter.zip")]
        public async Task S100FullExchSetTests(string zipFileName)
        {

            await testExecutionMethod(createPayload(), zipFileName);
            
        }


        [Theory]
        [DisableParallelization] // This test runs in parallel with other tests. However, its test cases are run sequentially.
        [InlineData("ProductName eq '101GB004DEVQK'", "Single101Product.zip")]
        [InlineData("ProductName eq '102CA005N5040W00130'", "Single102Product.zip")]
        [InlineData("ProductName eq '104CA00_20241103T001500Z_GB3DEVK0_DCF2'", "Single104Product.zip")]
        [InlineData("ProductName eq '111FR00_20241217T001500Z_GB3DEVK0_DCF2'", "Single111Product.zip")]
        [InlineData("ProductName eq '111CA00_20241217T001500Z_GB3DEVQ0_DCF2' or ProductName eq '104CA00_20241103T001500Z_GB3DEVK0_DCF2'", "MultipleProducts.zip")]
        [InlineData("ProductName eq '101GB004DEVQK' or startswith(ProductName, '104')", "SingleProductAndStartWithS104Products.zip")]
        public async Task S100FilterTests01(string filter, string zipFileName)
        {

            await testExecutionMethod(createPayload(filter), zipFileName);

        }


        [Theory]
        [DisableParallelization] // This test runs in parallel with other tests. However, its test cases are run sequentially.
        [InlineData("startswith(ProductName, '101')", "StartWithS101Products.zip")]
        [InlineData("startswith(ProductName, '102')", "StartWithS102Products.zip")]
        [InlineData("startswith(ProductName, '104')", "StartWithS104Products.zip")]
        [InlineData("startswith(ProductName, '111')", "StartWithS111Products.zip")]
        [InlineData("startswith(ProductName , '111') or startswith(ProductName,'101')", "StartWithS101AndS111.zip")]
        [InlineData("startswith(ProductName, '101') or startswith(ProductName, '102') or startswith(ProductName, '104') or startswith(ProductName, '111')", "AllProducts.zip")]
        [InlineData("startswith(ProductName, '111') or startswith(ProductName, '121')", "StartWithS111Products.zip")]
        public async Task S100FilterTests02(string filter, string zipFileName)
        {

            await testExecutionMethod(createPayload(filter), zipFileName);

        }

        //Negative scenarios
        [Theory]
        [DisableParallelization] // This test runs in parallel with other tests. However, its test cases are run sequentially.
        [InlineData("startswith(ProductName, '121')")]
        [InlineData("ProductName eq '131GB004DEVQK'")]
        public async Task S100FilterTestsWithInvalidIdentifier(string filter)
        {

            var response = await OrchestratorCommands.SubmitJobAsync(_jobId, createPayload(filter));
            await checkJobsResponce(response, expectedJobStatus: "upToDate", expectedBuildStatus: "none");
        }

        [Fact]
        public async Task S100ProductsTests()
        {
            var productNames = new string[] { "104CA00_20241103T001500Z_GB3DEVK0_DCF2", "101GB004DEVQP", "101FR005DEVQG" };
            await testExecutionMethod(createPayload(products: productNames), "SelectedProducts.zip");
        }

        //If both a filter and specific products are provided, the system should generate the Exchange Set based on the given products.
        [Fact]
        public async Task S100ProductsAndFilterTests()
        {
            var productNames = new string[] { "104CA00_20241103T001500Z_GB3DEVK0_DCF2", "101GB004DEVQP", "101FR005DEVQG" };
            await testExecutionMethod(createPayload(filter: "startswith(ProductName, '101')", products: productNames), "SelectedProductsOnly.zip");

        }
    }
}
