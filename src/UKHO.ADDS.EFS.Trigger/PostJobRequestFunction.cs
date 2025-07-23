using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UKHO.ADDS.EFS.Configuration.Namespaces;

namespace UKHO.ADDS.EFS.Trigger
{
    public class PostJobRequestFunction
    {
        private readonly ILogger _logger;
        private readonly IConfiguration _config;
        private readonly string _httpsEndpoint;
        private readonly HttpClient _httpClient;
        public PostJobRequestFunction(ILoggerFactory loggerFactory, IConfiguration config, IHttpClientFactory httpFactory)
        {
            _logger = loggerFactory.CreateLogger<PostJobRequestFunction>();
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _httpsEndpoint = _config.GetSection($"services:{ProcessNames.OrchestratorService}:https:0").Value;
            _httpClient = httpFactory.CreateClient();
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            _httpClient.BaseAddress = new Uri(_httpsEndpoint);
        }

        [Function(nameof(PostJobRequestFunction))]
        public async Task Run([TimerTrigger("0 */5 * * * *")]TimerInfo infoTimer)
        {
            var content = new StringContent(
                """
                {
                    "version": 1,
                    "dataStandard": "s100",
                    "products": "",
                    "filter": ""
                }
                """,
                Encoding.UTF8, "application/json");
            var requestId = Guid.NewGuid().ToString();
            content.Headers.Add("x-correlation-id", $"job-{requestId}");


            var jobSubmitResponse = await _httpClient.PostAsync("/jobs", content);

            _logger.LogInformation($"Requested S100 exchange set at: {DateTime.Now}");
            _logger.LogInformation($"Next request schedule at: {infoTimer.ScheduleStatus.Next}");
        }
    }
}
