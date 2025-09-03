using UKHO.ADDS.EFS.Domain.Builds;
using UKHO.ADDS.EFS.Domain.Builds.S63;
using UKHO.ADDS.EFS.Domain.Services.Storage;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Completion;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Completion.Nodes.S63
{
    internal class CreateBuildMementoNode : CompletionPipelineNode<S63Build>
    {
        private readonly IRepository<BuildMemento> _buildMementoRepository;

        public CreateBuildMementoNode(CompletionNodeEnvironment nodeEnvironment, IRepository<BuildMemento> buildMementoRepository)
            : base(nodeEnvironment)
        {
            _buildMementoRepository = buildMementoRepository;
        }

        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<PipelineContext<S63Build>> context)
        {
            var memento = new BuildMemento()
            {
                BuilderExitCode = Environment.BuilderExitCode,
                JobId = context.Subject.Job.Id,
                BuilderSteps = context.Subject.Build.Statuses,
            };

            await _buildMementoRepository.AddAsync(memento);

            if (Environment.BuilderExitCode != BuilderExitCode.Success)
            {
                await context.Subject.SignalBuildFailure();
            }

            return NodeResultStatus.Succeeded;
        }
    }
}
