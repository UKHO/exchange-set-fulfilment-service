using UKHO.ADDS.EFS.Domain.Builds;
using UKHO.ADDS.EFS.Domain.Jobs;
using UKHO.ADDS.EFS.Domain.Services;
using UKHO.ADDS.EFS.Orchestrator.Api.Messages;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure
{
    internal partial class PipelineContext<TBuild> where TBuild : Build
    {
        private readonly Job _job;
        private readonly TBuild _build;
        private readonly IStorageService _storageService;

        public PipelineContext(Job job, TBuild build, IStorageService storageService, RequestType? requestType = null)
        {
            _job = job;
            _build = build;
            _storageService = storageService;
            _requestType = requestType;
        }

        public Job Job => _job;

        public TBuild Build => _build;

        public RequestType? _requestType { get; }

        public bool IsErrorFileCreated { get; set; }

        public ErrorResponseModel ErrorResponse { get; set; } = new ErrorResponseModel();
    }
}
