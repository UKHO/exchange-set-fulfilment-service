using System.ComponentModel.Design;
using System.Net;
using System.Threading.Tasks.Dataflow;
using ESSFulfilmentService.Builder.Pipelines;
using ESSFulfilmentService.Common.Messages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using UKHO.ADDS.Infrastructure.Results;

namespace ESSFulfilmentService.Builder
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
                var serviceCollection = new ServiceCollection();

                ConfigureLogging(serviceCollection);
                var serviceProvider = ConfigureInjection(serviceCollection);

                //var startupResult = await ExecutePipeline<StartupPipeline, StartupPipelineContext>(serviceProvider);

                //if (startupResult.IsSuccess(out var startupContext))
                //{
                //    var assemblyResult = await ExecutePipeline<AssemblyPipeline, AssemblyPipelineContext>(serviceProvider);

                //    if (assemblyResult.IsSuccess(out var assemblyContext))
                //    {

                //    }
                //    else
                //    {
                //        Log.Fatal("Assembly pipeline failed to execute");
                //        return (int)ExchangeSetBuilderResult.AssemblyPipelineFailed;
                //    }
                //}
                //else
                //{
                //    Log.Fatal("Startup pipeline failed to execute");
                //    return (int)ExchangeSetBuilderResult.StartupPipelineFailed;
                //}

                await Task.Delay(100000);

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

        private static async Task<IResult<TContext>> ExecutePipeline<TPipeline, TContext>(IServiceProvider serviceProvider) where TPipeline : IBuilderPipeline<TContext> where TContext : class
        {
            var pipeline = serviceProvider.GetRequiredService<TPipeline>();
            var context = serviceProvider.GetRequiredService<TContext>();

            var result = await pipeline.ExecutePipeline(context);

            return result;
        }

        private static void ConfigureLogging(IServiceCollection collection)
        {

            collection.AddLogging(builder => { builder.AddConsole().AddSerilog(dispose: true); });
        }

        private static IServiceProvider ConfigureInjection(IServiceCollection collection)
        {
            collection.AddSingleton<StartupPipeline>();
            collection.AddSingleton<StartupPipelineContext>();

            collection.AddSingleton<AssemblyPipeline>();
            collection.AddSingleton<AssemblyPipelineContext>();

            collection.AddSingleton<ProcessingPipeline>();
            collection.AddSingleton<ProcessingPipelineContext>();

            collection.AddSingleton<DistributionPipeline>();
            collection.AddSingleton<DistributionPipelineContext>();

            return collection.BuildServiceProvider();
        }
    }
}
