using System.Text;
using System.Text.Json;
using UKHO.ADDS.EFS.Configuration.Namespaces;
using UKHO.ADDS.EFS.EndToEndTests.Helper;
using Xunit.Abstractions;

namespace UKHO.ADDS.EFS.EndToEnd_Tests.Tests
{
    public class EndToEndTests : TestBase
    {
        private readonly ITestOutputHelper _output;

        public EndToEndTests(ITestOutputHelper output) : base()
        {
            _output = output;
        }

        [Theory]
        [InlineData("ProductName eq '101GB004DEVQK'", "Single101Product.zip")]
        [InlineData("ProductName eq '102CA005N5040W00130'", "Single102Product.zip")]
        [InlineData("ProductName eq '104CA00_20241103T001500Z_GB3DEVK0_dcf2'", "Single104Product.zip")]
        [InlineData("ProductName eq '111FR00_20241217T001500Z_GB3DEVK0_dcf2'", "Single111Product.zip")]
        [InlineData("ProductName eq '111CA00_20241217T001500Z_GB3DEVQ0_dcf2' or ProductName eq '104CA00_20241103T001500Z_GB3DEVK0_dcf2'", "MultipleProducts.zip")]
        [InlineData("startswith(ProductName, '101')", "StartWithS101Products.zip")]
        [InlineData("startswith(ProductName, '102')", "StartWithS102Products.zip")]
        [InlineData("startswith(ProductName, '104')", "StartWithS104Products.zip")]
        [InlineData("startswith(ProductName, '111')", "StartWithS111Products.zip")]
        [InlineData("ProductName eq '101GB004DEVQK' or startswith(ProductName, '104')", "SingleProductAndStartWithS104Products.zip")]
        [InlineData("startswith(ProductName , '111') or startswith(ProductName,'101')", "StartWithS101AndS111.zip")]
        [InlineData("", "WithoutFilter.zip")]
        public async Task S100EndToEnd(string filter, string zipFileName)
        {
            var httpClient = App!.CreateHttpClient(ProcessNames.OrchestratorService);

            var jobId = await SubmitJobAsync(httpClient, filter);

            await WaitForJobCompletionAsync(httpClient, jobId);

            await VerifyBuildStatusAsync(httpClient, jobId);

            var exchangeSetDownloadPath = await ZipUtility.DownloadExchangeSetAsZipAsync(jobId, App!);
            var sourceZipPath = Path.Combine(ProjectDirectory!, "TestData", zipFileName);

            ZipUtility.CompareZipFolderStructure(sourceZipPath, exchangeSetDownloadPath);
        }

        [Fact]
        public async Task TestMultipleRequests()
        {

            var httpClient = App!.CreateHttpClient(ProcessNames.OrchestratorService);
            const int expectedNumberOfJobs = 8;
            var jobIds = new List<string>();

            // 1. Submit multiple job requests with empty filter
            for (var i = 0; i < expectedNumberOfJobs; i++)
            {
                try
                {
                    var jobId = await SubmitJobAsync(httpClient, jobNumber: i);
                    jobIds.Add(jobId);
                }
                catch (Exception e)
                {
                    _output.WriteLine(e.Message);
                    _output.WriteLine("Submit Job failed for Job Id :- " + jobIds[i]);
                }
            }

            // 2. Wait for all jobs to complete
            foreach (var jobId in jobIds)
            {
                try
                {
                    await WaitForJobCompletionAsync(httpClient, jobId);
                }
                catch (Exception e)
                {
                    _output.WriteLine(e.Message);
                    _output.WriteLine("Job completion failed for Job Id :- " + jobId);
                }
            }

            // 3. Verify build status for each job
            foreach (var jobId in jobIds)
            {
                try
                {
                    await VerifyBuildStatusAsync(httpClient, jobId);
                }
                catch(Exception e)
                {
                    jobIds.Remove(jobId);
                }
            }
            Assert.Equal(expectedNumberOfJobs, jobIds.Count);
        }

        private static async Task<string> SubmitJobAsync(HttpClient httpClient, string filter = "", int jobNumber = 1)
        {
            var requestId = $"job-000{jobNumber}-" + Guid.NewGuid();
            var payload = new { version = 1, dataStandard = "s100", products = "", filter = $"{filter}" };

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            content.Headers.Add("x-correlation-id", requestId);

            var response = await httpClient.PostAsync("/jobs", content);

            Assert.True(response.IsSuccessStatusCode, $"Expected success status code but got: {response.StatusCode}");

            var responseJson = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var jobId = responseJson.RootElement.GetProperty("jobId").GetString();
            var jobStatus = responseJson.RootElement.GetProperty("jobStatus").GetString();
            var buildStatus = responseJson.RootElement.GetProperty("buildStatus").GetString();

            Assert.Equal("submitted", jobStatus);
            Assert.Equal("scheduled", buildStatus);

            return requestId!;
        }

        private static async Task WaitForJobCompletionAsync(HttpClient httpClient, string jobId)
        {
            const int waitDurationMs = 2000;
            const double maxWaitMinutes = 2;
            var startTime = DateTime.Now;

            string jobState, buildState;
            do
            {
                var response = await httpClient.GetAsync($"/jobs/{jobId}");
                var responseJson = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

                jobState = responseJson.RootElement.GetProperty("jobState").GetString() ?? string.Empty;
                buildState = responseJson.RootElement.GetProperty("buildState").GetString() ?? string.Empty;

                if (jobState == "completed" && buildState == "succeeded")
                    break;

                await Task.Delay(waitDurationMs);
            } while ((DateTime.Now - startTime).TotalMinutes < maxWaitMinutes);

            Assert.Equal("completed", jobState);
            Assert.Equal("succeeded", buildState);
        }

        private static async Task VerifyBuildStatusAsync(HttpClient httpClient, string jobId)
        {
            var response = await httpClient.GetAsync($"/jobs/{jobId}/build");
            Assert.True(response.IsSuccessStatusCode, $"Expected success status code but got: {response.StatusCode}");

            var responseJson = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var builderExitCode = responseJson.RootElement.GetProperty("builderExitCode").GetString();

            Assert.Equal("success", builderExitCode);
        }

    }
}
