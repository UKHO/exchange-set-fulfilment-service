using System.Threading.Channels;
using Scalar.AspNetCore;
using Serilog;
using UKHO.ADDS.EFS.Common.Configuration.Namespaces;
using UKHO.ADDS.EFS.Common.Messages;
using UKHO.ADDS.EFS.Orchestrator.Api;
using UKHO.ADDS.EFS.Orchestrator.Services;

namespace UKHO.ADDS.EFS.Orchestrator
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();

            var builder = WebApplication.CreateBuilder(args);

            builder.Host.UseSerilog((context, loggerConfiguration) =>
            {
                loggerConfiguration.WriteTo.Console();
                loggerConfiguration.ReadFrom.Configuration(context.Configuration);
            });

            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .AddJsonFile("appsettings.Development.json")
                .Build();

            ConfigureServices(builder, configuration);

            builder.AddServiceDefaults();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                app.MapScalarApiReference(_ => _.Servers = []); // Stop OpenAPI specifying the wrong port in the generated OpenAPI doc
            }

            app.UseAuthorization();

            BuildsApi.Register(app);

            app.Run();
        }

        public static void ConfigureServices(WebApplicationBuilder builder, IConfigurationRoot configuration)
        {
            builder.AddAzureQueueClient(StorageConfiguration.QueuesName);
            builder.AddAzureTableClient(StorageConfiguration.TablesName);

            builder.Services.AddAuthorization();
            builder.Services.AddOpenApi();
            
            var queueChannelSize = configuration.GetValue<int>("QueuePolling:ChannelSize");

            builder.Services.AddSingleton(Channel.CreateBounded<ExchangeSetRequestMessage>(new BoundedChannelOptions(queueChannelSize)
            {
                FullMode = BoundedChannelFullMode.Wait
            }));

            builder.Services.AddHostedService<QueuePollingService>();
            builder.Services.AddHostedService<BuilderDispatcherService>();
        }
    }
}
