﻿using System.Diagnostics;
using UKHO.ADDS.EFS.Builder.S100.Pipelines.Startup.Logging;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Builder.S100.Pipelines.Startup
{
    internal class StartTomcatNode : ExchangeSetPipelineNode
    {
        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<ExchangeSetPipelineContext> context)
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

            var ready = false;

            for (var i = 0; i < 30; i++) // TODO configure this from orchestrator ~30s timeout
            {
                try
                {
                    var result = await context.Subject.ToolClient.ListWorkspaceAsync(context.Subject.WorkspaceAuthenticationKey);
                    if (result.IsSuccess(out var response) && !string.IsNullOrEmpty(response))
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
                //throw new Exception("Tomcat did not start in time");
            }

            return NodeResultStatus.Succeeded;
        }
    }
}
