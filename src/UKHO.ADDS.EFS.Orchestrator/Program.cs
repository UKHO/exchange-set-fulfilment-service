using System.Text.Json;
using System.Threading.Channels;
using Microsoft.AspNetCore.Http.Json;
using Scalar.AspNetCore;
using Serilog;
using UKHO.ADDS.EFS.Configuration.Namespaces;
using UKHO.ADDS.EFS.Messages;
using UKHO.ADDS.EFS.Orchestrator.Api;
using UKHO.ADDS.EFS.Orchestrator.Services;
using UKHO.ADDS.EFS.Orchestrator.Tables;
using UKHO.ADDS.Infrastructure.Serialization.Json;

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
            StatusApi.Register(app);

            app.Run();
        }

        public static void ConfigureServices(WebApplicationBuilder builder, IConfigurationRoot configuration)
        {
            builder.Services.Configure<JsonOptions>(options => JsonCodec.DefaultOptions.CopyTo(options.SerializerOptions));

            builder.AddAzureQueueClient(StorageConfiguration.QueuesName);
            builder.AddAzureTableClient(StorageConfiguration.TablesName);

            builder.Services.AddAuthorization();
            builder.Services.AddOpenApi();

            var queueChannelSize = configuration.GetValue<int>("QueuePolling:ChannelSize");

            builder.Services.AddSingleton(Channel.CreateBounded<ExchangeSetRequestMessage>(new BoundedChannelOptions(queueChannelSize) { FullMode = BoundedChannelFullMode.Wait }));

            builder.Services.AddHostedService<QueuePollingService>();
            builder.Services.AddHostedService<BuilderDispatcherService>();

            builder.Services.AddSingleton<ExchangeSetRequestTable>();
            builder.Services.AddSingleton<ExchangeSetBuilderNodeStatusTable>();
        }
    }
}
