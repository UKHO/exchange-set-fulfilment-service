using System.Net;
using UKHO.ADDS.EFS.Orchestrator.API.FunctionalTests.Facades;
using UKHO.ADDS.EFS.Orchestrator.API.FunctionalTests.Helpers;

namespace UKHO.ADDS.EFS.Orchestrator.API.FunctionalTests.API
{
    public class OrchestratorAPITests
    {        
       
        [Test]
        public async Task PostRequests_WithValidBodyAndHeader_ReturnsSuccess()
        {

            OrchestratorAPIFacade _orchestratorAPIFacade = new OrchestratorAPIFacade();
            ExchangeSetDownloadAPIFacade _exchangeSetDownloadAPIFacade = new ExchangeSetDownloadAPIFacade();
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
            var blobHelpers = new BlobHelpers();          

            // New single call for download and assertion
            await blobHelpers.AssertBlobStateAndCorrelationIdAsync("succeeded", correlationId);
                      
            var sourceZipPath = "./TestData/exchangeSet-25Products.zip";           

            //Download Exchange Set, call to the Admin API for downloading the exchange set
            var exchangeSetDownloadPath = await _exchangeSetDownloadAPIFacade.DownloadExchangeSetAsZipAsync(correlationId);

            // Compare the folder and file structures of the source and target zip files
            var fileHelpers = new FileHelpers();
            fileHelpers.CompareZipFolderAndFileStructures(sourceZipPath, exchangeSetDownloadPath);
            
        }        

    }
}
