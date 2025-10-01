using Meziantou.Xunit;
using UKHO.ADDS.EFS.FunctionalTests.Framework;
using UKHO.ADDS.EFS.FunctionalTests.Http;
using UKHO.ADDS.EFS.FunctionalTests.Infrastructure;
using UKHO.ADDS.EFS.FunctionalTests.Utilities;
using Xunit.Abstractions;

namespace UKHO.ADDS.EFS.FunctionalTests.Scenarios
{
    [Collection("Startup Collection")]
    [EnableParallelization] // Needed to parallelize inside the class, not just between classes
    public class FunctionalTests(StartupFixture startup, ITestOutputHelper output) : FunctionalTestBase(startup, output)
    {
        private readonly string _jobId = $"job-autoTest-" + Guid.NewGuid();
        private readonly string _endpoint = "/jobs";

        private static dynamic CreatePayload(string filter = "", object[]? products = null)
        {
            products ??= [""];
            var payload = new { dataStandard = "s100", products, filter = $"{filter}" };
            return payload;
        }


        [Theory]
        [InlineData("WithoutFilter.zip")]
        public async Task S100FullExchSetTests(string zipFileName)
        {
            await TestExecutionHelper.ExecuteFullExchangeSetTestSteps(_jobId, CreatePayload(), _endpoint, zipFileName);
        }


        [Theory]
        [InlineData("ProductName eq '101GB004DEVQK'", "Single101Product.zip")]
        [InlineData("ProductName eq '102CA005N5040W00130'", "Single102Product.zip")]
        [InlineData("ProductName eq '104CA00_20241103T001500Z_GB3DEVK0_DCF2'", "Single104Product.zip")]
        [InlineData("ProductName eq '111FR00_20241217T001500Z_GB3DEVK0_DCF2'", "Single111Product.zip")]
        public async Task S100FilterTests00(string filter, string zipFileName)
        {
            await TestExecutionHelper.ExecuteFullExchangeSetTestSteps(_jobId, CreatePayload(filter), _endpoint, zipFileName);
        }

        [Theory]
        [InlineData("ProductName eq '111CA00_20241217T001500Z_GB3DEVQ0_DCF2' or ProductName eq '104CA00_20241103T001500Z_GB3DEVK0_DCF2'", "MultipleProducts.zip")]
        [InlineData("ProductName eq '101GB004DEVQK' or startswith(ProductName, '104')", "SingleProductAndStartWithS104Products.zip")]
        public async Task S100FilterTests01(string filter, string zipFileName)
        {
            await TestExecutionHelper.ExecuteFullExchangeSetTestSteps(_jobId, CreatePayload(filter), _endpoint, zipFileName);
        }


        [Theory]
        [InlineData("startswith(ProductName, '101')", "StartWithS101Products.zip")]
        [InlineData("startswith(ProductName, '102')", "StartWithS102Products.zip")]
        [InlineData("startswith(ProductName, '104')", "StartWithS104Products.zip")]
        [InlineData("startswith(ProductName, '111')", "StartWithS111Products.zip")]
        public async Task S100FilterTests02(string filter, string zipFileName)
        {
            await TestExecutionHelper.ExecuteFullExchangeSetTestSteps(_jobId, CreatePayload(filter), _endpoint, zipFileName);
        }

        [Theory]
        [InlineData("startswith(ProductName , '111') or startswith(ProductName,'101')", "StartWithS101AndS111.zip")]
        [InlineData("startswith(ProductName, '101') or startswith(ProductName, '102') or startswith(ProductName, '104') or startswith(ProductName, '111')", "AllProducts.zip")]
        [InlineData("startswith(ProductName, '111') or startswith(ProductName, '121')", "StartWithS111Products.zip")]
        public async Task S100FilterTests03(string filter, string zipFileName)
        {
            await TestExecutionHelper.ExecuteFullExchangeSetTestSteps(_jobId, CreatePayload(filter), _endpoint, zipFileName);
        }

        //Negative scenarios
        [Theory]
        [InlineData("startswith(ProductName, '121')")]
        [InlineData("ProductName eq '131GB004DEVQK'")]
        public async Task S100FilterTestsWithInvalidIdentifier(string filter)
        {
            await TestExecutionHelper.ExecuteFullExchangeSetTestSteps(_jobId, CreatePayload(filter), _endpoint, expectedJobStatus: "upToDate", expectedBuildStatus: "none");
        }

        [Fact]
        public async Task S100ProductsTests()
        {
            var productNames = new string[] { "104CA00_20241103T001500Z_GB3DEVK0_DCF2", "101GB004DEVQP", "101FR005DEVQG" };
            await TestExecutionHelper.ExecuteFullExchangeSetTestSteps(_jobId, CreatePayload(products: productNames), _endpoint, "SelectedProducts.zip", productNames: productNames);
        }

        //If both a filter and specific products are provided, the system should generate the Exchange Set based on the given products.
        [Fact]
        public async Task S100ProductsAndFilterTests()
        {
            var productNames = new string[] { "104CA00_20241103T001500Z_GB3DEVK0_DCF2", "101GB004DEVQP", "101FR005DEVQG" };
            await TestExecutionHelper.ExecuteFullExchangeSetTestSteps(_jobId, CreatePayload(filter: "startswith(ProductName, '101')", products: productNames), _endpoint, "SelectedProductsOnly.zip", productNames: productNames);
        }
    }
}
