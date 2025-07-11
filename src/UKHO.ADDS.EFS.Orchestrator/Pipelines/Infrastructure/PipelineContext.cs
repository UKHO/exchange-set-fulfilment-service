﻿using UKHO.ADDS.EFS.Builds;
using UKHO.ADDS.EFS.Orchestrator.Jobs;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Services;

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
    }
}
