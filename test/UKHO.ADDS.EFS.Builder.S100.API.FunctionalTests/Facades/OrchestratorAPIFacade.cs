using System.Net.Http.Json;
using UKHO.ADDS.EFS.Orchestrator.API.FunctionalTests.Support;

namespace UKHO.ADDS.EFS.Orchestrator.API.FunctionalTests.Facades
{
    public class OrchestratorAPIFacade
    {        
        private readonly string _orchestratorApiEndpoint;
        public OrchestratorAPIFacade()
        {
            TestConfiguration testConfiguration = new TestConfiguration();
            _orchestratorApiEndpoint = testConfiguration.OrchestratorApiEndpointName;            
        }              

        public async Task<HttpResponseMessage> RequestOrchestrator(string correlationID, string productsList, string filterCriteria)
        {           

            HttpClient _client = new HttpClient();

            // Arrange  
            var request = new
            {
                version = "1",
                dataStandard = "s100",
                products = productsList,
                filter = filterCriteria
            };

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, _orchestratorApiEndpoint)
            {
                Content = JsonContent.Create(request)
            };

            httpRequest.Headers.Add("x-correlation-id", correlationID);

            var response = await _client.SendAsync(httpRequest);
            _client.Dispose();

            return response;

        }

        public async Task<HttpResponseMessage> CheckJobStatus(string jobId)
        {
            var jobStatusEndpoint = $"{_orchestratorApiEndpoint}/{jobId}";

            var client = new HttpClient();
            var httpRequest = new HttpRequestMessage(HttpMethod.Get, jobStatusEndpoint);

            var response = await client.SendAsync(httpRequest);
            return response;
        }
    }
}
