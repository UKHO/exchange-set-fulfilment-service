using Azure.Storage.Queues;
using UKHO.ADDS.EFS.Configuration.Orchestrator;

namespace UKHO.ADDS.EFS.Builder.S100.Factories
{
    internal class QueueClientFactory
    {
        private const string AzuriteAccountName = "devstoreaccount1";
        private const string AzuriteKey = "Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==";

        public static QueueClient CreateRequestQueueClient(IConfiguration configuration)
        {
            var environment = Environment.GetEnvironmentVariable(BuilderEnvironmentVariables.AddsEnvironment)!;

            switch (environment)
            {
                case "local":
                    break;
                default:
                    
                    break;
            }

            return null;
        }

        public static QueueClient CreateResponseQueueClient(IConfiguration configuration)
        {
            return null;
        }
    }
}
