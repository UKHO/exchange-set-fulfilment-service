using System.Diagnostics;
using Serilog;
using UKHO.ADDS.EFS.Builder.S100.IIC;
using UKHO.ADDS.EFS.Builder.S100.Pipelines;

namespace UKHO.ADDS.EFS.Builder.S100
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

                var provider = ConfigureServices();

                var pipelineContext = provider.GetRequiredService<PipelineContext>();
                var startupPipeline = provider.GetRequiredService<StartupPipeline>();

                var startupResult = await startupPipeline.ExecutePipeline(pipelineContext);

                if (startupResult.IsFailure(out var startupError))
                {
                    Log.Error($"Startup failed : {startupError.Message}");
                    return -1;
                }

                var assemblyPipeline = provider.GetRequiredService<AssemblyPipeline>();
                var assemblyResult = await assemblyPipeline.ExecutePipeline(pipelineContext);

                if (assemblyResult.IsFailure(out var assemblyError))
                {
                    Log.Error($"Assembly failed : {assemblyError.Message}");
                    return -1;
                }

                var creationPipeline = provider.GetRequiredService<CreationPipeline>();
                var creationResult = await creationPipeline.ExecutePipeline(pipelineContext);

                if (creationResult.IsFailure(out var creationError))
                {
                    Log.Error($"Creation failed : {creationError.Message}");
                    return -1;
                }

                var distributionPipeline = provider.GetRequiredService<DistributionPipeline>();
                var distributionResult = await distributionPipeline.ExecutePipeline(pipelineContext);

                if (distributionResult.IsFailure(out var distributionError))
                {
                    Log.Error($"Distribution failed : {distributionError.Message}");
                    return -1;
                }

                return 0;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"An unhandled exception occurred during execution : {ex.Message}");
                return -1;
            }
            finally
            {
                await Log.CloseAndFlushAsync();
            }
        }

        private static IServiceProvider ConfigureServices()
        {
            var collection = new ServiceCollection();

            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .AddJsonFile("appsettings.Development.json", true)
                .Build();

            collection.AddHttpClient();

            collection.AddSingleton<IConfiguration>(x => configuration);

            collection.AddSingleton<PipelineContext>();
            collection.AddSingleton<StartupPipeline>();
            collection.AddSingleton<AssemblyPipeline>();
            collection.AddSingleton<CreationPipeline>();
            collection.AddSingleton<DistributionPipeline>();

            collection.AddSingleton<IToolClient, ToolClient>();
            

            return collection.BuildServiceProvider();
        }

        private static async Task DoRequestAsync(string baseAddress, string path)
        {
            using var client = new HttpClient { BaseAddress = new Uri(baseAddress) };
            using var response = await client.GetAsync(path);

            var content = await response.Content.ReadAsStringAsync();

            Log.Information($"Content : {content}");
        }

        private static async Task DoRequestTelemetryStyleAsync(string baseAddress, string path)
        {
            using var client = new HttpClient { BaseAddress = new Uri(baseAddress) };
            using var response = await client.GetAsync(path);

            var content = await response.Content.ReadAsStringAsync();
            var sanitisedContent = content.ReplaceLineEndings("");

            Log.Information($"[TELEMETRY] {sanitisedContent}");
        }

        private static async Task StartTomcatAsync()
        {
            Log.Information("Starting Tomcat...");

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/usr/local/tomcat/bin/catalina.sh",
                    Arguments = "run",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = false
                }
            };

            // Tomcat writes logs to stderr

            process.OutputDataReceived += (sender, args) => Log.Information($"[Tomcat] {args.Data}");
            process.ErrorDataReceived += (sender, args) => Log.Information($"[Tomcat] {args.Data}");

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            // Wait for Tomcat to respond on port 8080
            using var httpClient = new HttpClient();
            var ready = false;

            for (var i = 0; i < 30; i++) // ~30s timeout
            {
                try
                {
                    var response = await httpClient.GetAsync("http://localhost:8080");
                    if (response.IsSuccessStatusCode)
                    {
                        Console.WriteLine("Tomcat is ready");
                        ready = true;
                        break;
                    }
                }
                catch
                {
                    // Ignore and retry
                }

                Log.Information("Waiting for Tomcat to become ready...");
                await Task.Delay(1000);
            }

            if (!ready)
            {
                throw new Exception("Tomcat did not start in time");
            }
        }

        private static void ConfigureLogging(IServiceCollection collection) => collection.AddLogging(builder => { builder.AddConsole().AddSerilog(dispose: true); });
    }
}
