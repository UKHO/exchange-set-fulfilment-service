using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UKHO.ADDS.EFS.Trigger.Logging;
using UKHO.ADDS.EFS.Messages;
using UKHO.ADDS.EFS.Jobs;

namespace UKHO.ADDS.EFS.Trigger
{
    public class PostJobRequestFunction
    {
        private readonly ILogger<PostJobRequestFunction> _logger;
        private readonly IConfiguration _config;
        private readonly OrchestratorJobClient _jobClient;

        public PostJobRequestFunction(ILoggerFactory loggerFactory, IConfiguration config, OrchestratorJobClient jobClient)
        {
            _logger = loggerFactory.CreateLogger<PostJobRequestFunction>();
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _jobClient = jobClient ?? throw new ArgumentNullException(nameof(jobClient));
        }

        [Function(nameof(PostJobRequestFunction))]
        public async Task Run([TimerTrigger("* * * * *")] TimerInfo infoTimer)
        {
            _logger.LogOrchestratorFunctionStarted(DateTime.UtcNow);

            var correlationId = $"job-{Guid.NewGuid():N}";
            var orchestratorApiUrl = _config["OrchestratorJobApiUrl"];

            if (string.IsNullOrWhiteSpace(orchestratorApiUrl))
            {
                _logger.LogOrchestratorMissingApiUrl();
                return;
            }

            var jobRequest = CreateJobRequest();
            var jobRequestJson = JsonSerializer.Serialize(jobRequest);

            _logger.LogOrchestratorSendingJobRequest(orchestratorApiUrl, correlationId, jobRequestJson);

            try
            {
                var response = await _jobClient.PostJobAsync(orchestratorApiUrl, jobRequest, correlationId);
                var responseBody = await response.Content.ReadAsStringAsync();

                LogApiResponse(response.StatusCode, responseBody);

                _logger.LogOrchestratorNextSchedule(infoTimer.ScheduleStatus.Next);
            }
            catch (Exception ex)
            {
                _logger.LogOrchestratorJobApiException(ex, correlationId);
            }
        }

        private static JobRequestApiMessage CreateJobRequest()
        {
            return new JobRequestApiMessage
            {
                Version = 1,
                DataStandard = DataStandard.S100,
                Products = "",
                Filter = ""
            };
        }

        private void LogApiResponse(System.Net.HttpStatusCode statusCode, string responseBody)
        {
            var code = (int)statusCode;
            if (code >= 200 && code < 300)
            {
                _logger.LogOrchestratorJobApiSucceeded(code, responseBody);
            }
            else if (code >= 400 && code < 500)
            {
                _logger.LogOrchestratorJobApiClientError(code, responseBody);
            }
            else if (code >= 500)
            {
                _logger.LogOrchestratorJobApiServerError(code, responseBody);
            }
            else
            {
                _logger.LogOrchestratorJobApiUnexpectedStatus(code, responseBody);
            }
        }
    }
}
