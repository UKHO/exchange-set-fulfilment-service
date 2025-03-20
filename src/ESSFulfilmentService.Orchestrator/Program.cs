using ESSFulfilmentService.Common.Configuration;
using Serilog;

namespace ESSFulfilmentService.Orchestrator
{
    internal class Program
    {
        private static async Task<int> Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .CreateLogger();

            try
            {
                var host = CreateHost(args);
                await host.RunAsync();

                return 0;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Host terminated unexpectedly");
                return -1;
            }

            finally
            {
                await Log.CloseAndFlushAsync();
            }
        }

        private static IHost CreateHost(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);

            builder.AddAzureQueueClient(StorageConfiguration.QueuesName);
            builder.AddAzureServiceBusClient(ServiceBusConfiguration.ServiceBusName);

            builder.AddServiceDefaults();
            builder.Services.AddHostedService<Worker>();

            builder.Services.AddSerilog();

            return builder.Build();
        }
    }
}
