using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace UKHO.ADDS.EFS.Trigger
{
    public class PostJobRequestFunction
    {
        private readonly ILogger<PostJobRequestFunction> _logger;
        private readonly IConfiguration _config;
        public PostJobRequestFunction(ILogger<PostJobRequestFunction> logger,
        IConfiguration config)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        [Function(nameof(PostJobRequestFunction))]
        public void Run([TimerTrigger("* * * * *")] TimerInfo infoTimer)
        {
            _logger.LogInformation($"Trigger function started at: {DateTime.Now}");
            _logger.LogInformation($"Next request schedule at: {infoTimer.ScheduleStatus.Next}");
        }
    }
}
