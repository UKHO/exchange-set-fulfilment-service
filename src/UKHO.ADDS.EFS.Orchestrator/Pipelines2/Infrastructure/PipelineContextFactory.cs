using System.IO.Pipelines;
using UKHO.ADDS.EFS.Builds;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables;
using UKHO.ADDS.EFS.Orchestrator.Jobs;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines2.Infrastructure
{
    internal class PipelineContextFactory<TBuild> where TBuild : Build, new()
    {
        private readonly ITable<Job> _jobTable;
        private readonly ITable<TBuild> _buildTable;

        public PipelineContextFactory(ITable<Job> jobTable, ITable<TBuild> buildTable)
        {
            _jobTable = jobTable;
            _buildTable = buildTable;
        }

        public async Task Persist(PipelineContext<TBuild> context)
        {
            await _jobTable.UpsertAsync(context.Job);

            if (context.Build != null)
            {
                await _buildTable.UpsertAsync(context.Build);
            }
        }

        public Task<PipelineContext<TBuild>> CreatePipelineContext(AssemblyPipelineParameters parameters)
        {
            var job = new Job()
            {
                Id = parameters.JobId,
                DataStandard = parameters.DataStandard,
                Timestamp = DateTime.UtcNow
            };

            var build = new TBuild()
            {
                JobId = parameters.JobId,
                DataStandard = parameters.DataStandard,
                BatchId = null
            };

            var context = new PipelineContext<TBuild>(job, build);

            return Task.FromResult(context);
        }

        public async Task<PipelineContext<TBuild>?> CreatePipelineContext(CompletionPipelineParameters parameters)
        {
            var jobResult = await _jobTable.GetUniqueAsync(parameters.JobId);
            var buildResult = await _buildTable.GetUniqueAsync(parameters.JobId);

            if (jobResult.IsSuccess(out var job) && buildResult.IsSuccess(out var build))
            {
                return new PipelineContext<TBuild>(job, build);
            }

            return null;
        }
    }
}
