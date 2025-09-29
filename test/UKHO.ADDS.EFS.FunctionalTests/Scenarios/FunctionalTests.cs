using System.Text.Json;
using FluentAssertions;
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
    public class FunctionalTests : FunctionalTestBase
    {
        private string _jobId = "";
        private string _endpoint = "/jobs";

        public FunctionalTests(StartupFixture startup, ITestOutputHelper output) : base(startup, output)
        {
            _jobId = $"job-autoTest-" + Guid.NewGuid();
        }


        private object CreatePayload(string filter = "", object[]? products = null)
        {
            products ??= new object[] { "" };
            var payload = new { dataStandard = "s100", products = products, filter = $"{filter}" };
            return payload;
        }


        private async Task CheckJobsResponse(HttpResponseMessage responseJobSubmit, string expectedJobStatus = "submitted", string expectedBuildStatus = "scheduled")
        {
            Assert.True(responseJobSubmit.IsSuccessStatusCode, $"Expected success status code but got: {responseJobSubmit.StatusCode}");

            var responseContent = await responseJobSubmit.Content.ReadAsStringAsync();
            _output.WriteLine($"ResponseContent: {responseContent}");

            var responseJson = JsonDocument.Parse(responseContent);
            var batchId = responseJson.RootElement.GetProperty("batchId").GetString();

            _output.WriteLine($"JobId => Expected: {_jobId} Actual: {responseJson.RootElement.GetProperty("jobId").GetString()}\n" +
                $"JobStatus => Expected: {expectedJobStatus} Actual: {responseJson.RootElement.GetProperty("jobStatus").GetString()}\n" +
                $"BuildStatus => Expected: {expectedBuildStatus} Actual: {responseJson.RootElement.GetProperty("buildStatus").GetString()}\n" +
                $"DataStandard => Expected: s100 Actual: {responseJson.RootElement.GetProperty("dataStandard").GetString()}\n" +
                $"BatchId: {batchId}");

            var root = responseJson.RootElement;

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


        private async Task TestExecutionSteps(object payload, string zipFileName)
        {
            var responseJobSubmit = await OrchestratorClient.PostRequestAsync(_jobId, payload, _endpoint);
            await CheckJobsResponse(responseJobSubmit);

            var apiResponseAssertions = new ExchangeSetApiAssertions();

            _output.WriteLine($"Started waiting for job completion ... {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}");
            var responseJobStatus = await OrchestratorClient.WaitForJobCompletionAsync(_jobId);
            await apiResponseAssertions.CheckJobCompletionStatus(responseJobStatus);
            _output.WriteLine($"Finished waiting for job completion ... {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}");

            var responseBuildStatus = await OrchestratorClient.GetBuildStatusAsync(_jobId);
            await apiResponseAssertions.CheckBuildStatus(responseBuildStatus);

            _output.WriteLine($"Trying to download file V01X01_{_jobId}.zip");
            var exchangeSetDownloadPath = await MockFilesClient.DownloadExchangeSetAsZipAsync(_jobId);
            var sourceZipPath = Path.Combine(AspireTestHost.ProjectDirectory!, "TestData", zipFileName);

            ZipArchiveAssertions.CompareZipFilesExactMatch(sourceZipPath, exchangeSetDownloadPath);
        }

        [Theory]
        [InlineData("WithoutFilter.zip")]
        public async Task S100FullExchSetTests(string zipFileName)
        {

            await TestExecutionSteps(CreatePayload(), zipFileName);

        }


        [Theory]
        [InlineData("ProductName eq '101GB004DEVQK'", "Single101Product.zip")]
        [InlineData("ProductName eq '102CA005N5040W00130'", "Single102Product.zip")]
        [InlineData("ProductName eq '104CA00_20241103T001500Z_GB3DEVK0_DCF2'", "Single104Product.zip")]
        [InlineData("ProductName eq '111FR00_20241217T001500Z_GB3DEVK0_DCF2'", "Single111Product.zip")]
        public async Task S100FilterTests00(string filter, string zipFileName)
        {

            await TestExecutionSteps(CreatePayload(filter), zipFileName);

        }

        [Theory]
        [InlineData("ProductName eq '111CA00_20241217T001500Z_GB3DEVQ0_DCF2' or ProductName eq '104CA00_20241103T001500Z_GB3DEVK0_DCF2'", "MultipleProducts.zip")]
        [InlineData("ProductName eq '101GB004DEVQK' or startswith(ProductName, '104')", "SingleProductAndStartWithS104Products.zip")]
        public async Task S100FilterTests01(string filter, string zipFileName)
        {

            await TestExecutionSteps(CreatePayload(filter), zipFileName);

        }


        [Theory]
        [InlineData("startswith(ProductName, '101')", "StartWithS101Products.zip")]
        [InlineData("startswith(ProductName, '102')", "StartWithS102Products.zip")]
        [InlineData("startswith(ProductName, '104')", "StartWithS104Products.zip")]
        [InlineData("startswith(ProductName, '111')", "StartWithS111Products.zip")]
        public async Task S100FilterTests02(string filter, string zipFileName)
        {

            await TestExecutionSteps(CreatePayload(filter), zipFileName);

        }

        [Theory]
        [InlineData("startswith(ProductName , '111') or startswith(ProductName,'101')", "StartWithS101AndS111.zip")]
        [InlineData("startswith(ProductName, '101') or startswith(ProductName, '102') or startswith(ProductName, '104') or startswith(ProductName, '111')", "AllProducts.zip")]
        [InlineData("startswith(ProductName, '111') or startswith(ProductName, '121')", "StartWithS111Products.zip")]
        public async Task S100FilterTests03(string filter, string zipFileName)
        {

            await TestExecutionSteps(CreatePayload(filter), zipFileName);

        }

        //Negative scenarios
        [Theory]
        [InlineData("startswith(ProductName, '121')")]
        [InlineData("ProductName eq '131GB004DEVQK'")]
        public async Task S100FilterTestsWithInvalidIdentifier(string filter)
        {

            var responseFromJob = await OrchestratorClient.PostRequestAsync(_jobId, CreatePayload(filter), _endpoint);
            await CheckJobsResponse(responseFromJob, expectedJobStatus: "upToDate", expectedBuildStatus: "none");
        }

        [Fact]
        public async Task S100ProductsTests()
        {
            var productNames = new string[] { "104CA00_20241103T001500Z_GB3DEVK0_DCF2", "101GB004DEVQP", "101FR005DEVQG" };
            await TestExecutionSteps(CreatePayload(products: productNames), "SelectedProducts.zip");
        }

        //If both a filter and specific products are provided, the system should generate the Exchange Set based on the given products.
        [Fact]
        public async Task S100ProductsAndFilterTests()
        {
            var productNames = new string[] { "104CA00_20241103T001500Z_GB3DEVK0_DCF2", "101GB004DEVQP", "101FR005DEVQG" };
            await TestExecutionSteps(CreatePayload(filter: "startswith(ProductName, '101')", products: productNames), "SelectedProductsOnly.zip");

        }
    }
}
