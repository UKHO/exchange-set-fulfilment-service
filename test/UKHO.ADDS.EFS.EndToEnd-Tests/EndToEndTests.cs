using UKHO.ADDS.EFS.Configuration.Namespaces;
using UKHO.ADDS.EFS.EndToEndTests.Services;
using Xunit.Abstractions;

namespace UKHO.ADDS.EFS.EndToEnd_Tests.Tests
{
    public class EndToEndTests : TestBase
    {
        private readonly ITestOutputHelper _output;

        public EndToEndTests(ITestOutputHelper output)
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

            var jobId = await OrchestratorJobHelper.SubmitJobAsync(httpClient, filter);

            await OrchestratorJobHelper.WaitForJobCompletionAsync(httpClient, jobId);

            await OrchestratorJobHelper.VerifyBuildStatusAsync(httpClient, jobId);

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
                    var jobId = await OrchestratorJobHelper.SubmitJobAsync(httpClient, jobNumber: i);
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
                    await OrchestratorJobHelper.WaitForJobCompletionAsync(httpClient, jobId);
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
                    await OrchestratorJobHelper.VerifyBuildStatusAsync(httpClient, jobId);
                }
                catch(Exception e)
                {
                    jobIds.Remove(jobId);
                }
            }
            Assert.Equal(expectedNumberOfJobs, jobIds.Count);
        }
    }
}
