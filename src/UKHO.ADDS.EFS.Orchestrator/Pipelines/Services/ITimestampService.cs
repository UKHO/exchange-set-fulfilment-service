﻿using UKHO.ADDS.EFS.Orchestrator.Jobs;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Services
{
    internal interface ITimestampService
    {
        /// <summary>
        ///     Gets the timestamp for a given job.
        /// </summary>
        /// <param name="job"></param>
        /// <returns></returns>
        Task<DateTime> GetTimestampForJobAsync(Job job);

        Task SetTimestampForJobAsync(Job job);
    }
}
