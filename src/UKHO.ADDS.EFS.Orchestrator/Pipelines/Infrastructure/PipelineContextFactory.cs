using UKHO.ADDS.EFS.Domain.Builds;
using UKHO.ADDS.EFS.Domain.Jobs;
using UKHO.ADDS.EFS.Domain.Services;
using UKHO.ADDS.EFS.Domain.Services.Storage;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Assembly;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Completion;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure
{
    internal class PipelineContextFactory<TBuild> : IPipelineContextFactory<TBuild> where TBuild : Build, new()
    {
        private readonly IRepository<Job> _jobRepository;
        private readonly IRepository<TBuild> _buildRepository;

        private readonly IStorageService _storageService;

        public PipelineContextFactory(IRepository<Job> jobRepository, IRepository<TBuild> buildRepository, IStorageService storageService)
        {
            _jobRepository = jobRepository;
            _buildRepository = buildRepository;
            _storageService = storageService;
        }

        public async Task Persist(PipelineContext<TBuild> context)
        {
            await _jobRepository.UpsertAsync(context.Job);

            if (context.Build != null)
            {
                await _buildRepository.UpsertAsync(context.Build);
            }
        }

        public Task<PipelineContext<TBuild>> CreatePipelineContext(AssemblyPipelineParameters parameters)
        {
            var job = parameters.CreateJob();

            var build = new TBuild()
            {
                JobId = parameters.JobId,
                DataStandard = parameters.DataStandard,
                BatchId = BatchId.None,
            };

            var context = new PipelineContext<TBuild>(job, build, _storageService, parameters.RequestType);

            return Task.FromResult(context);
        }

        public async Task<PipelineContext<TBuild>> CreatePipelineContext(CompletionPipelineParameters parameters)
        {
            var jobResult = await _jobRepository.GetUniqueAsync((string)parameters.JobId);
            var buildResult = await _buildRepository.GetUniqueAsync((string)parameters.JobId);

            if (jobResult.IsSuccess(out var job) && buildResult.IsSuccess(out var build))
            {
                return new PipelineContext<TBuild>(job, build, _storageService);
            }

            // TODO Tighten up error handling here

            return null;
        }
    }
}
