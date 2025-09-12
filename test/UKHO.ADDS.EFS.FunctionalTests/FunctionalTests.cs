using System.Text;
using System.Text.Json;
using UKHO.ADDS.EFS.Configuration.Namespaces;
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
        //[InlineData("ProductName eq '102CA005N5040W00130'", "Single102Product.zip")]
        //[InlineData("ProductName eq '104CA00_20241103T001500Z_GB3DEVK0_dcf2'", "Single104Product.zip")]
        //[InlineData("ProductName eq '111FR00_20241217T001500Z_GB3DEVK0_dcf2'", "Single111Product.zip")]
        //[InlineData("ProductName eq '111CA00_20241217T001500Z_GB3DEVQ0_dcf2' or ProductName eq '104CA00_20241103T001500Z_GB3DEVK0_dcf2'", "MultipleProducts.zip")]
        //[InlineData("startswith(ProductName, '101')", "StartWithS101Products.zip")]
        //[InlineData("startswith(ProductName, '102')", "StartWithS102Products.zip")]
        //[InlineData("startswith(ProductName, '104')", "StartWithS104Products.zip")]
        //[InlineData("startswith(ProductName, '111')", "StartWithS111Products.zip")]
        //[InlineData("ProductName eq '101GB004DEVQK' or startswith(ProductName, '104')", "SingleProductAndStartWithS104Products.zip")]
        //[InlineData("startswith(ProductName , '111') or startswith(ProductName,'101')", "StartWithS101AndS111.zip")]
        //[InlineData("startswith(ProductName, '101') or startswith(ProductName, '102') or startswith(ProductName, '104') or startswith(ProductName, '111')", "AllProducts.zip")]
        //[InlineData("startswith(ProductName, '111') or startswith(ProductName, '121')", "StartWithS111Products.zip")]
        //[InlineData("", "WithoutFilter.zip")]
        public async Task S100FilterTests(string filter, string zipFileName)
        {
            var httpClient = App!.CreateHttpClient(ProcessNames.OrchestratorService);

            // rhz: debugging
            var dummy = zipFileName;
            var requestId = $"job-0001-" + Guid.NewGuid();
            var payload = new { version = 1, dataStandard = "s100", products = "", filter = $"{filter}" };

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            content.Headers.Add("x-correlation-id", requestId);

            var response = await httpClient.PostAsync("/jobs", content);

            _output.WriteLine($"Job ID: {requestId}");

            await Task.Delay(120000);

            var logs = LoggerProvider.GetLogs();
            ////_output.WriteLine($"Logs for Job ID {requestId}:\n{string.Join("\n", logs.Select(log => $"{log.LogLevel}: {log.Message}"))}");
            _output.WriteLine($"Logs for Job ID {requestId}:");
            foreach (var log in logs)  //.OrderByDescending(l => l.EventId.Id)
            {
                if (log.Message.Contains("peekonly=true") || log.Message.Contains("s57") || log.Message.Contains("s63") || log.Message.Contains("comp=list") || log.Message.Contains("buildresponse"))
                {
                    continue; // Skip peek logs
                }
                _output.WriteLine($"  [{log.LogLevel}] {log.Message}");
                if (log.Exception != null)
                {
                    _output.WriteLine($"      Exception: {log.Exception.Message}");
                }
            }

            var jobId = requestId; // Assuming the job ID is the same as the request ID for this example
            // rhz: debugging end



            //var jobId = await OrchestratorCommands.SubmitJobAsync(httpClient, filter);
            //_output.WriteLine($"Job ID: {jobId}");

            //await OrchestratorCommands.WaitForJobCompletionAsync(httpClient, jobId);
            //var logs = LoggerProvider.GetLogs();
            //_output.WriteLine($"Logs for Job ID {jobId}:\n{string.Join("\n", logs.Select(log => $"{log.LogLevel}: {log.Message}"))}");

            await OrchestratorCommands.VerifyBuildStatusAsync(httpClient, jobId);

            //var exchangeSetDownloadPath = await ZipStructureComparer.DownloadExchangeSetAsZipAsync(jobId, App!);
            //var sourceZipPath = Path.Combine(ProjectDirectory!, "TestData", zipFileName);

            //ZipStructureComparer.CompareZipFilesExactMatch(sourceZipPath, exchangeSetDownloadPath);
        }

        //Negative scenarios
        //[Theory]
        //[InlineData("startswith(ProductName, '121')")]
        //[InlineData("ProductName eq '131GB004DEVQK'")]
        public async Task S100FilterTestsWithInvalidIdentifier(string filter)
        {
            var httpClient = App!.CreateHttpClient(ProcessNames.OrchestratorService);

            await OrchestratorCommands.SubmitJobAsync(httpClient, filter, expectedJobStatus: "upToDate", expectedBuildStatus: "none");
        }
    }
}
