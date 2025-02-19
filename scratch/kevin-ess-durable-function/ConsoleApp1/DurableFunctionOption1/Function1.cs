using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;

namespace DurableFunctionOption1
{
    public static class Function1
    {
        // Runs every hour on the hour (cron: 0 0 * * * *)
        // Adjust the schedule to meet your requirements
        [FunctionName("TimeTrigger_Start")]
        public static async Task Run(
            [TimerTrigger("0 0 * * * *")] TimerInfo myTimer,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            string instanceId = await starter.StartNewAsync("OrchestrationFunction", null);
            log.LogInformation($"[TimeTrigger_Start] Started orchestration with ID = '{instanceId}'.");
        }
    }
}
