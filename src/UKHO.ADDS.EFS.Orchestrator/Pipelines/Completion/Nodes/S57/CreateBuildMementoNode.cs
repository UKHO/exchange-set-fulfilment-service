using UKHO.ADDS.EFS.Domain.Builds;
using UKHO.ADDS.EFS.Domain.Builds.S57;
using UKHO.ADDS.EFS.Domain.Services.Storage;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Completion;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Completion.Nodes.S57
{
    internal class CreateBuildMementoNode : CompletionPipelineNode<S57Build>
    {
        private readonly IRepository<BuildMemento> _buildRepositoryTable;

        public CreateBuildMementoNode(CompletionNodeEnvironment nodeEnvironment, IRepository<BuildMemento> buildRepositoryTable)
            : base(nodeEnvironment)
        {
            _buildRepositoryTable = buildRepositoryTable;
        }

        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<PipelineContext<S57Build>> context)
        {
            var memento = new BuildMemento()
            {
                BuilderExitCode = Environment.BuilderExitCode,
                JobId = context.Subject.Job.Id,
                BuilderSteps = context.Subject.Build.Statuses,
            };

            await _buildRepositoryTable.AddAsync(memento);

            if (Environment.BuilderExitCode != BuilderExitCode.Success)
            {
                await context.Subject.SignalBuildFailure();
            }

            return NodeResultStatus.Succeeded;
        }
    }
}
