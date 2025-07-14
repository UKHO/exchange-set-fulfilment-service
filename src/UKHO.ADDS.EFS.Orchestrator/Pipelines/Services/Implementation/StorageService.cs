using UKHO.ADDS.EFS.Builds.S100;
using UKHO.ADDS.EFS.Builds.S57;
using UKHO.ADDS.EFS.Builds.S63;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables;
using UKHO.ADDS.EFS.Orchestrator.Jobs;
using UKHO.ADDS.Infrastructure.Results;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Services.Implementation
{
    internal class StorageService : IStorageService
    {
        private readonly ITable<Job> _jobTable;
        private readonly ITable<S100Build> _s100BuildTable;
        private readonly ITable<S63Build> _s63BuildTable;
        private readonly ITable<S57Build> _s57BuildTable;

        public StorageService(ITable<Job> jobTable, ITable<S100Build> s100BuildTable, ITable<S63Build> s63BuildTable, ITable<S57Build> s57BuildTable)
        {
            _jobTable = jobTable;
            _s100BuildTable = s100BuildTable;
            _s63BuildTable = s63BuildTable;
            _s57BuildTable = s57BuildTable;
        }

        public async Task<Result> CreateJobAsync(Job job) => await _jobTable.AddAsync(job);

        public async Task<Result> UpdateJobAsync(Job job) => await _jobTable.UpsertAsync(job);

        public async Task<Result> UpdateS100BuildAsync(S100Build build) => await _s100BuildTable.UpsertAsync(build);

        public async Task<Result> UpdateS63BuildAsync(S63Build build) => await _s63BuildTable.UpsertAsync(build);

        public async Task<Result> UpdateS57BuildAsync(S57Build build) => await _s57BuildTable.UpsertAsync(build);
    }
}
