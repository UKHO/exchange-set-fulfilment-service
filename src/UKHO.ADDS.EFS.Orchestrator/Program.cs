using System.Threading.Channels;
using Scalar.AspNetCore;
using UKHO.ADDS.EFS.Common.Configuration.Namespaces;
using UKHO.ADDS.EFS.Common.Configuration.Orchestrator;
using UKHO.ADDS.EFS.Common.Messages;
using UKHO.ADDS.EFS.Orchestrator.Api;
using UKHO.ADDS.EFS.Orchestrator.Services;

namespace UKHO.ADDS.EFS.Orchestrator
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .AddJsonFile("appsettings.Development.json")
                .Build();

            ConfigureServices(builder, config);

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

        public static void ConfigureServices(WebApplicationBuilder builder, IConfigurationRoot config)
        {
            builder.AddAzureQueueClient(StorageConfiguration.QueuesName);
            builder.AddAzureTableClient(StorageConfiguration.TablesName);

            builder.Services.AddAuthorization();
            builder.Services.AddOpenApi();
            
            var queuePollingMaxMessages = config.GetValue<int>("QueuePolling:QueuePollingMaxMessages");

            var builderStartupValue = Environment.GetEnvironmentVariable(OrchestratorEnvironmentVariables.BuilderStartup);
            if (builderStartupValue == null)
            {
                throw new InvalidOperationException($"Environment variable {OrchestratorEnvironmentVariables.BuilderStartup} is not set");
            }

            var builderStartup = Enum.Parse<BuilderStartup>(builderStartupValue);

            builder.Services.AddSingleton(Channel.CreateBounded<ExchangeSetRequestMessage>(new BoundedChannelOptions(queuePollingMaxMessages)
            {
                FullMode = BoundedChannelFullMode.Wait
            }));

            builder.Services.AddHostedService<QueuePollingService>();
        }
    }
}
