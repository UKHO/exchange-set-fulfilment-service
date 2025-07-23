using System.Diagnostics;
using UKHO.ADDS.EFS.Builder.S100.Pipelines.Startup.Logging;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Builder.S100.Pipelines.Startup
{
    internal class StartTomcatNode : S100ExchangeSetPipelineNode
    {
        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<S100ExchangeSetPipelineContext> context)
        {
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

            // Wait for Tomcat to respond by using the ToolClient's ListWorkspaceAsync method
            var toolClient = context.Subject.ToolClient;
            var ready = false;

            const int maxRetries = 30; // TODO: consider getting this from configuration
            const int retryDelayInMilliseconds = 1000;

            for (var i = 0; i < maxRetries; i++)
            {
                var result = await toolClient.ListWorkspaceAsync(context.Subject.WorkspaceAuthenticationKey);
                if (result.IsSuccess(out var response) && !string.IsNullOrEmpty(response))
                {
                    ready = true;
                    break;
                }

                await Task.Delay(retryDelayInMilliseconds);
            }

            if (!ready)
            {
                throw new Exception("Tomcat did not start in time");
            }

            return NodeResultStatus.Succeeded;
        }
    }
}
