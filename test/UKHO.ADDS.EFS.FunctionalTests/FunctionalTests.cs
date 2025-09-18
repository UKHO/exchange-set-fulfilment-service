using UKHO.ADDS.EFS.FunctionalTests.Services;
using UKHO.ADDS.EFS.Infrastructure.Configuration.Namespaces;
using Xunit.Abstractions;

namespace UKHO.ADDS.EFS.FunctionalTests
{
    public class FunctionalTests : TestBase
    {
        private readonly ITestOutputHelper _output;

        public FunctionalTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Theory]
        [InlineData("ProductName eq '101GB004DEVQK'", "Single101Product.zip")]
        [InlineData("ProductName eq '102CA005N5040W00130'", "Single102Product.zip")]
        [InlineData("ProductName eq '104CA00_20241103T001500Z_GB3DEVK0_DCF2'", "Single104Product.zip")]
        [InlineData("ProductName eq '111FR00_20241217T001500Z_GB3DEVK0_DCF2'", "Single111Product.zip")]
        [InlineData("ProductName eq '111CA00_20241217T001500Z_GB3DEVQ0_DCF2' or ProductName eq '104CA00_20241103T001500Z_GB3DEVK0_DCF2'", "MultipleProducts.zip")]
        [InlineData("startswith(ProductName, '101')", "StartWithS101Products.zip")]
        [InlineData("startswith(ProductName, '102')", "StartWithS102Products.zip")]
        [InlineData("startswith(ProductName, '104')", "StartWithS104Products.zip")]
        [InlineData("startswith(ProductName, '111')", "StartWithS111Products.zip")]
        [InlineData("ProductName eq '101GB004DEVQK' or startswith(ProductName, '104')", "SingleProductAndStartWithS104Products.zip")]
        [InlineData("startswith(ProductName , '111') or startswith(ProductName,'101')", "StartWithS101AndS111.zip")]
        [InlineData("startswith(ProductName, '101') or startswith(ProductName, '102') or startswith(ProductName, '104') or startswith(ProductName, '111')", "AllProducts.zip")]
        [InlineData("startswith(ProductName, '111') or startswith(ProductName, '121')", "StartWithS111Products.zip")]
        [InlineData("", "WithoutFilter.zip")]
        public async Task S100FilterTests(string filter, string zipFileName)
        {
            await Task.Delay(500);
            var jobId = await OrchestratorCommands.SubmitJobAsync(httpClient, filter);
            _output.WriteLine($"S100FilterTest Submitted job with ID: {jobId} for filter: {filter}");

            await OrchestratorCommands.WaitForJobCompletionAsync(httpClient, jobId);

            await OrchestratorCommands.VerifyBuildStatusAsync(httpClient, jobId);

            //rhz: add a delay for testing purpose
            await Task.Delay(1000);
            _output.WriteLine($"S100FilterTest Job {jobId} completed successfully.");

            var exchangeSetDownloadPath = await ZipStructureComparer.DownloadExchangeSetAsZipAsync(jobId, httpClientMock);
            var sourceZipPath = Path.Combine(ProjectDirectory!, "TestData", zipFileName);

            ZipStructureComparer.CompareZipFilesExactMatch(sourceZipPath, exchangeSetDownloadPath);
            await Task.Delay(500);
        }

        //Negative scenarios
        [Theory]
        [InlineData("startswith(ProductName, '121')")]
        [InlineData("ProductName eq '131GB004DEVQK'")]
        public async Task S100FilterTestsWithInvalidIdentifier(string filter)
        {
            await OrchestratorCommands.SubmitJobAsync(httpClient, filter, expectedJobStatus: "upToDate", expectedBuildStatus: "none");
        }

        [Fact]
        public async Task S100ProductsTests()
        {
            await Task.Delay(500);
            var products = new string[] { "104CA00_20241103T001500Z_GB3DEVK0_DCF2", "101GB004DEVQP", "101FR005DEVQG" };

            var jobId = await OrchestratorCommands.SubmitJobAsync(httpClient, products: products);
            _output.WriteLine($"S100ProductsTests Submitted job with ID: {jobId} ");

            await OrchestratorCommands.WaitForJobCompletionAsync(httpClient, jobId);

            await OrchestratorCommands.VerifyBuildStatusAsync(httpClient, jobId);

            //rhz: add a delay for testing purpose
            await Task.Delay(1000);
            _output.WriteLine($"S100ProductsTests Job {jobId} completed successfully.");

            var exchangeSetDownloadPath = await ZipStructureComparer.DownloadExchangeSetAsZipAsync(jobId, httpClientMock);
            var sourceZipPath = Path.Combine(ProjectDirectory!, "TestData", "SelectedProducts.zip");

            ZipStructureComparer.CompareZipFilesExactMatch(sourceZipPath, exchangeSetDownloadPath, products);
            await Task.Delay(500);
        }

        //If both a filter and specific products are provided, the system should generate the Exchange Set based on the given products.
        [Fact]
        public async Task S100ProductsAndFilterTests()
        {
            await Task.Delay(500);
            var products = new string[] { "104CA00_20241103T001500Z_GB3DEVK0_DCF2", "101GB004DEVQP", "101FR005DEVQG" };

            var jobId = await OrchestratorCommands.SubmitJobAsync(httpClient, filter: "startswith(ProductName, '101')", products: products);
            _output.WriteLine($"S100ProductsAndFilterTests Submitted job with ID: {jobId} ");

            await OrchestratorCommands.WaitForJobCompletionAsync(httpClient, jobId);

            await OrchestratorCommands.VerifyBuildStatusAsync(httpClient, jobId);

            //rhz: add a delay for testing purpose
            await Task.Delay(1000);
            _output.WriteLine($"S100ProductsAndFilterTests Job {jobId} completed successfully.");

            var exchangeSetDownloadPath = await ZipStructureComparer.DownloadExchangeSetAsZipAsync(jobId, httpClientMock);
            var sourceZipPath = Path.Combine(ProjectDirectory!, "TestData", "SelectedProductsOnly.zip");

            ZipStructureComparer.CompareZipFilesExactMatch(sourceZipPath, exchangeSetDownloadPath, products);
            await Task.Delay(500);
        }
    }
}
