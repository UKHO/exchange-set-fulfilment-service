using System.IO.Compression;
using System.Reflection;
using Serilog;
using UKHO.ADDS.EFS.Builder.S100.Pipelines.Nodes;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Builder.S100.Pipelines.Startup
{
    internal class DeployWorkspaceNode : BuilderNode<PipelineContext>
    {
        private const string WorkspaceResourcePath = "UKHO.ADDS.EFS.Builder.S100.Assets.workspace-root.zip";

        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<PipelineContext> context)
        {
            var assembly = Assembly.GetExecutingAssembly();
            await using var stream = assembly.GetManifestResourceStream(WorkspaceResourcePath)!;

            var workspacePath = Path.Combine("/var/workspace-root");

            if (!Directory.Exists(workspacePath))
            {
                Directory.CreateDirectory(workspacePath);
            }

            ZipFile.ExtractToDirectory(stream, workspacePath);

            Log.Information($"Workspace created at {workspacePath}");
            context.Subject.WorkspaceRootPath = workspacePath;

            return NodeResultStatus.Succeeded;
        }
    }
}
