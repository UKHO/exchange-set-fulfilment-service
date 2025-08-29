using UKHO.ADDS.EFS.Domain.Services.Configuration.Namespaces;
using UKHO.ADDS.EFS.FunctionalTests.Services;
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
        [InlineData("ProductName eq '104CA00_20241103T001500Z_GB3DEVK0_dcf2'", "Single104Product.zip")]
        [InlineData("ProductName eq '111FR00_20241217T001500Z_GB3DEVK0_dcf2'", "Single111Product.zip")]
        [InlineData("ProductName eq '111CA00_20241217T001500Z_GB3DEVQ0_dcf2' or ProductName eq '104CA00_20241103T001500Z_GB3DEVK0_dcf2'", "MultipleProducts.zip")]
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
            var httpClient = App!.CreateHttpClient(ProcessNames.OrchestratorService);

            var jobId = await OrchestratorCommands.SubmitJobAsync(httpClient, filter);

            await OrchestratorCommands.WaitForJobCompletionAsync(httpClient, jobId);

            await OrchestratorCommands.VerifyBuildStatusAsync(httpClient, jobId);

            var exchangeSetDownloadPath = await ZipStructureComparer.DownloadExchangeSetAsZipAsync(jobId, App!);
            var sourceZipPath = Path.Combine(ProjectDirectory!, "TestData", zipFileName);

            ZipStructureComparer.CompareZipFilesExactMatch(sourceZipPath, exchangeSetDownloadPath);
        }

        //Negative scenarios
        [Theory]
        [InlineData("startswith(ProductName, '121')")]
        [InlineData("ProductName eq '131GB004DEVQK'")]
        public async Task S100FilterTestsWithInvalidIdentifier(string filter)
        {
            var httpClient = App!.CreateHttpClient(ProcessNames.OrchestratorService);

            await OrchestratorCommands.SubmitJobAsync(httpClient, filter, expectedJobStatus: "upToDate", expectedBuildStatus: "none");
        }

        [Fact]
        public async Task S100ProductsTests()
        {
            var httpClient = App!.CreateHttpClient(ProcessNames.OrchestratorService);

            var products = new string[] { "104CA00_20241103T001500Z_GB3DEVK0_dcf2", "101GB004DEVQP", "101FR005DEVQG" };

            var jobId = await OrchestratorCommands.SubmitJobAsync(httpClient, products: products);

            await OrchestratorCommands.WaitForJobCompletionAsync(httpClient, jobId);

            await OrchestratorCommands.VerifyBuildStatusAsync(httpClient, jobId);

            var exchangeSetDownloadPath = await ZipStructureComparer.DownloadExchangeSetAsZipAsync(jobId, App!);
            var sourceZipPath = Path.Combine(ProjectDirectory!, "TestData", "SelectedProducts.zip");

            ZipStructureComparer.CompareZipFilesExactMatch(sourceZipPath, exchangeSetDownloadPath, products);
        }

        //If both a filter and specific products are provided, the system should generate the Exchange Set based on the given products.
        [Fact]
        public async Task S100ProductsAndFilterTests()
        {
            var httpClient = App!.CreateHttpClient(ProcessNames.OrchestratorService);

            var products = new string[] { "104CA00_20241103T001500Z_GB3DEVK0_dcf2", "101GB004DEVQP", "101FR005DEVQG" };

            var jobId = await OrchestratorCommands.SubmitJobAsync(httpClient, filter: "startswith(ProductName, '101')", products: products);

            await OrchestratorCommands.WaitForJobCompletionAsync(httpClient, jobId);

            await OrchestratorCommands.VerifyBuildStatusAsync(httpClient, jobId);

            var exchangeSetDownloadPath = await ZipStructureComparer.DownloadExchangeSetAsZipAsync(jobId, App!);
            var sourceZipPath = Path.Combine(ProjectDirectory!, "TestData", "SelectedProductsOnly.zip");

            ZipStructureComparer.CompareZipFilesExactMatch(sourceZipPath, exchangeSetDownloadPath, products);
        }
    }
}
