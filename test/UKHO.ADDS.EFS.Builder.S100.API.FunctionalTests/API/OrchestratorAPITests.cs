using System.Net;
using UKHO.ADDS.EFS.Orchestrator.API.FunctionalTests.Facades;
using UKHO.ADDS.EFS.Orchestrator.API.FunctionalTests.Helpers;

namespace UKHO.ADDS.EFS.Orchestrator.API.FunctionalTests.API
{
    public class OrchestratorAPITests
    {         
        
        private const string ContainerName = "exchangesetjob";

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

            var blobName = $"{ContainerName}/{correlationId}/{correlationId}";

            // To get as JsonDocument:
            var jsonDoc = await blobHelpers.DownloadBlobAsJsonAsync(blobName);
                       
            // Assert on jsonDoc.RootElement as needed
            Assert.That(jsonDoc.RootElement.GetProperty("state").GetString(), Is.EqualTo("succeeded"), "The 'state' property is not 'succeeded'.");
            Assert.That(jsonDoc.RootElement.GetProperty("correlationId").GetString(), Is.EqualTo(correlationId), "The 'correlationId' property does not match the expected value.");

            var sourceZipPath = @"D:\\toCompare\\source.zip";
            var targetZipPath = @"D:\\toCompare\\target.zip";

            //Download Exchange Set

            // Give a call to the Admin API for downloading the exchange set
            await _exchangeSetDownloadAPIFacade.DownloadExchangeSetAsZipAsync(targetZipPath, "FSS", "S100_ExchangeSet_20250613.zip");

            // Compare the folder and file structures of the source and target zip files          

            var fileHelpers = new FileHelpers();
            fileHelpers.CompareZipFolderAndFileStructures(sourceZipPath, targetZipPath);
            
        }        

    }
}
