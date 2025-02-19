using Microsoft.Extensions.Hosting;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace ExchangeSetFulfilment
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = new HostBuilder()
                .ConfigureWebJobs(builder =>
                {
                    // Add Azure Storage bindings (if you need them)
                    builder.AddAzureStorageCoreServices();
                    // Add Timer triggers
                    builder.AddTimer();
                })
                .ConfigureLogging((context, b) =>
                {
                    // Configure logging here if you like
                    b.SetMinimumLevel(LogLevel.Information);
                    b.AddConsole();
                })
                .ConfigureServices(services =>
                {
                    // Register any custom services or HttpClient factories, etc.
                    // e.g., services.AddTransient<IMyApiClient, MyApiClient>();
                })
                .Build();

            using (host)
            {
                host.Run();
            }
        }
    }
}
