using UKHO.ADDS.EFS.Builds.S100;
using UKHO.ADDS.EFS.Builds.S57;
using UKHO.ADDS.EFS.Builds.S63;
using UKHO.ADDS.EFS.Orchestrator.Jobs;
using UKHO.ADDS.Infrastructure.Results;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Services
{
    internal interface IStorageService
    {
        Task<Result> CreateJobAsync(Job job);

        Task<Result> UpdateJobAsync(Job job);

        Task<Result> UpdateS100BuildAsync(S100Build build);

        Task<Result> UpdateS63BuildAsync(S63Build build);

        Task<Result> UpdateS57BuildAsync(S57Build build);
    }
}
