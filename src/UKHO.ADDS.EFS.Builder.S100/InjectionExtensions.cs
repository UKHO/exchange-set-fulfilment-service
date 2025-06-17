using System.Diagnostics.CodeAnalysis;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;
using UKHO.ADDS.Clients.FileShareService.ReadOnly;
using UKHO.ADDS.Clients.FileShareService.ReadWrite;
using UKHO.ADDS.EFS.Builder.S100.IIC;
using UKHO.ADDS.EFS.Builder.S100.Pipelines;
using UKHO.ADDS.EFS.Builder.S100.Services;
using UKHO.ADDS.EFS.Configuration.Orchestrator;
using UKHO.ADDS.EFS.Extensions;

namespace UKHO.ADDS.EFS.Builder.S100
{
    [ExcludeFromCodeCoverage]
    internal static class InjectionExtensions
    {
        public static IServiceCollection AddS100BuilderServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddLogging(ConfigureLogging);
            services.AddHttpClient();
            services.AddPipelineServices();
            services.AddFileShareServices(configuration);
            services.AddIICToolServices(configuration);

            return services;
        }

        private static void ConfigureLogging(ILoggingBuilder loggingBuilder)
        {
            // Clear any default providers
            loggingBuilder.ClearProviders();

            // Add Serilog as the only logger
            loggingBuilder.AddSerilog(dispose: true);
        }

        private static IServiceCollection AddPipelineServices(this IServiceCollection services)
        {
            services.AddSingleton<ExchangeSetPipelineContext>();
            services.AddSingleton<StartupPipeline>();
            services.AddSingleton<AssemblyPipeline>();
            services.AddSingleton<CreationPipeline>();
            services.AddSingleton<DistributionPipeline>();
            services.AddSingleton<INodeStatusWriter, NodeStatusWriter>();

            return services;
        }

        private static IServiceCollection AddFileShareServices(this IServiceCollection services, IConfiguration configuration)
        {
            var fileShareEndpoint = Environment.GetEnvironmentVariable(BuilderEnvironmentVariables.FileShareEndpoint)
                ?? configuration["Endpoints:FileShareService"]!;

            // Read-Write Client
            services.AddSingleton<IFileShareReadWriteClientFactory>(provider =>
                new FileShareReadWriteClientFactory(provider.GetRequiredService<IHttpClientFactory>()));

            services.AddSingleton(provider =>
            {
                var factory = provider.GetRequiredService<IFileShareReadWriteClientFactory>();
                return factory.CreateClient(fileShareEndpoint.RemoveControlCharacters(), string.Empty);
            });

            // Read-Only Client
            services.AddSingleton<IFileShareReadOnlyClientFactory>(provider =>
                new FileShareReadOnlyClientFactory(provider.GetRequiredService<IHttpClientFactory>()));

            services.AddSingleton(provider =>
            {
                var factory = provider.GetRequiredService<IFileShareReadOnlyClientFactory>();
                return factory.CreateClient(fileShareEndpoint.RemoveControlCharacters(), string.Empty);
            });

            return services;
        }

        private static IServiceCollection AddIICToolServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddHttpClient<IToolClient, ToolClient>((serviceProvider, client) =>
            {
                var baseUrl = configuration["Endpoints:IICTool"];
                if (string.IsNullOrWhiteSpace(baseUrl))
                    throw new InvalidOperationException("Endpoints:IICTool configuration is missing.");
                client.BaseAddress = new Uri(baseUrl);
            });

            return services;
        }

        public static void ConfigureSerilog()
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.Console(new JsonFormatter())
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
        }
    }
}
