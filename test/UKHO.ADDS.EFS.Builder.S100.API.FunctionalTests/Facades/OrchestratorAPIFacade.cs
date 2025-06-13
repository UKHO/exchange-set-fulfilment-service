using System.Net.Http.Json;

namespace UKHO.ADDS.EFS.Orchestrator.API.FunctionalTests.Facades
{
    public class OrchestratorAPIFacade
    {
        private readonly string OrchestratorApiEndpoint = "https://localhost:51627/requests";       

        public async Task<HttpResponseMessage> RequestOrchestrator(string correlationID, string productsList)
        {           

            HttpClient _client = new HttpClient();

            // Arrange  
            var request = new
            {
                dataStandard = "s100", // S100  
                products = productsList
            };

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, OrchestratorApiEndpoint)
            {
                Content = JsonContent.Create(request)
            };

            httpRequest.Headers.Add("x-correlation-id", correlationID);

            var response = await _client.SendAsync(httpRequest);
            _client.Dispose();

            return response;

        }
    }
}
