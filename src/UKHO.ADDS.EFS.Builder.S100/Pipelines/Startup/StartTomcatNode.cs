using System.Diagnostics;
using UKHO.ADDS.EFS.Builder.S100.Pipelines.Startup.Logging;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Builder.S100.Pipelines.Startup
{
    internal class StartTomcatNode : ExchangeSetPipelineNode
    {
        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<ExchangeSetPipelineContext> context)
        {
            context.Subject.WorkSpaceRootPath = @"/usr/local/tomcat/ROOT";

            var logger = context.Subject.LoggerFactory.CreateLogger<StartTomcatNode>();
            var catalinaHome = Environment.GetEnvironmentVariable("CATALINA_HOME");

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/usr/local/tomcat/bin/catalina.sh",
                    WorkingDirectory = catalinaHome,
                    Arguments = "run",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = false
                }
            };

            // Tomcat writes logs to stderr

            process.OutputDataReceived += (sender, args) => logger.LogTomcatMessage(new TomcatLogView() { TomcatMessage = args.Data! });
            process.ErrorDataReceived += (sender, args) => logger.LogTomcatMessage(new TomcatLogView() { TomcatMessage = args.Data! });

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            // Wait for Tomcat to respond on port 8080
            using var httpClient = new HttpClient();
            var ready = false;

            for (var i = 0; i < 30; i++) // TODO configure this from orchestrator ~30s timeout
            {
                try
                {
                    var response = await httpClient.GetAsync("http://localhost:8080/xchg-2.7/v2.7/dev?arg=test&authkey=noauth");
                    if (response.IsSuccessStatusCode)
                    {
                        ready = true;
                        break;
                    }
                }
                catch
                {
                    // Ignore and retry
                }

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
