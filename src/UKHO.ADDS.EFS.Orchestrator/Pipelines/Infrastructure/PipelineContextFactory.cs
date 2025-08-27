using UKHO.ADDS.EFS.Builds;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables;
using UKHO.ADDS.EFS.Orchestrator.Jobs;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Assembly;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Completion;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Services;
using UKHO.ADDS.EFS.VOS;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure
{
    internal class PipelineContextFactory<TBuild> where TBuild : Build, new()
    {
        private readonly ITable<Job> _jobTable;
        private readonly ITable<TBuild> _buildTable;
        private readonly IStorageService _storageService;

        public PipelineContextFactory(ITable<Job> jobTable, ITable<TBuild> buildTable, IStorageService storageService)
        {
            _jobTable = jobTable;
            _buildTable = buildTable;
            _storageService = storageService;
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
            var job = parameters.CreateJob();

            var build = new TBuild()
            {
                JobId = parameters.JobId,
                DataStandard = parameters.DataStandard,
                BatchId = BatchId.None
            };

            var context = new PipelineContext<TBuild>(job, build, _storageService);

            return Task.FromResult(context);
        }

        public async Task<PipelineContext<TBuild>> CreatePipelineContext(CompletionPipelineParameters parameters)
        {
            var jobResult = await _jobTable.GetUniqueAsync((string)parameters.JobId);
            var buildResult = await _buildTable.GetUniqueAsync((string)parameters.JobId);

            if (jobResult.IsSuccess(out var job) && buildResult.IsSuccess(out var build))
            {
                return new PipelineContext<TBuild>(job, build, _storageService);
            }

            // TODO Tighten up error handling here

            return null;
        }
    }
}
