using Serilog;
using Serilog.Events;
using UKHO.ADDS.Aspire.Configuration;
using UKHO.ADDS.EFS.BuildRequestMonitor.Builders;
using UKHO.ADDS.EFS.BuildRequestMonitor.Monitors;
using UKHO.ADDS.EFS.BuildRequestMonitor.Services;
using UKHO.ADDS.EFS.Domain.Services.Configuration.Namespaces;

namespace UKHO.ADDS.EFS.BuildRequestMonitor
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.AspNetCore.Hosting.Diagnostics", LogEventLevel.Error)
                .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Error)
                .MinimumLevel.Override("Microsoft.AspNetCore.Mvc", LogEventLevel.Error)
                .MinimumLevel.Override("Microsoft.AspNetCore.Routing", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.AspNetCore.Hosting", LogEventLevel.Warning)
                .MinimumLevel.Override("Azure.Core", LogEventLevel.Fatal)
                .MinimumLevel.Override("Azure.Storage.Blobs", LogEventLevel.Fatal)
                .MinimumLevel.Override("Azure.Storage.Queues", LogEventLevel.Warning)
                .CreateLogger();

            var builder = Host.CreateApplicationBuilder(args);

            builder.AddConfiguration(ServiceConfiguration.ServiceName, ProcessNames.ConfigurationService);

            builder.Logging.ClearProviders();
            builder.Logging.AddSerilog(Log.Logger, dispose: true);

            builder.AddAzureQueueClient(StorageConfiguration.QueuesName);

            builder.Services.AddTransient<BuilderContainerService>();

            builder.Services.AddTransient<S100BuildRequestProcessor>();
            builder.Services.AddTransient<S63BuildRequestProcessor>();
            builder.Services.AddTransient<S57BuildRequestProcessor>();

            builder.Services.AddHostedService<S100BuildRequestMonitor>();
            builder.Services.AddHostedService<S63BuildRequestMonitor>();
            builder.Services.AddHostedService<S57BuildRequestMonitor>();

            var host = builder.Build();

            host.Run();
        }
    }
}

