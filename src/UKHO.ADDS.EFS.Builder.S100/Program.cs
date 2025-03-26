using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using UKHO.ADDS.EFS.Common.Configuration.Orchestrator;

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

            Log.Information("UKHO ADDS EFS S100 Builder");
            Log.Information($"Machine ID      : {Environment.MachineName}");

            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .AddJsonFile("appsettings.Development.json", true)
                .Build();

            var requestId = GetEnvironmentVariable(BuilderEnvironmentVariables.RequestId, WellKnownRequestId.DebugRequestId);
            var fileShareEndpoint = GetEnvironmentVariable(BuilderEnvironmentVariables.FileShareEndpoint, configuration.GetValue<string>("Endpoints:FileShareService")!);
            var salesCatalogueEndpoint = GetEnvironmentVariable(BuilderEnvironmentVariables.SalesCatalogueEndpoint, configuration.GetValue<string>("Endpoints:SalesCatalogueService")!);
            var buildServiceEndpoint = GetEnvironmentVariable(BuilderEnvironmentVariables.BuildServiceEndpoint, configuration.GetValue<string>("Endpoints:BuildService")!);

            Log.Information($"Request id      : {requestId}");
            Log.Information($"File Share      : {fileShareEndpoint}");
            Log.Information($"Sales Catalogue : {salesCatalogueEndpoint}");
            Log.Information($"Build Service   : {buildServiceEndpoint}");

            try
            {
                await StartTomcatAsync();

                //Log.Information($"Request : {requestJson}");

                await DoRequestAsync("http://localhost:8080", "/xchg-2.7/v2.7/dev?arg=test&authkey=noauth");
                await DoRequestAsync(fileShareEndpoint, "/health");
                await DoRequestAsync(salesCatalogueEndpoint, "/health");
                await DoRequestAsync(buildServiceEndpoint, "/");

                var i = 0;

                while (i < 60)
                {
                    ++i;

                    Console.WriteLine("Doing stuff...");
                    Thread.Sleep(1000);
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

        private static async Task DoRequestAsync(string baseAddress, string path)
        {
            using var client = new HttpClient { BaseAddress = new Uri(baseAddress) };
            using var response = await client.GetAsync(path);

            var content = await response.Content.ReadAsStringAsync();

            Log.Information($"Content : {content}");
        }

        private static string GetEnvironmentVariable(string variable, string overrideValue)
        {
            var value = Environment.GetEnvironmentVariable(variable);

            if (!string.IsNullOrEmpty(value))
            {
                return value;
            }



            Log.Error($"{variable} is not set");
            throw new InvalidOperationException($"{variable} is not set");
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
