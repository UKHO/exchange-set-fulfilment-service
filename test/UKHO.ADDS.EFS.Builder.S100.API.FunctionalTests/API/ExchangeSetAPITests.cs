using NUnit.Framework;
using System.Net;
using UKHO.ADDS.EFS.Orchestrator.API.FunctionalTests.Facades;
using UKHO.ADDS.EFS.Orchestrator.API.FunctionalTests.Helpers;
using UKHO.ADDS.EFS.Orchestrator.API.FunctionalTests.Support;

namespace UKHO.ADDS.EFS.Orchestrator.API.FunctionalTests.API
{
    [TestFixture]
    public class ExchangeSetAPITests
    {
        private OrchestratorAPIFacade _orchestratorAPIFacade;
        private ExchangeSetDownloadAPIFacade _exchangeSetDownloadAPIFacade;
        private BlobHelpers _blobHelpers;
        private FileHelpers _fileHelpers;

        [SetUp]
        public void SetUp()
        {
            _orchestratorAPIFacade = new OrchestratorAPIFacade();
            _exchangeSetDownloadAPIFacade = new ExchangeSetDownloadAPIFacade();
            _blobHelpers = new BlobHelpers();
            _fileHelpers = new FileHelpers();
        }

        [TearDown]
        public void TearDown()
        {
          
            var outDir = Path.Combine(TestConfiguration.ProjectDirectory, "out");
            var tempFilePath = Path.Combine(Path.GetTempPath(), "adds-mock");
            // Clean up temporary files and directories
            if (Directory.Exists(outDir))
                Array.ForEach(Directory.GetFiles(outDir, "*.zip"), File.Delete);
            if (Directory.Exists(tempFilePath))
                Array.ForEach(Directory.GetFiles(tempFilePath, "*.zip"), File.Delete);
        }

        [Test]
        public async Task Validate_Exchange_Set_Creation_End_To_End_Positive_Flow()
        {
            var correlationId = Guid.NewGuid().ToString();

            // Give a call to the orchestrator API for the exchange set generation
            var response = await _orchestratorAPIFacade.RequestOrchestrator(correlationId, "TestProduct");

            // Assert  
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            // Wait For Exchange Set Generation process is completed
            await ExchangeSetHelper.WaitForExchangeSetGeneration(correlationId);

            // Verify all builder nodes succeeded
            await ExchangeSetHelper.verifyAllBuilderNodesSucceeded(correlationId);

            //Connect to Azure Blob Storage and check the blob content
            await _blobHelpers.AssertBlobStateAndCorrelationIdAsync("succeeded", correlationId);
            
            var sourceZipPath = Path.Combine(TestConfiguration.ProjectDirectory, "TestData/exchangeSet-25Products.testzip");            

            //Download Exchange Set, call to the Admin API for downloading the exchange set
            var exchangeSetDownloadPath = await _exchangeSetDownloadAPIFacade.DownloadExchangeSetAsZipAsync("FSS", correlationId);

            // Compare the folder and file structures of the source and target zip files
            _fileHelpers.CompareZipFolderAndFileStructures(sourceZipPath, exchangeSetDownloadPath);
        }
    }
}
