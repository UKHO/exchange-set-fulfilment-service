using UKHO.ADDS.EFS.Domain.Builds;
using UKHO.ADDS.EFS.Domain.ExternalErrors;
using UKHO.ADDS.EFS.Domain.Jobs;
using UKHO.ADDS.EFS.Domain.Products;
using UKHO.ADDS.EFS.Domain.Services;
using UKHO.ADDS.EFS.Orchestrator.Api.Messages;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure
{
    internal partial class PipelineContext<TBuild> where TBuild : Build
    {
        private readonly Job _job;
        private readonly TBuild _build;
        private readonly IStorageService _storageService;

        public PipelineContext(Job job, TBuild build, IStorageService storageService)
        {
            _job = job;
            _build = build;
            _storageService = storageService;
        }

        public Job Job => _job;

        public TBuild Build => _build;

        public bool IsErrorFileCreated { get; set; }

        public ErrorResponseModel ErrorResponse { get; set; } = new ErrorResponseModel();
    }
}
