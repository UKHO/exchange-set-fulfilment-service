using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

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
            Log.Information($"Machine ID   : {Environment.MachineName}");

            try
            {
                await StartTomcatAsync();

                //using var client = new HttpClient() { BaseAddress = new Uri("http://host.docker.internal:5679") };
                //using var response = await client.GetAsync("/erp/health");

                //var content = await response.Content.ReadAsStringAsync();

                //Log.Information($"Content : {content}");

                using var client = new HttpClient() { BaseAddress = new Uri("http://localhost:8080") };
                using var response = await client.GetAsync("/xchg-2.7/v2.7/dev?arg=test&authkey=noauth");

                var content = await response.Content.ReadAsStringAsync();

                Log.Information($"Content : {content}");

                while (true)
                {
                    Console.WriteLine("Hello, World!");
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

        private static void ConfigureLogging(IServiceCollection collection)
        {

            collection.AddLogging(builder => { builder.AddConsole().AddSerilog(dispose: true); });
        }
    }
}
