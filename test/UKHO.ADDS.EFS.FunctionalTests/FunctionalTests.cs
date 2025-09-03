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

        //PBI 242670 - Input validation for the ESS API - Product Name Endpoint
        [Theory]
        [InlineData(new object[] { "101GB40079ABCDEFG", "102NO32904820801012", "104US00_CHES_TYPE1_20210630_0600", "111US00_ches_dcf8_20190703T00Z" }, "", HttpStatusCode.Accepted, "")] // Valid input
        [InlineData(new object[] { "101GB40079ABCDEFG", "102NO32904820801012" }, "https://valid.com/callback", HttpStatusCode.Accepted, "")] // Valid input with Valid callbackUri
        [InlineData(new object[] { }, "https://valid.com/callback", HttpStatusCode.BadRequest, "ProductNames cannot be null or empty.")] // Empty array
        [InlineData(new object[] { "" }, "https://valid.com/callback", HttpStatusCode.BadRequest, "ProductNames cannot be null or empty.")] // Array with Empty string
        [InlineData(new object[] { "101GB40079ABCDEFG" }, "InvalidCallbackUri", HttpStatusCode.BadRequest, "Invalid callbackUri format.")] // Invalid CallBackUrl
        [InlineData(new object[] { "101GB40079ABCDEFG", 123, 456, 789 }, "https://valid.com/callback", HttpStatusCode.InternalServerError, "")] // Mixed valid and invalid data types
        public async Task ValidateProductNamesEndpoint(object[] productNames, string callbackUri, HttpStatusCode expectedStatusCode, string expectedErrorMessage)
        {
            var httpClient = App!.CreateHttpClient(ProcessNames.OrchestratorService);

            await OrchestratorCommands.VerifyProductNamesEndpointResponse(productNames, httpClient, callbackUri, expectedStatusCode, expectedErrorMessage);
        }
    }
}
