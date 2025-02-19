using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;

namespace ConsoleApp1.DurableFunctions
{
    public static class TimeTriggerFunction
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
