using System.Diagnostics;
using Serilog;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Builder.S100.Pipelines.Startup
{
    internal class StartTomcatNode : ExchangeSetPipelineNode
    {
        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<ExchangeSetPipelineContext> context)
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

            process.OutputDataReceived += (sender, args) => Log.Debug($"[Tomcat] {args.Data}");
            process.ErrorDataReceived += (sender, args) => Log.Debug($"[Tomcat] {args.Data}");

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

            return NodeResultStatus.Succeeded;
        }
    }
}
