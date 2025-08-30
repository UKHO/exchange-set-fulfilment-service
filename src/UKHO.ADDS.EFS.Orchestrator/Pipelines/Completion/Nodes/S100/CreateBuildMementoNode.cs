using UKHO.ADDS.EFS.Domain.Builds;
using UKHO.ADDS.EFS.Domain.Builds.S100;
using UKHO.ADDS.EFS.Domain.Services.Infrastructure.Tables;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Completion;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Completion.Nodes.S100
{
    internal class CreateBuildMementoNode : CompletionPipelineNode<S100Build>
    {
        private readonly IRepository<BuildMemento> _buildMementoRepository;

        public CreateBuildMementoNode(CompletionNodeEnvironment nodeEnvironment, IRepository<BuildMemento> buildMementoRepository)
            : base(nodeEnvironment)
        {
            _buildMementoRepository = buildMementoRepository;
        }

        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<PipelineContext<S100Build>> context)
        {
            var memento = new BuildMemento()
            {
                BuilderExitCode = Environment.BuilderExitCode,
                JobId = context.Subject.Job.Id,
                BuilderSteps = context.Subject.Build.Statuses,
            };

            var result = await _buildMementoRepository.AddAsync(memento);

            if (Environment.BuilderExitCode != BuilderExitCode.Success)
            {
                await context.Subject.SignalBuildFailure();
            }

            if (result.IsFailure())
            {
                return NodeResultStatus.Failed;
            }

            return NodeResultStatus.Succeeded;
        }
    }
}
