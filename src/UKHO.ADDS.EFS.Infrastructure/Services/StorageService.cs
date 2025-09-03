using UKHO.ADDS.EFS.Domain.Builds.S100;
using UKHO.ADDS.EFS.Domain.Builds.S57;
using UKHO.ADDS.EFS.Domain.Builds.S63;
using UKHO.ADDS.EFS.Domain.Jobs;
using UKHO.ADDS.EFS.Domain.Services;
using UKHO.ADDS.EFS.Domain.Services.Storage;
using UKHO.ADDS.Infrastructure.Results;

namespace UKHO.ADDS.EFS.Infrastructure.Services
{
    internal class StorageService : IStorageService
    {
        private readonly IRepository<Job> _jobRepository;
        private readonly IRepository<S100Build> _s100BuildRepository;
        private readonly IRepository<S63Build> _s63BuildRepository;
        private readonly IRepository<S57Build> _s57BuildRepository;

        public StorageService(IRepository<Job> jobRepository, IRepository<S100Build> s100BuildRepository, IRepository<S63Build> s63BuildRepository, IRepository<S57Build> s57BuildRepository)
        {
            _jobRepository = jobRepository;
            _s100BuildRepository = s100BuildRepository;
            _s63BuildRepository = s63BuildRepository;
            _s57BuildRepository = s57BuildRepository;
        }

        public async Task<Result> CreateJobAsync(Job job) => await _jobRepository.AddAsync(job);

        public async Task<Result> UpdateJobAsync(Job job) => await _jobRepository.UpsertAsync(job);

        public async Task<Result> UpdateS100BuildAsync(S100Build build) => await _s100BuildRepository.UpsertAsync(build);

        public async Task<Result> UpdateS63BuildAsync(S63Build build) => await _s63BuildRepository.UpsertAsync(build);

        public async Task<Result> UpdateS57BuildAsync(S57Build build) => await _s57BuildRepository.UpsertAsync(build);
    }
}
